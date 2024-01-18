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
      var actual = MethodLines("Dict");
      Assert.AreEqual("Generic class System.Collections.Generic.Dictionary`2<int, string>", actual);
    }

    [TestMethod]
    public void ArrayTypeSpec() {
      Assert.AreEqual("int[3]", MethodLines("Array1"));
      Assert.AreEqual("int[,,,,,,]", MethodLines("Array2"));
      Assert.AreEqual("int[4,3,,,,]", MethodLines("Array3"));
      Assert.AreEqual("int[1...2,6...8]", MethodLines("Array4"));
      Assert.AreEqual("int[5,3...5,,]", MethodLines("Array5"));
    }

    [TestMethod]
    public void ArrayNestedTypeSpec() {
      Assert.AreEqual("int[5...8][,]", MethodLines("ArrayNested"));
    }

    [TestMethod]
    public void ArrayNegativeTypeSpec() {
      Assert.AreEqual("int[,-5...-2,-5...,-5...0]", MethodLines("ArrayNegative"));
    }

    [TestMethod]
    public void ClassTypeSpec() {
      Assert.AreEqual("class SignatureTests", MethodLines("Class"));
    }

    [TestMethod]
    public void ValueTypeTypeSpec() {
      Assert.AreEqual("valuetype System.ValueTuple", MethodLines("ValueType"));
    }

    [TestMethod]
    public void ModObjectSpec() {
      var actual = MethodLines("ModObject");
      Assert.AreEqual("object modopt (System.Text.StringBuilder)", actual);
    }

    [TestMethod]
    public void ModObjectArraySpec() {
      var actual = MethodLines("ModObjectArray");
      Assert.AreEqual("object modopt (string)[]", actual);
    }

    [TestMethod]
    public void ModArrayObjectSpec() {
      var actual = MethodLines("ModArrayObject");
      Assert.AreEqual("object[] modopt (string)", actual);
    }

    [TestMethod]
    public void SzArrayTypeSpec() {
      Assert.AreEqual("int[]", MethodLines("SzArray"));
      Assert.AreEqual("class SignatureTests[]", MethodLines("SzArrayClass"));
    }

    [TestMethod]
    public void PtrTypeSpec() {
      Assert.AreEqual("int*", MethodLines("Ptr"));
      Assert.AreEqual("void*", MethodLines("VoidPtr"));
      Assert.AreEqual("int**", MethodLines("PtrPtr"));
      Assert.AreEqual("void modopt (char)*", MethodLines("PtrMod"));
    }

    [TestMethod]
    public void ModTypeSpec() {
      Assert.AreEqual("int modopt (char)[]", MethodLines("Mod"));
    }

    [TestMethod]
    public void ModsTypeSpec() {
      var actual = MethodLines("Mods");
      Assert.AreEqual("int modopt (char) modreq (int) modreq (short) modopt (short)[]", actual);
    }

    [TestMethod]
    public void FnptrTypeSpec() {
      var actual =  MethodLines("Fnptr");
      Lines.AreEqual(@"method static void *(int)
method explicitthis char *()
method void *()
method static vararg void *(method static fastcall void *(char, uint), ...)", actual);
    }

    [TestMethod]
    public void GenTypeSpec() {
      Assert.AreEqual("!!0", MethodLines("GenCast"));
    }

    [TestMethod]
    public void MethodRefDefSig() {
      var actual = MethodLines("StandAloneMethodSigRunner");
      Lines.AreEqual(@"static int AddTen(int)
static int (int)
static vararg void VarArgsMethod(int, ...)
static vararg void VarArgsMethod(int, ..., int)
static vararg void VarArgsMethod(int, ..., uint)
static vararg void (int, ..., uint)", actual);
    }

    [TestMethod]
    public void FieldSig() {
      var fields = assm.TildeStream.Fields.ToDictionary(f => f.Name.NodeValue);
      var fieldSigs = fields.ToDictionary(f => f.Key, f => f.Value.Signature.Value.NodeValue);

      Assert.AreEqual("int", fieldSigs["pointCount"]);
      Assert.AreEqual("int", fieldSigs["fld"]);
      Assert.AreEqual("int modopt (char) modreq (int)", fieldSigs["customized"]);

      var fldConst = assm.TildeStream.Constants.Where(c => c.Parent.NodeValue == "int fld").Single();
      Assert.AreEqual("{0x2A, 0x0, 0x0, 0x0}", fldConst.Value.NodeValue);

      var rvas = assm.TildeStream.FieldRVAs.Select(r => r.Field.Value.Name.NodeValue).ToHashSet();
      foreach (var name in "classCount globalCount".Split(' ')) {
        var field = fields[name];
        Assert.IsTrue(field.Flags.Flags.HasFlag(FieldAttributes.AdditionalFlags.HasFieldRVA));
        Assert.IsTrue(rvas.Contains(name));
      }
    }

    [TestMethod]
    public void FieldSigOps() {
      Lines.AreEqual(@"int classCount
int* classCount", MethodLines("FieldSigRunner"));
    }

    [TestMethod]
    public void MethodSpecs() {
      Lines.AreEqual(@"static void Gen<int, string>(char)
static void Gen<string, class MethodSpecsTests>(char)", MethodLines("MethodSpecs"));
    }

    [TestMethod]
    public void LocalVarSig() {
      Assert.AreEqual("char, long", GetMethod("SimpleVar").FatFormat.LocalVarSigTok.NodeValue);
      Assert.AreEqual("string, object", GetMethod("ClassVar").FatFormat.LocalVarSigTok.NodeValue);
      Assert.AreEqual("char& pinned, string pinned", GetMethod("PinnedVar").FatFormat.LocalVarSigTok.NodeValue);
      Assert.AreEqual("long&", GetMethod("ByRefVar").FatFormat.LocalVarSigTok.NodeValue);
      Assert.AreEqual("long modopt (string)", GetMethod("ModOptVar").FatFormat.LocalVarSigTok.NodeValue);
      Assert.AreEqual("typedref, IntPtr", GetMethod("TypeRefVar").FatFormat.LocalVarSigTok.NodeValue);
    }

    static string MethodLines(string methodName) => string.Join("\n", MethodLinks(methodName).Select(n => n.NodeValue));

    static List<CodeNode> MethodLinks(string methodName) {
      var method = GetMethod(methodName);
      var links = new List<CodeNode>();
      method.CallBack(node => { if (node.Link != null) links.Add(node.Link); });
      return links;
    }

    static Method GetMethod(string methodName) {
      var def = assm.TildeStream.MethodDefs.Where(m => m.Name.NodeValue == methodName).Single();
      return (Method)def.Child(nameof(def.RVA)).Link;
    }
  }

  static class Lines {
    public static void AreEqual(string x, string y) {
      // Don't rely on the source code EOL
      Assert.AreEqual(x.Replace("\r", ""), y.Replace("\r", ""));
    }
  }
}
