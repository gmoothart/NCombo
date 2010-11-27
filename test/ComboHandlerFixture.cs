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
            mockRequest.Setup(r => r.ApplicationPath).Returns("/");

            mockResponse = new Mock<HttpResponseBase>();
            mockResponse.Setup(r => r.Write(It.IsAny<string>()));

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
            mockServer.Setup(m => m.MapPath("/yui/file/not/found.js"))
                      .Returns(@"..\..\testScripts\test0.js");

            handle();

            mockResponse.VerifySet(m => m.StatusCode = 404);
        }

        [Test]
        public void Handler_DoesNotThrow404_ForATrailingAmpersand()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/test1.js&file/test2.js&"));
            mockServer.Setup(m => m.MapPath("/yui/file/test1.js"))
                      .Returns(@"..\..\testScripts\test1.js");
            mockServer.Setup(m => m.MapPath("/yui/file/test2.js"))
                      .Returns(@"..\..\testScripts\test2.js");

            handle();

            mockResponse.VerifySet((m => m.StatusCode = 404), Times.Never());

        }

        [Test]
        public void Handler_ServesJSAsProperMimeType()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/test1.js&file/test2.js&"));
            mockServer.Setup(m => m.MapPath("/yui/file/test1.js"))
                      .Returns(@"..\..\testScripts\test1.js");

            handle();

            mockResponse.VerifySet(m => m.ContentType = "application/x-javascript");
        }

        [Test]
        public void Handler_ServesCSSAsProperMimeType()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/test1.css&file/test2.css"));
            mockServer.Setup(m => m.MapPath("/yui/file/test1.css"))
                      .Returns(@"..\..\testScripts\test1.css");

            handle();
            
            mockResponse.VerifySet(m => m.ContentType = "text/css");
        }
        
        /// <summary>
        /// Ensure that css asset paths are rewritten for:
        /// 1. relative paths (e.g.) url(../../foo.png)
        /// 2. just filename or subdirs/filename (e.g) url(foo.png), url(foo/foo.png)
        /// 3. AlphaImageLoader relative paths (e.g.) AlphaImageLoader(src='../../foo.png')
        /// </summary>
        [Test]
        public void Handler_FixesCssPaths()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?ver/build/module/relPaths.css"));
            mockServer.Setup(m => m.MapPath("/yui/ver/build/module/relPaths.css"))
                      .Returns(@"..\..\testStylesheets\relPaths.css");

            string outCss = "";
            mockResponse.Setup(m => m.Write(It.IsAny<string>()))
                .Callback<string>(s => { outCss = s; });

            handle();

            //for debugging
            //Console.Write(outCss);

            mockResponse.Verify(m => m.Write(
                It.Is<string>(s => s.Contains("/yui/ver/build/module/../../../../assets/skins/sam/relPath.png"))),
                "fixed relative path not found");
            mockResponse.Verify(m => m.Write(
                It.Is<string>(s => s.Contains("/yui/ver/build/module/assets/skins/sam/subDir.png"))),
                "fixed subdir path not found");
            mockResponse.Verify(m => m.Write(
                It.Is<string>(s => s.Contains("/yui/ver/build/module/dir.png"))),
                "fixed file path not found");
            mockResponse.Verify(m => m.Write(
                It.Is<string>(s => s.Contains("src='/yui/ver/build/module/../Images/alpha.png'"))),
                "fixed alpha path not found");                
        }

        [Test]
        public void Handler_DoesNotTryToFixDataUris()
        {
            string css = 
            @".class {
                background-image: url(data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAYAAAABCAYAAA==)
            }";
            string cssResult = handler.fixupCss("/yui/ver/modulue/assets/dataUri.css", css);

            Assert.Contains(cssResult, "url(data:image/png;base64,iV");
        }

        [Test]
        public void Handler_DoesNotTryToFixAbsoluteUrls()
        {
            string css =
            @".class {
                background-image: url(http://www.google.com/css/foo.png)
            }";
            string cssResult = handler.fixupCss("/yui/ver/modulue/assets/absUrl.css", css);

            Assert.Contains(cssResult, "url(http://www.google.com/css/foo.png)");
        }
    }
}
