using System;

namespace Tests
{
  static class Asserts
  {
    public static void IsLessThan(int a, int b, string message) {
      if (a < b) return;

      throw new Exception($"{message}: Unexpected 0x{a:X} < 0x{b:X}");
    }

    public static void IsLessThanOrEqual(int a, int b, string message) {
      if (a <= b) return;

      throw new Exception($"{message}: Unexpected 0x{a:X} <= 0x{b:X}");
    }
  }
}
