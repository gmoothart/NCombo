using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace NComboTest
{
    [TestFixture]
    public class ComboHandlerFixture
    {
        [SetUp]
        public void Setup()
        {
            NCombo.ComboHandler ch = new NCombo.ComboHandler();


        }

        [Test]
        public void Test1()
        {
            Assert.Fail();
        }
    }
}
