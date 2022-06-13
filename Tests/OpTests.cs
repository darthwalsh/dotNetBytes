using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tests
{
  [TestClass]
  public class OpTests
  {
    static AssemblyBytes assm;

    [ClassInitialize()]
    public static void MyClassInitialize(TestContext testContext) {
      var assembled = AssemblyBytesTests.Assemble("BrokenMethods.il");
      var openFile = File.OpenRead(assembled);
      assm = new AssemblyBytes(openFile);
      AssemblyBytesTests.DumpJson(assembled, assm);
    }

    [TestMethod]
    public void WorkingControlFlow() {
      Assert.AreEqual(@"ldarg.0 (Stack: 0 -> 1)
switch (4, 2) (Stack: 1 -> 0)
br.s 0x4 (Stack: 0 -> 0)
ldc.i4.0 (Stack: 0 -> 1)
ret (Stack: 1 -> 0)
ldc.i4.1 (Stack: 0 -> 1)
ret (Stack: 1 -> 0)
ldc.i4.m1 (Stack: 0 -> 1)
ret (Stack: 1 -> 0)", OpDesc("WorkingControlFlow"));

      GetMethod("WorkingControlFlow").CilOps.CallBack(n => Assert.AreEqual(0, n.Errors.Count));
    }

    [TestMethod]
    public void BrokenBranch() {
      var (i, s) = OpErrors("BrokenBranch");
      Assert.AreEqual(0, i);
      Assert.IsTrue(s.StartsWith("Branch target "));
    }

    [TestMethod]
    public void WithinBranch() {
      var (i, s) = OpErrors("WithinBranch");
      Assert.AreEqual(0, i);
      Assert.IsTrue(s.StartsWith("Branch target "));
    }

    [TestMethod]
    public void BrokenSwitch() {
      var (i, s) = OpErrors("BrokenSwitch");
      Assert.AreEqual(1, i);
      Assert.IsTrue(s.StartsWith("Branch target "));
    }

    [TestMethod]
    public void StackUnderflow() {
      Assert.AreEqual((0, "Stack underflow!"), OpErrors("StackUnderflow"));
    }

    [TestMethod]
    public void StackOverMax() {
      Assert.AreEqual((1, "Stack is 2 > maxstack 1"), OpErrors("StackOverMax"));
    }

    [TestMethod]
    public void RetEmpty() {
      Assert.AreEqual((0, "stack should be 1, but stack is 0"), OpErrors("RetEmpty"));
    }

    [TestMethod]
    public void RetExtra() {
      Assert.AreEqual((1, "return type is void, but stack is 1"), OpErrors("RetExtra"));
    }

    static string OpDesc(string methodName) => string.Join("\n", 
      GetMethod(methodName).CilOps.Children.Select(n => n.Description));

    static (int i, string error) OpErrors(string methodName) => GetMethod(methodName)
      .CilOps.Children
      .Select((n, i) => (i, n.Errors.SingleOrDefault()))
      .Where(o => o.Item2 != null)
      .Single();

    static Method GetMethod(string methodName) {
      var def = assm.TildeStream.MethodDefs.Where(m => m.Name.NodeValue == methodName).Single();
      return (Method)def.Children.Where(n => n.NodeName == "RVA").Single().Link;
    }
  }
}
