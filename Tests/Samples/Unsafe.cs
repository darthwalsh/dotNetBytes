public static unsafe class Unsafe
{
    static void Main()
    {
        var buffer = new byte[16];

        fixed (byte* p = buffer)
        {
            p->GetHashCode();
            (*p).GetHashCode();
        }
    }
}