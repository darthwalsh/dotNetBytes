public static class Delegate
{
    delegate int Transformer(int x);

    static int Transform(int x)
    {
        return x;
    }

    static void Main(string[] args)
    {
        var t = new Transformer(Transform);
        t = Transform;
        t = delegate (int y) { return -y; };
        t = y => 2 * y;
    }
}