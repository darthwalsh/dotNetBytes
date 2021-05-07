using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
  [TestClass]
  public class CompressionTests
  {
    [TestMethod]
    public void DecompressUnsignedTest() {
      AssertDecompressUnsigned(0x03, "03");
      AssertDecompressUnsigned(0x7F, "7F");
      AssertDecompressUnsigned(0x80, "8080");
      AssertDecompressUnsigned(0x2E57, "AE57");
      AssertDecompressUnsigned(0x3FFF, "BFFF");
      AssertDecompressUnsigned(0x4000, "C0004000");
      AssertDecompressUnsigned(0x1FFFFFFF, "DFFFFFFF");
    }

    static void AssertDecompressUnsigned(uint expected, string hex) {
      Assert.AreEqual(expected, GetData(hex).DecompressUnsigned(0));
    }

    [TestMethod]
    public void DecompressSignedTest() {
      AssertDecompressSigned(3, "06");
      AssertDecompressSigned(-3, "7B");
      AssertDecompressSigned(64, "8080");
      AssertDecompressSigned(-64, "01");
      AssertDecompressSigned(8192, "C0004000");
      AssertDecompressSigned(-8192, "8001");
      AssertDecompressSigned(268435455, "DFFFFFFE");
      AssertDecompressSigned(-268435456, "C0000001");
    }

    static void AssertDecompressSigned(int expected, string hex) {
      Assert.AreEqual(expected, GetData(hex).DecompressSigned(0));
    }

    static byte[] GetData(string hex) {
      Assert.AreEqual(0, hex.Length % 2);

      var data = new byte[hex.Length / 2];
      for (var i = 0; i < data.Length; ++i) {
        data[i] = (byte)(GetNibble(hex[2 * i]) << 4);
        data[i] += (byte)(GetNibble(hex[2 * i + 1]));
      }
      return data;
    }

    static int GetNibble(char c) {
      if ('0' <= c && c <= '9')
        return c - '0';
      if ('A' <= c && c <= 'F')
        return c - 'A' + 10;
      throw new ArgumentException(c.ToString());
    }
  }
}