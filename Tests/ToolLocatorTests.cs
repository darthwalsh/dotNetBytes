using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
  [TestClass]
  public class ToolLocatorTests
  {
    [TestMethod]
    [DataRow("10.0.9,10.0.10", "10.0.10", DisplayName = "numeric, not string, order")]
    [DataRow("10.0.100-rc.1.25451.107,10.0.100", "10.0.100", DisplayName = "release beats its prerelease")]
    [DataRow("10.0.100,10.0.101-rc.1", "10.0.101-rc.1", DisplayName = "prerelease of a higher version wins")]
    public void NewestVersion(string versions, string expected) =>
      Assert.AreEqual(expected, ToolLocator.NewestVersion(versions.Split(',')));

    [TestMethod]
    public void NewestSdkSkipsOtherMajorsAndOrdersNumerically() {
      // CRLF like Windows; mac uses LF. Order is as printed by dotnet: ascending
      var output = "8.0.404 [/usr/local/share/dotnet/sdk]\r\n10.0.100 [/usr/local/share/dotnet/sdk]\r\n10.0.99 [/other/sdk]\r\n11.0.100 [/usr/local/share/dotnet/sdk]\r\n";

      Assert.AreEqual(("10.0.100", "/usr/local/share/dotnet/sdk"), ToolLocator.NewestSdk(output, 10));
    }

    [TestMethod]
    public void NewestSdkReturnsRootOfNewest() {
      var output = "10.0.100 [/old/sdk]\n10.0.200 [/new/sdk]\n";

      Assert.AreEqual(("10.0.200", "/new/sdk"), ToolLocator.NewestSdk(output, 10));
    }

    [TestMethod]
    public void NewestSdkThrowsWithoutMatchingMajor() {
      var ex = Assert.ThrowsExactly<InvalidOperationException>(
        () => ToolLocator.NewestSdk("8.0.404 [/usr/local/share/dotnet/sdk]\n", 10));

      Assert.AreEqual(
        "No .NET 10 SDK in `dotnet --list-sdks` output: 8.0.404 [/usr/local/share/dotnet/sdk]\n",
        ex.Message);
    }

    [TestMethod]
    public void VsVersionDirsNewestFirst() {
      var root = Path.Combine("Program Files", "Microsoft Visual Studio");
      string[] dirs = ["2019", "Installer", "2022", "18", "Shared", "2017"];

      var sorted = ToolLocator.VsVersionDirsNewestFirst(dirs.Select(d => Path.Combine(root, d)));

      // 18 is VS 2026, newer than every year-named folder
      CollectionAssert.AreEqual(
        new[] { "18", "2022", "2019", "2017" }.Select(d => Path.Combine(root, d)).ToArray(),
        sorted.ToArray());
    }
  }
}
