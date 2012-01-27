using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using NCombo;
using System.Web;
using Moq;
using System.Collections.Specialized;
using System.Net;
using System.IO;

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
            mockServer.Setup(m => m.MapPath(It.Is<string>(s => s.StartsWith("~/yui/cache/"))))
                      .Returns<string>(s => @"..\..\cacheDir\" + Path.GetFileName(s));
            mockServer.Setup(m => m.MapPath("~/yui/"))
                      .Returns(@"..\..\test\");

            // set up cache dir
            if (!Directory.Exists(@"..\..\cacheDir"))
            {
                Directory.CreateDirectory(@"..\..\cacheDir");
            }

            // make sure that no files are hanging around from a prior test run
            foreach(string file in Directory.EnumerateFiles(@"..\..\cacheDir")) {
                File.Delete(file);
            }

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
                      .Returns(@"..\..\test\scripts\test0.js");

            handle();

            mockResponse.VerifySet(m => m.StatusCode = 404);
        }

        [Test]
        public void Handler_DoesNotAllow_FilesNotInWhitelist()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?/web.config"));
            mockServer.Setup(m => m.MapPath("/yui/web.config"))
                      .Returns(@"..\..\test\web.config");

            handle();

            mockResponse.VerifySet(m => m.StatusCode = 404);
        }

        [Test]
        public void Handler_DoesNotAllow_RequestsOutisdeRootDir()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?folder/../../packages.config"));
            mockServer.Setup(m => m.MapPath("/packages.config"))
                      .Returns(@"..\..\packages.config");

            handle();

            mockResponse.VerifySet(m => m.StatusCode = 404);
        }

        [Test]
        public void Handler_DoesNotThrow404_ForATrailingAmpersand()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/test1.js&file/test2.js&"));
            mockServer.Setup(m => m.MapPath("/yui/file/test1.js"))
                      .Returns(@"..\..\test\scripts\test1.js");
            mockServer.Setup(m => m.MapPath("/yui/file/test2.js"))
                      .Returns(@"..\..\test\scripts\test2.js");

            handle();

            mockResponse.VerifySet((m => m.StatusCode = 404), Times.Never());

        }

        [Test]
        public void Handler_ServesJSAsProperMimeType()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/test1.js&file/test2.js&"));
            mockServer.Setup(m => m.MapPath("/yui/file/test1.js"))
                      .Returns(@"..\..\test\scripts\test1.js");
            mockServer.Setup(m => m.MapPath("/yui/file/test2.js"))
                      .Returns(@"..\..\test\scripts\test2.js");
            

            handle();

            mockResponse.VerifySet(m => m.ContentType = "application/x-javascript");
        }

        [Test]
        public void Handler_ServesCSSAsProperMimeType()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/test1.css&file/test2.css"));
            mockServer.Setup(m => m.MapPath("/yui/file/test1.css"))
                      .Returns(@"..\..\test\scripts\test1.css");

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
                      .Returns(@"..\..\test\styles\relPaths.css");

            string outCss = "";
            mockResponse.Setup(m => m.WriteFile(It.IsAny<string>()))
                .Callback<string>(s => { outCss = File.ReadAllText(s); });

            handle();

            //for debugging
            //Console.Write(outCss);

            Assert.Contains(outCss, "/yui/ver/build/module/../../../../assets/skins/sam/relPath.png",
                "fixed relative path not found");
            Assert.Contains(outCss, "/yui/ver/build/module/assets/skins/sam/subDir.png",
                "fixed subdir path not found");
            Assert.Contains(outCss, "/yui/ver/build/module/dir.png",
                "fixed file path not found");
            Assert.Contains(outCss, "src='/yui/ver/build/module/../Images/alpha.png'",
                "fixed alpha path not found");                
        }

        [Test]
        public void Handler_FixesSubdirCssPathsCorrectly()
        {
            string root = "/root/";
            string css = @".{background-image:url(Images/img.png);_background-image:url(Images/img.gif);}";

            string outCss = handler.fixupCss(root, css);
              
            Assert.Contains(outCss, "/root/Images/img.png");
            Assert.Contains(outCss, "/root/Images/img.gif");
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

        /// <summary>
        /// Invalid query string should return a 500
        /// </summary>
        [Test]
        public void Handler_ValidatesQueryString()
        {
            mockRequest.Setup(m => m.QueryString)
                .Returns(new NameValueCollection());

            Assert.IsFalse(handler.ValidateParameters(mockContext.Object));
        }
    }
}
