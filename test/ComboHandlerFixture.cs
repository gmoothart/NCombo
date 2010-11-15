using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using NCombo;

namespace NComboTest
{
    [TestFixture]
    public class ComboHandlerFixture
    {
        ComboHandler ch;
        
        [SetUp]
        public void Setup()
        {
            ch = new NCombo.ComboHandler();


        }

        [Test]
        public void Handler_Throws404_IfAFileIsNotFound()
        {
            Assert.Fail();
        }

        [Test]
        public void Handler_DoesNotThrow404_ForATrailingAmpersand()
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
