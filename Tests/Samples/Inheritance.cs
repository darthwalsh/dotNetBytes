interface IX
{
  int X();
}
interface IX2
{
  int X();
}

abstract class AX : IX
{
  public int X() => 0;

  protected abstract int Y();

  protected virtual int Z() => 1;
}

sealed class CX : AX, IX, IX2
{
  int IX.X() => -1;
  int IX2.X() => -2;

  protected sealed override int Y() => 1;

  protected override int Z() => 2;
}

static class Inheritance
{
  static void Main() {
  }
}