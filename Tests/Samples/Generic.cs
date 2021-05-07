interface I<in X, out Y> where X : new()
{

}

class C<Z> : I<int, double>
{
    static void M<T>(Z z) where T : C<T>, new() {
        var t = z as T;
        var c = new C<T>();
    }
}

public static class Generic
{
    static void Main(string[] args) {
        foreach (var i in new System.Collections.Generic.Dictionary<char, string>()) {
        }
    }
}