using System;
using System.Linq;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace NCombo
{
    public class ComboHandler : BaseHttpHandler
    {
        string baseDir;
        string cacheDir;
        Regex cssRelativeUrl = new Regex(@"(url\()(?!(http|data))(\S+)(\))");
        Regex cssAlphaImageUrl = new Regex(@"AlphaImageLoader\(src=['""](.*?)['""]");
        FileCache fileCache = new FileCache();

        protected override void Init()
        {
            //
            // Read configuration
            //
            var config = (NComboSectionHandler)ConfigurationManager.GetSection("ncombo");
            bool hasConfig = (config != null);
            baseDir = hasConfig ? config.BaseDir : "~/yui/";
            cacheDir = hasConfig ? config.CacheDir : "~/yui/cache/";
        }

        public override void HandleRequest(HttpContextBase context)
        {
            string q = context.Request.Url.Query.Substring(1);
            var paths = from path in q.Split('&')
                        where !string.IsNullOrEmpty(path)
                        select VirtualPathUtility.ToAbsolute(baseDir + path, context.Request.ApplicationPath);
            
            bool isCSS = pathIsCSS(paths.First());

            if (fileCache.Contains(q)) {
                // cache provider is gzip-aware. Serve the content gzipped
                //   if the client supports it
                SetGzipHeaders();
                context.Response.Write(fileCache.Get(q, clientGzipEnabled));
            }
            else {
                StringBuilder sb = new StringBuilder();

                //
                // create combo file by concatenating files together
                //
                foreach (string virtualPath in paths)
                {
                    string filePath = context.Server.MapPath(virtualPath);
                    if (!File.Exists(filePath))
                    {
                        RespondFileNotFound(context);
                    }
                    string contents = File.ReadAllText(filePath);

                    if (isCSS)
                    {
                        contents = fixupCss(virtualPath, contents);
                    }

                    sb.AppendLine(contents);
                }
                string result = sb.ToString();

                // cache - both plaintext and gzipped
                fileCache.Add(q, result);

                // Write to the response.
                //   We assume that gzipping is turned on at the IIS level, 
                //   so there's no advantage to doing it ourselves here.
                //   On the next request the file will be served gzipped
                //   from cache.
                context.Response.Write(result);
            }

            //
            // Set Mime Type
            //
            if ( isCSS ) {
                context.Response.ContentType = "text/css";
            }
            else {
                context.Response.ContentType = "application/x-javascript";
            }
        }

        /// <summary>
        /// Calculate a unique key representing this request.
        /// </summary>
        public string getCacheKey(string query)
        {
            //
            // lowercase the query, and hash it
            //
            byte[] MessageBytes = Encoding.ASCII.GetBytes(query.ToLower());

            SHA1Managed sha1 = new SHA1Managed();
            byte[] sha1Bytes = sha1.ComputeHash(MessageBytes);

            StringBuilder sb = new StringBuilder();
            sb.Append( Convert.ToBase64String(sha1Bytes) );

            //
            // replace any invalid path characters, b/c we'll
            // be using this as a filename
            //
            foreach(char c in Path.GetInvalidFileNameChars()) {
                sb.Replace(c, '_');
            }

            return sb.ToString();
        }
        
        /// <summary>
        /// When combo-loading, css paths get mixed up. Must fix that
        /// </summary>
        /// <remarks>
        /// Regular Expressions and logic inspired by the PHP Loader:
        /// https://github.com/yui/phploader/blob/master/phploader/combo.php
        /// </remarks>
        public string fixupCss(string path, string contents)
        {
            string resourceBase = path.Substring(0, path.LastIndexOf("/") + 1);
            string resourceContent = contents;
            
            // fix url for relative paths (e.g. ../../foo.png), 
            // just filename (e.g. url(foo.png) ), or subdirs/filename (e.g. url(foo/foo.png) )
            // don't match absolute urls or data uris.
            resourceContent = cssRelativeUrl.Replace(resourceContent,
                "$1" + resourceBase + "$3$4");
            
            // AlphaImageLoader relative paths (e.g.) AlphaImageLoader(src='../../foo.png')
            resourceContent = cssAlphaImageUrl.Replace(resourceContent,
                "AlphaImageLoader(src='" + resourceBase + "$1'");
            
            return resourceContent;
        }

        private bool pathIsCSS(string path)
        {
            return Path.GetExtension(path) == ".css";
        }


        /// <summary>
        /// Validate query string. At this stage, just make sure it exists.
        /// </summary>
        public override bool ValidateParameters(HttpContextBase context)
        {
            return context.Request.QueryString.Count > 0;
        }

        public override void SetResponseCachePolicy(HttpCachePolicyBase cache)
        {
            cache.SetCacheability(HttpCacheability.Public);
            cache.SetExpires(DateTime.Now.AddYears(10));
        }

        public override bool RequiresAuthentication
        {
            get { return false; }
        }
    }
}
