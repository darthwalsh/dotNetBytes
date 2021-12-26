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
      AssemblyBytesTests.DumpJson(assembled, assm);
    }

    [TestMethod]
    public void GenericTypeSpec() {
      var actual = GetTypeSpec("Dict");
      Assert.AreEqual("Generic class System.Collections.Generic.Dictionary`2<int, string>", actual);
    }

    [TestMethod]
    public void ArrayTypeSpec() {
      Assert.AreEqual("int[3]", GetTypeSpec("Array1"));
      Assert.AreEqual("int[,,,,,,]", GetTypeSpec("Array2"));
      Assert.AreEqual("int[4,3,,,,]", GetTypeSpec("Array3"));
      Assert.AreEqual("int[1...2,6...8]", GetTypeSpec("Array4"));
      Assert.AreEqual("int[5,3...5,,]", GetTypeSpec("Array5"));
    }

    [TestMethod]
    public void ArrayNestedTypeSpec() {
      Assert.AreEqual("int[5...8][,]", GetTypeSpec("ArrayNested"));
    }

    [TestMethod]
    public void ArrayNegativeTypeSpec() {
      Assert.AreEqual("int[,-5...-2,-5...,-5...0]", GetTypeSpec("ArrayNegative"));
    }

    [TestMethod]
    public void ClassTypeSpec() {
      Assert.AreEqual("class SignatureTests", GetTypeSpec("Class"));
    }

    [TestMethod]
    public void ValueTypeTypeSpec() {
      Assert.AreEqual("valuetype System.ValueTuple", GetTypeSpec("ValueType"));
    }

    [TestMethod]
    public void ModObjectSpec() {
      var actual = GetTypeSpec("ModObject");
      Assert.AreEqual("object modopt (System.Text.StringBuilder)", actual);
    }

    [TestMethod]
    public void ModObjectArraySpec() {
      var actual = GetTypeSpec("ModObjectArray");
      Assert.AreEqual("object modopt (string)[]", actual);
    }

    [TestMethod]
    public void ModArrayObjectSpec() {
      var actual = GetTypeSpec("ModArrayObject");
      Assert.AreEqual("object[] modopt (string)", actual);
    }

    [TestMethod]
    public void SzArrayTypeSpec() {
      Assert.AreEqual("int[]", GetTypeSpec("SzArray"));
    }

    [TestMethod]
    public void PtrTypeSpec() {
      Assert.AreEqual("int*", GetTypeSpec("Ptr"));
      Assert.AreEqual("void*", GetTypeSpec("VoidPtr"));
    }

    [TestMethod]
    public void ModTypeSpec() {
      Assert.AreEqual("int modopt (char)[]", GetTypeSpec("Mod"));
    }

    [TestMethod]
    public void ModsTypeSpec() {
      var actual = GetTypeSpec("Mods");
      Assert.AreEqual("int modopt (char) modreq (int) modreq (short) modopt (short)[]", actual);
    }

    [TestMethod]
    public void GenTypeSpec() {
      Assert.AreEqual("!!0", GetTypeSpec("GenCast"));
    }

    [TestMethod]
    public void MethodSpecs() {
      var links = MethodLinks("MethodSpecs");
      var actual = string.Join("|", links.Cast<MethodSpec>().Select(x => x.NodeValue));
      Assert.AreEqual("Gen<int, string>()|Gen<string, class MethodSpecsTests>()", actual);
    }

    static string GetTypeSpec(string methodName) {
      var links = MethodLinks(methodName);
      var spec = (TypeSpec)links.Single();
      return spec.Signature.NodeValue;
    }

    static List<CodeNode> MethodLinks(string methodName) {
      var def = assm.TildeStream.MethodDefs.Where(m => m.Name.NodeValue == methodName).Single();
      var method = (Method)def.Children.Where(n => n.NodeName == "RVA").Single().Link;
      var links = new List<CodeNode>();
      method.CallBack(node => { if (node.Link != null) links.Add(node.Link); });
      return links;
    }
  }
}
