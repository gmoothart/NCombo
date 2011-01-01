using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;

namespace NCombo
{
    /// <summary>
    /// An abstract base Http Handler for all your
    /// <see cref="IHttpHandler"/> needs.
    /// </summary>
    /// <remarks>
    /// <p>
    /// For the most part, classes that inherit from this
    /// class do not need to override <see cref="ProcessRequest"/>.
    /// Instead implement the abstract methods and
    /// properties and put the main business logic
    /// in the <see cref="HandleRequest"/>.
    /// </p>
    /// <p>
    /// HandleRequest should respond with a StatusCode of
    /// 200 if everything goes well, otherwise use one of
    /// the various "Respond" methods to generate an appropriate
    /// response code.  Or use the HttpStatusCode enumeration
    /// if none of these apply.
    /// </p>
    /// <p>
    /// Courtesy of Phil Haack:
    /// http://haacked.com/archive/2005/03/17/AnAbstractBoilerplateHttpHandler.aspx
    /// </p>
    /// <p>
    /// Updated to use HttpContextBase for better testability, inspired by
    /// Kazi Manzur:
    /// http://weblogs.asp.net/rashid/archive/2009/03/12/unit-testable-httpmodule-and-httphandler.aspx
    /// </p>
    /// </remarks>
    public abstract class BaseHttpHandler : IHttpHandler
    {
        /// <summary>
        /// Creates a new <see cref="BaseHttpHandler"/> instance.
        /// </summary>
        public BaseHttpHandler() {
            Init();
        }

        /// <summary>
        /// Code which is necessary to initialize this instance.
        /// </summary>
        protected abstract void Init();

        /// <summary>
        /// Processs the incoming HTTP request.
        /// </summary>
        /// <param name="context">Context.</param>
        public void ProcessRequest(HttpContext context)
        {
            // replace HttpContext with mockable HttpContextBase for testability
            var ctx = new HttpContextWrapper(context);

            SetResponseCachePolicy(ctx.Response.Cache);

            if (!ValidateParameters(ctx))
            {
                RespondInternalError(ctx);
                return;
            }

            if (RequiresAuthentication
                && !context.User.Identity.IsAuthenticated)
            {
                RespondForbidden(ctx);
                return;
            }
            
            HandleRequest(ctx);
        }

        /// <summary>
        /// Indicates whether or not this handler can be
        /// reused between successive requests.
        /// </summary>
        /// <remarks>
        /// Return true if this handler does not maintain
        /// any state (generally a good practice).  Otherwise
        /// returns false.
        /// </remarks>
        public bool IsReusable
        {
            get { return true; }
        }

        /// <summary>
        /// Handles the request.  This is where you put your
        /// business logic.
        /// </summary>
        /// <remarks>
        /// <p>This method should result in a call to one 
        /// (or more) of the following methods:</p>
        /// <p><code>context.Response.BinaryWrite();</code></p>
        /// <p><code>context.Response.Write();</code></p>
        /// <p><code>context.Response.WriteFile();</code></p>
        /// <p>
        /// <code>
        /// someStream.Save(context.Response.OutputStream);
        /// </code>
        /// </p>
        /// <p>etc...</p>
        /// <p>
        /// If you want a download box to show up with a 
        /// pre-populated filename, add this call here 
        /// (supplying a real filename).
        /// </p>
        /// <p>
        /// </p>
        /// <code>Response.AddHeader("Content-Disposition"
        /// , "attachment; filename=\"" + Filename + "\"");</code>
        /// </p>
        /// </remarks>
        /// <param name="context">Context.</param>
        public abstract void HandleRequest(HttpContextBase context);

        /// <summary>
        /// Validates the parameters.  Inheriting classes must
        /// implement this and return true if the parameters are
        /// valid, otherwise false.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <returns><c>true</c> if the parameters are valid,
        /// otherwise <c>false</c></returns>
        public abstract bool ValidateParameters(HttpContextBase context);

        /// <summary>
        /// Gets a value indicating whether this handler
        /// requires users to be authenticated.
        /// </summary>
        /// <value>
        ///    <c>true</c> if authentication is required
        ///    otherwise, <c>false</c>.
        /// </value>
        public abstract bool RequiresAuthentication { get; }

        /// <summary>
        /// Sets the cache policy.  Unless a handler overrides
        /// this method, handlers will not allow a respons to be
        /// cached.
        /// </summary>
        /// <param name="cache">Cache.</param>
        public virtual void SetResponseCachePolicy
            (HttpCachePolicyBase cache)
        {
            cache.SetCacheability(HttpCacheability.NoCache);
            cache.SetNoStore();
            cache.SetExpires(DateTime.MinValue);
        }

        /// <summary>
        /// Helper method used to Respond to the request
        /// that the file was not found.
        /// </summary>
        /// <param name="context">Context.</param>
        protected void RespondFileNotFound(HttpContextBase context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.End();
        }

        /// <summary>
        /// Helper method used to Respond to the request
        /// that an error occurred in processing the request.
        /// </summary>
        /// <param name="context">Context.</param>
        protected void RespondInternalError(HttpContextBase context)
        {
            // It's really too bad that StatusCode property
            // is not of type HttpStatusCode.
            context.Response.StatusCode =
                (int)HttpStatusCode.InternalServerError;
            context.Response.End();
        }

        /// <summary>
        /// Helper method used to Respond to the request
        /// that the request in attempting to access a resource
        /// that the user does not have access to.
        /// </summary>
        /// <param name="context">Context.</param>
        protected void RespondForbidden(HttpContextBase context)
        {
            context.Response.StatusCode
                = (int)HttpStatusCode.Forbidden;
            context.Response.End();
        }
    }

}
