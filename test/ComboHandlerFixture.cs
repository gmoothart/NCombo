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
            mockResponse.Setup(r => r.End()).Throws(new ApplicationException("all done here"));

            mockServer = new Mock<HttpServerUtilityBase>();

            mockContext = new Mock<HttpContextBase>();
            mockContext.Setup(ctxt => ctxt.Request).Returns(mockRequest.Object);
            mockContext.Setup(ctxt => ctxt.Response).Returns(mockResponse.Object);
            mockContext.Setup(ctxt => ctxt.Server).Returns(mockServer.Object);
        }

        [Test]
        public void Handler_Throws404_IfAFileIsNotFound()
        {
            mockRequest.Setup(m => m.Url)
                       .Returns(new Uri("http://app/ncombo.axd?file/not/found.js"));
            mockServer.Setup(m => m.MapPath("~/yui/file/not/found.js"))
                      .Returns(@"c:\foo\bar\baz.txt");

            try {
                handler.HandleRequest(mockContext.Object);
            }
            catch (ApplicationException ex)
            {
                // If this is a Response.End() exception, we can safely ignore.
                // Otherwise, thow.
                if (ex.Message != "all done here") throw;
            }

            mockResponse.VerifySet(m => m.StatusCode = 404);
        }

        [Test]
        public void Handler_DoesNotThrow404_ForATrailingAmpersand()
        {
            Assert.Fail();

        }

        [Test]
        public void Handler_Returns200OnSuccess()
        {
            Assert.Fail();
        }

        [Test]
        public void Handler_ServesJSAsProperMimeType()
        {
            Assert.Fail();
        }

        [Test]
        public void Handler_ServesCSSAsProperMimeType()
        {
            Assert.Fail();
        }
    }
}
