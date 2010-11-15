using System;
using System.Linq;
using System.Web;
using System.IO;

namespace NCombo
{
    public class ComboHandler : BaseHttpHandler
    {
        // TODO: make this configurable
        private string yuiDir = "~/yui/";

        public override void HandleRequest(HttpContextBase context)
        {
            string q = context.Request.Url.Query.Substring(1);

            var paths =
                from path in q.Split('&')
                select context.Server.MapPath(yuiDir + path);

            // copy individual file streams to the output
            foreach (string p in paths) {
                if (!File.Exists(p)) {
                    RespondFileNotFound(context);
                }
                using (FileStream fs = File.OpenRead(p)) {
                    context.Response.WriteFile(p);
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
            cache.SetExpires(DateTime.MaxValue);
        }

        public override bool RequiresAuthentication
        {
            get { return false; }
        }

        public override string ContentMimeType
        {
            get {
                // TODO: may want to combo-load css or other resource types in the future
                return "application/javascript";
            }
        }

        /// <summary>
        /// Copies one stream to another.
        /// </summary>
        /// <remarks>
        /// Courtesy of Mehrdad Afshari
        /// http://stackoverflow.com/questions/444309/what-would-be-the-fastest-way-to-concatenate-three-files-in-c/444316#444316
        /// </remarks>
        private void CopyStream(Stream destination, Stream source) {
            int count;
           
            byte[] buffer = new byte[1024];
            while( (count = source.Read(buffer, 0, buffer.Length)) > 0)
               destination.Write(buffer, 0, count);
        }
    }
}