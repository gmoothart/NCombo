using System;
using System.Linq;
using System.Web;

namespace NCombo
{
    public class ComboHandler : IHttpHandler
    {
        /// <summary>
        /// You will need to configure this handler in the web.config file of your 
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpHandler Members

        public bool IsReusable
        {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            //
            // possible sources:
            //   yui3
            //   yui2in3
            //   yui gallery
            //   app javascript
            //
            string q = context.Request.Url.Query.Substring(1);
            var modules = q.Split('&');
                          

            //write your handler implementation here.
        }

        #endregion
    }
}
