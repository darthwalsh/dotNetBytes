using System;
using System.Linq;
using System.IO;

public class AssemblyBytes
{
  static void DoNothing(object o) {
  }

  public AssemblyBytes(Stream s) {
    this.Stream = s;

    FileFormat = new FileFormat { Bytes = this };
    FileFormat.NodeName = "FileFormat";
    try {
      FileFormat.Read();
    } catch (EndOfStreamException e) {
      FileFormat.Errors.Add(e.ToString());
    }

    // Reading custom values from the blob heap is lazy, mutating the heap children.
    // Touch all entries now so sorting sees them before JSON serialization.
    FileFormat.CallBack(n => {
      DoNothing(n.Description);
      DoNothing(n.NodeValue);
    });

    FileFormat.CallBack(n => {
      n.Children = n.Children.OrderBy(c => c.Start).ToList();
    });

    FileFormat.CallBack(n => {
      if (n.End > s.Length) n.Errors.Add($"End was set beyond byte end to {n.End}");
      if (n.Start < 0) n.Errors.Add($"Start was set to {n.Start}");
    });

    FileFormat.AssignPath();
  }

  internal AssemblyBytes(Stream s, AssemblyBytes forMocking) {
    this.Stream = s;
  }

  internal FileFormat FileFormat { get; private set; }
  public CodeNode Node => FileFormat;

  public Stream Stream { get; }
  internal Section CLIHeaderSection { get; set; }

  internal StringHeap StringHeap => CLIHeaderSection.StringHeap;
  internal UserStringHeap UserStringHeap => CLIHeaderSection.UserStringHeap;
  internal BlobHeap BlobHeap => CLIHeaderSection.BlobHeap;
  internal GuidHeap GuidHeap => CLIHeaderSection.GuidHeap;
  internal TildeStream TildeStream => CLIHeaderSection.TildeStream;

  CodeNode pendingLink;
  public CodeNode PendingLink {
    get {
      return pendingLink;
    }
    internal set {
      if (PendingLink != null && value != null) throw new InvalidOperationException();
      pendingLink = value;
    }
  }

  public T Read<T>() where T : struct => Stream.ReadStruct<T>();
  public T ReadClass<T>() where T : CodeNode, new() {
    // MAYBE https://stackoverflow.com/questions/2974519/generic-constraints-where-t-struct-and-where-t-class
    var t = new T { Bytes = this };
    t.Read();
    return t;
  }

  public T Peek<T>() where T : struct {
    using (TempReposition()) {
      return Read<T>();
    }
  }

  public IDisposable TempReposition(long pos = -1) {
    var orig = new ResetPos(Stream);
    if (pos >= 0) Stream.Position = pos;
    return orig;
  }

  class ResetPos : IDisposable
  {
    Stream stream;
    long origPos;

    public ResetPos(Stream s) {
      this.stream = s;
      this.origPos = s.Position;
    }

    public void Dispose() {
      stream.Position = origPos;
    }
  }
}
