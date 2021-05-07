using System.Runtime.CompilerServices;
using System.Threading;

class Lock
{
  static volatile int x;

  [MethodImpl(MethodImplOptions.Synchronized)]
  void M() {

  }

  [MethodImpl(MethodImplOptions.Synchronized)]
  static void Main() {
    Interlocked.Increment(ref x);

    lock (new object()) {

    }
  }
}