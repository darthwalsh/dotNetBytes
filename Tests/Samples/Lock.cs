class Lock
{
    static volatile int x;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
    void M()
    {

    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
    static void Main(string[] args)
    {
        System.Threading.Interlocked.Increment(ref x);

        lock (new object())
        {

        }
    }
}