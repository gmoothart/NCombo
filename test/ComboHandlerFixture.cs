using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using NCombo;
using System.Web;
using Moq;
using System.Collections.Specialized;

namespace NComboTest
{
    [TestFixture]
    public class ComboHandlerFixture
    {
        ComboHandler handler;
        Mock<HttpContextBase> mockContext;
        Mock<HttpRequestBase> mockRequest;
        Mock<HttpResponseBase> mockResponse;
        Mock<HttpServerUtilityBase> mockServer;

        
        [SetUp]
        public void Setup()
        {
            handler = new NCombo.ComboHandler();

            mockRequest = new Mock<HttpRequestBase>();
            mockRequest.Setup(r => r.Headers).Returns(new NameValueCollection());

            mockResponse = new Mock<HttpResponseBase>();
            mockResponse.Setup(r => r.WriteFile(It.IsAny<string>()));
            mockResponse.Setup(r => r.Write('\n'));

            //
            // Response.End() interupts execution in a server context. To 
            // simulate the same thing in our tests, we have it throw.
            //
            mockResponse.Setup(r => r.End())
                        .Throws(new ApplicationException("all done here"));

            mockServer = new Mock<HttpServerUtilityBase>();

            mockContext = new Mock<HttpContextBase>();
            mockContext.Setup(ctxt => ctxt.Request).Returns(mockRequest.Object);
            mockContext.Setup(ctxt => ctxt.Response).Returns(mockResponse.Object);
            mockContext.Setup(ctxt => ctxt.Server).Returns(mockServer.Object);
        }

        private bool isResponseEnd(Exception ex)
        {
            return (ex is ApplicationException && ex.Message == "all done here");
        }

        private void handle()
        {
            try
            {
                handler.HandleRequest(mockContext.Object);
            }
            catch (ApplicationException ex)
            {
                // If this is a Response.End() exception, we can safely ignore.
                // Otherwise, throw.
                if (!isResponseEnd(ex)) throw;
            }
        }

        [Test]
        public void Handler_Throws404_IfAFileIsNotFound()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/not/found.js"));
            mockServer.Setup(m => m.MapPath("~/yui/file/not/found.js"))
                      .Returns(@"..\..\testScripts\test0.js");

            handle();

            mockResponse.VerifySet(m => m.StatusCode = 404);
        }

        [Test]
        public void Handler_DoesNotThrow404_ForATrailingAmpersand()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/test1.js&file/test2.js&"));
            mockServer.Setup(m => m.MapPath("~/yui/file/test1.js"))
                      .Returns(@"..\..\testScripts\test1.js");
            mockServer.Setup(m => m.MapPath("~/yui/file/test2.js"))
                      .Returns(@"..\..\testScripts\test2.js");

            handle();

            mockResponse.VerifySet((m => m.StatusCode = 404), Times.Never());

        }

        [Test]
        public void Handler_ServesJSAsProperMimeType()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/test1.js&file/test2.js&"));
            mockServer.Setup(m => m.MapPath("~/yui/file/test1.js"))
                      .Returns(@"..\..\testScripts\test1.js");

            handle();

            mockResponse.VerifySet(m => m.ContentType = "application/x-javascript");
        }

        [Test]
        public void Handler_ServesCSSAsProperMimeType()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/test1.css&file/test2.css"));
            mockServer.Setup(m => m.MapPath("~/yui/file/test1.css"))
                      .Returns(@"..\..\testScripts\test1.css");

            handle();

            mockResponse.VerifySet(m => m.ContentType = "text/css");
        }
    }
}
