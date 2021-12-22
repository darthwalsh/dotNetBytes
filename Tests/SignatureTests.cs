using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
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
    public void GenericTypeSpec() {
      var actual = GetTypeSpec("Dict");
      Assert.AreEqual("Generic Class System.Collections.Generic.Dictionary`2<Int4, String>", actual);
    }

    // [TestMethod]
    // public void ArrayTypeSpec() {
    //   var actual = GetTypeSpec("Array");
    //   Assert.AreEqual("Generic Class System.Collections.Generic.Dictionary`2<Int4, String>", actual);
    // }

    [TestMethod]
    public void SzArrayTypeSpec() {
      var actual = GetTypeSpec("SzArray");
      Assert.AreEqual("Int4[]", actual);
    }

    [TestMethod]
    public void ModTypeSpec() {
      var actual = GetTypeSpec("Mod");
      Assert.AreEqual("modopt (Char) Int4[]", actual);
    }

    [TestMethod]
    public void ModsTypeSpec() {
      var actual = GetTypeSpec("Mods");
      Assert.AreEqual("modopt (Char) modreq (Int4) modreq (Int2) modopt (Int2) Int4[]", actual);
    }

    static string GetTypeSpec(string methodName) {
      var def = assm.TildeStream.MethodDefs.Where(m => m.Name.NodeValue == methodName).Single();
      var method = (Method)def.Children.Where(n => n.NodeName == "RVA").Single().Link;
      var links = new List<CodeNode>();
      method.CallBack(node => { if (node.Link != null) links.Add(node.Link); });
      var spec = (TypeSpec)links.Single();
      return spec.Signature.NodeValue;
    }
  }
}
