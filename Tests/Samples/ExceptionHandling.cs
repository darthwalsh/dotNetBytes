using System;
using System.Runtime.CompilerServices;

public static class Exceptions
{
  static void Main(string[] args) {
    RuntimeHelpers.PrepareConstrainedRegions();
    try {
    } finally {
      "CER".GetHashCode();
    }

    try {

    } catch (InvalidOperationException) when (new object() == new object()) {
      throw;
    } catch (Exception e) {
      e.GetHashCode();
    }

    "outside".GetHashCode();
    try {
      "inside1".GetHashCode();
      try {
        "inside2".GetHashCode();
        try {
          "inside3".GetHashCode();
        } catch { }
      } catch { }
    } catch { }
  }
}