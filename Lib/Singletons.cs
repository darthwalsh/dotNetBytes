using System;

// Thread-safe globals for parsing th assembly.
//TODO(solonode) pass an instance with the Read() method
class Singletons
{
  [ThreadStatic]
  static Singletons instance;

  public static Singletons Instance {
    get {
      if (instance == null) {
        instance = new Singletons();
      }
      return instance;
    }
  }

  public int MethodCount { get; set; }

  SetOnce<TildeStream> tildeStream = new SetOnce<TildeStream>();
  public TildeStream TildeStream { get { return tildeStream.Value; } set { tildeStream.Value = value; } }

  class SetOnce<T> where T : class
  {
    T t;
    public T Value {
      get {
        return t;
      }
      set {
        if (t != null) throw new InvalidOperationException();
        t = value;
      }
    }
  }
}

