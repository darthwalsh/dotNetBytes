using System;
using System.Runtime.InteropServices;
using System.Text;

public static class PInvoke
{
    struct bytes
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public int[] vals;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct union
    {
        [FieldOffset(0)]
        public int i;
        [FieldOffset(0)]
        public double d;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2, Size = 8)]
    public struct sparse
    {
        public int i;
        public double d;
    }

    [DllImport("something.dll", SetLastError = true, PreserveSig = true)]
    static extern int Blah(
        [MarshalAs(UnmanagedType.I4)]
        int i,
        string s,
        union u,
        sparse sp,
        [In, Out] string inout,
        StringBuilder sb,
        IntPtr ip,
        out bool ob,
        ref bytes bs);

    static void Main(string[] args) {
        var o = new bytes { vals = new int[] { 0 } };
    }
}