using System;
using System.Collections.Generic;

// Thread-safe globals for parsing the assembly.
//TODO(cleanup) pass an instance with the Read() method
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

  public static void Reset() {
    instance = null;
  }

  public int MethodCount { get; set; }
  public int ResourceEntryCount { get; set; }

  SetOnce<StringHeap> stringHeap = new SetOnce<StringHeap>();
  public StringHeap StringHeap { get { return stringHeap.Value; } set { stringHeap.Value = value; } }

  SetOnce<UserStringHeap> userStringHeap = new SetOnce<UserStringHeap>();
  public UserStringHeap UserStringHeap { get { return userStringHeap.Value; } set { userStringHeap.Value = value; } }

  SetOnce<BlobHeap> blobHeap = new SetOnce<BlobHeap>();
  public BlobHeap BlobHeap { get { return blobHeap.Value; } set { blobHeap.Value = value; } }

  SetOnce<GuidHeap> guidHeap = new SetOnce<GuidHeap>();
  public GuidHeap GuidHeap { get { return guidHeap.Value; } set { guidHeap.Value = value; } }

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

