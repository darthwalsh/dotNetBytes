using System;

namespace Tests
{
  static class Asserts
  {
    public static void IsLessThanOrEqual(int a, int b) {
      if (a <= b) return;

      throw new Exception($"Unexpected 0x{a:X} < 0x{b:X}");
    }
  }
}