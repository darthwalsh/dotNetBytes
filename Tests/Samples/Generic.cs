interface I<in X, out Y> where X : new()
{

}

class C<Z> : I<int, double>
{
  static void M<T>(Z z) where T : C<T>, new() {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
    var t = z as T;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
    _ = new C<T>();
  }
}

public static class Generic
{
  static void Main(string[] args) {
    foreach (var _ in new System.Collections.Generic.Dictionary<char, string>()) {
    }
  }
}