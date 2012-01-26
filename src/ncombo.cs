using System;
using System.Linq;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Diagnostics;

namespace NCombo
{
    public class ComboHandler : BaseHttpHandler
    {
        string baseDir;
        string cacheDir;
        Regex cssRelativeUrl = new Regex(@"(url\()(?!(http|data))(\S+?)(\))");
        Regex cssAlphaImageUrl = new Regex(@"AlphaImageLoader\(src=['""](.*?)['""]");

        protected override void Init(HttpContextBase context)
        {
            //
            // Read configuration
            //
            var config = (NComboSectionHandler)ConfigurationManager.GetSection("ncombo");
            bool hasConfig = (config != null);
            baseDir = hasConfig ? config.BaseDir : "~/yui/";
            cacheDir = hasConfig ? config.CacheDir : "~/yui/cache/";

            //
            // Make sure the cache directory exists
            //
            if (context != null)
            {
                string absCachePath = context.Server.MapPath(cacheDir);
                if (!Directory.Exists(absCachePath))
                {
                    Directory.CreateDirectory(absCachePath);
                }
            }
        }

        public override void HandleRequest(HttpContextBase context)
        {
            string q = context.Request.Url.Query.Substring(1);
            var paths = from path in q.Split('&')
                        where !string.IsNullOrEmpty(path)
                        select VirtualPathUtility.ToAbsolute(baseDir + path, context.Request.ApplicationPath);
            

            //
            // Set mime type and compression header
            //
            bool isCSS = pathIsCSS(paths.First());
            if (isCSS)
            {
                context.Response.ContentType = "text/css";
            }
            else
            {
                context.Response.ContentType = "application/x-javascript";
            }

            bool canGzip = isClientGzipEnabled(context.Request);
            if (canGzip)
            {
                context.Response.AppendHeader("Content-Encoding", "gzip");
            }


            //
            // Get results. Check disk cache first.
            //
            string cacheFilename = getCacheFilename(q, canGzip, context.Server);
            if (File.Exists(cacheFilename)) {
                context.Response.WriteFile(cacheFilename);
            }
            else {
                //
                // First calculate and cache plaintext version
                //
                string plainCacheFilename = getCacheFilename(q, false, context.Server);
                using(StreamWriter sw = new StreamWriter(plainCacheFilename))
                {
                    // create combo file by concatenating files together
                    foreach (string virtualPath in paths)
                    {
                        string filePath = context.Server.MapPath(virtualPath);
                        if (!File.Exists(filePath))
                        {
                            RespondFileNotFound(context);
                        }
                        string scriptContent = File.ReadAllText(filePath);

                        if (isCSS)
                        {
                            scriptContent = fixupCss(virtualPath, scriptContent);
                        }

                        sw.WriteLine(scriptContent);
                    }
                }

                //
                // Now cache gzipped version
                //
                string gzipCacheFilename = getCacheFilename(q, true, context.Server);
                GzipFile(plainCacheFilename, gzipCacheFilename);

                //
                // Finally, return
                //
                string fn = canGzip ? gzipCacheFilename : plainCacheFilename;
                context.Response.WriteFile(fn);
            }
        }

        /// <summary>
        /// Gzip the source file to the dest filename.
        /// </summary>
        /// <remarks>
        /// Borrowed mostly from:
        /// http://msdn.microsoft.com/en-us/library/ms404280(v=VS.85).aspx
        /// 
        /// There is a somewhat simpler method in .Net 4, using CopyTo:
        /// http://msdn.microsoft.com/en-us/library/ms404280.aspx
        /// 
        /// I am not sure what difference, if any, it makes on performance.
        /// </remarks>
        private void GzipFile(string sourceFilename, string destFilename)
        {
            using(FileStream sourceFile = File.OpenRead(sourceFilename))
            using(FileStream destFile = File.Create(destFilename))
            using(GZipStream output = new GZipStream(destFile, CompressionMode.Compress))
            {
                byte[] buffer = new byte[sourceFile.Length];
                sourceFile.Read(buffer, 0, buffer.Length);

                output.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Calculate a unique filename representing this request.
        /// </summary>
        public string getCacheFilename(string query, bool withGzip, 
            HttpServerUtilityBase server)
        {
            Debug.Assert(!string.IsNullOrEmpty(cacheDir));

            //
            // lowercase the query, and hash it
            //
            byte[] MessageBytes = Encoding.ASCII.GetBytes(query.ToLower());

            SHA1Managed sha1 = new SHA1Managed();
            byte[] sha1Bytes = sha1.ComputeHash(MessageBytes);

            StringBuilder sb = new StringBuilder();
            sb.Append( Convert.ToBase64String(sha1Bytes) );

            //
            // replace any invalid path characters
            //
            foreach(char c in Path.GetInvalidFileNameChars()) {
                sb.Replace(c, '_');
            }

            //
            // use a distinct key for gzipped content
            //
            if (withGzip)
            {
                sb.Append(".gz");
            }

            string relPath = Path.Combine(cacheDir, sb.ToString());
            return server.MapPath(relPath);
        }

        public bool isClientGzipEnabled(HttpRequestBase rq)
        {
            string AcceptEncoding = rq.Headers["Accept-Encoding"];
            return (!string.IsNullOrEmpty(AcceptEncoding) &&
                   (AcceptEncoding.Contains("gzip")));
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
