using System;
using System.Linq;
using System.Web;
using System.IO;
using System.Collections.Generic;

namespace NCombo
{
    public class ComboHandler : BaseHttpHandler
    {
        // TODO: make this configurable
        private string yuiDir = "~/yui/";

        private Dictionary<string, string> extToMime = new Dictionary<string, string>
        {
            {".css", "text/css"},
            {".js", "application/x-javascript"},
        };

        public override void HandleRequest(HttpContextBase context)
        {
            string q = context.Request.Url.Query.Substring(1);

            var paths =
                from path in q.Split('&')
                where !string.IsNullOrEmpty(path)
                select context.Server.MapPath(yuiDir + path);

            //
            // Set Mime Type
            //
            string ext = Path.GetExtension( paths.First() );
            if (extToMime.ContainsKey(ext)) {
                context.Response.ContentType = extToMime[ext];
            }

            // TODO: cache
            // copy individual file streams to the output
            foreach (string p in paths) {
                if (!File.Exists(p)) {
                    RespondFileNotFound(context);
                }
                using (FileStream fs = File.OpenRead(p)) {
                    context.Response.WriteFile(p);
                    context.Response.Write('\n');
                }
            }

        }

        public override bool ValidateParameters(HttpContextBase context)
        {
            // TODO: validation on query string
            return true;
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