using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Tests
{
  [TestClass]
  public class SignatureTests
  {
    static AssemblyBytes assm;

    [ClassInitialize()]
    public static void MyClassInitialize(TestContext testContext) {
      var assembled = AssemblyBytesTests.Assemble("Signatures.il");
      var openFile = File.OpenRead(assembled);
      assm = new AssemblyBytes(openFile);
    }

    [TestMethod]
    public void GenericSig() {
      var actual = GetExtendsTypeSpecSig("ExtendsDict");
      Assert.AreEqual("Generic Class System.Collections.Generic.Dictionary`2<Int4, String>", actual);
    }

    static string GetExtendsTypeSpecSig(string type) {
      var def = assm.TildeStream.TypeDefs.Where(t => t.TypeName.NodeValue == type).Single();
      var spec = (TypeSpec)def.Extends.Link;
      return spec.Signature.NodeValue;
    }
  }
}
