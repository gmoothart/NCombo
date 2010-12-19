using System;
using System.Linq;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;

namespace NCombo
{
    public class ComboHandler : BaseHttpHandler
    {
        private string yuiDir;

        Regex cssRelativeUrl = new Regex(@"(url\()(?!(http|data))(\S+)(\))");
        Regex cssAlphaImageUrl = new Regex(@"AlphaImageLoader\(src=['""](.*?)['""]");

        public override void HandleRequest(HttpContextBase context)
        {
            yuiDir = ConfigurationManager.AppSettings["yuiDir"];
            if (string.IsNullOrEmpty(yuiDir))
            {
                yuiDir = "~/yui/";
            }

            string q = context.Request.Url.Query.Substring(1);

            var paths =
                from path in q.Split('&')
                where !string.IsNullOrEmpty(path)
                select VirtualPathUtility.ToAbsolute(yuiDir + path, context.Request.ApplicationPath);

            //
            // Set Mime Type
            //
            if ( isCSS(paths.First()) ) {
                context.Response.ContentType = "text/css";
            }
            else {
                context.Response.ContentType = "application/x-javascript";
            }

            // TODO: cache
            // copy individual file streams to the output
            foreach (string virtualPath in paths) {
                string filePath = context.Server.MapPath(virtualPath);
                if (!File.Exists(filePath)) {
                    RespondFileNotFound(context);
                }
                string contents = File.ReadAllText(filePath);

                if (isCSS(virtualPath)) {
                    contents = fixupCss(virtualPath, contents);
                }

                context.Response.Write(contents);
                context.Response.Write('\n');
            }
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

        private bool isCSS(string path)
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
