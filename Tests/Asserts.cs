using System;

namespace Tests
{
    static class Asserts
    {
        public static void IsLessThanOrEqual(int a, int b)
        {
            if (a <= b) return;

            throw new Exception($"Unexpected {a} < {b}");
        }
    }
}