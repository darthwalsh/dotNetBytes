using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class AssemblyBytesTests
    {
        [TestMethod]
        public void TwoMethods()
        {
            Verify.Run(File.OpenRead("TwoMethods.exe"));
        }

        [TestMethod]
        public void TwoSameMethods()
        {
            Verify.Run(File.OpenRead("TwoSameMethods.exe"));
        }

        // TODO test parameters, return values
        // TODO Add test to exercise the various NotImplementedExceptions

        [TestMethod]
        public void TestExample()
        {
            Verify.Run(File.OpenRead(@"C:\code\dotNetBytes\view\Program.dat"));
        }
    }
}
