using System;
using System.Linq;
using System.Web;

namespace NCombo
{
    public class ComboHandler : BaseHttpHandler
    {

        public override void HandleRequest(HttpContextBase context)
        {
            
        }


        public override bool ValidateParameters(HttpContextBase context)
        {
            // TODO: validation on query string
            return true;
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
    }
}