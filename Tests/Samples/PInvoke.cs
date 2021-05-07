using System;
using System.Runtime.InteropServices;
using System.Text;

public static class PInvoke
{
  struct Bytes
  {
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public int[] Vals;
  }

  [StructLayout(LayoutKind.Explicit)]
  struct Union
  {
    [FieldOffset(0)]
    public int I;
    [FieldOffset(0)]
    public double D;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 2, Size = 8)]
  struct Sparse
  {
    public int I;
    public double D;
  }

  [DllImport("something.dll", SetLastError = true, PreserveSig = true, CharSet = CharSet.Unicode)]
  static extern int Blah(
      [MarshalAs(UnmanagedType.I4)]
        int i,
      string s,
      Union u,
      Sparse sp,
      [In, Out] string inout,
      StringBuilder sb,
      IntPtr ip,
      out bool ob,
      ref Bytes bs);

  static void Main() {
    var _ = new Bytes { Vals = new int[] { 0 } };
  }
}