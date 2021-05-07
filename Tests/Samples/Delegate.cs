public static class Delegate
{
  delegate int Transformer(int x);

  static int Transform(int x) => x;

  static void Main(string[] args) {
    var _ = new Transformer(Transform);
    _ = Transform;
    _ = delegate (int y) { return -y; };
    _ = y => 2 * y;
  }
}