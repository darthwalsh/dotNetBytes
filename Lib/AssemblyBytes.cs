using System;
using System.Linq;
using System.IO;

public class AssemblyBytes
{
  public AssemblyBytes(Stream s) {
    this.Stream = s;

    FileFormat = new FileFormat { Bytes = this };
    FileFormat.NodeName = "FileFormat";
    try {
      FileFormat.Read();
    }
    catch (EndOfStreamException e) {
      FileFormat.Errors.Add(e.ToString());
    }

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

  public T Read<T>() where T : struct => Stream.ReadStruct<T>();
  public T ReadClass<T>() where T : CodeNode, new() {
    var t = new T { Bytes = this };
    t.Read();
    return t;
  }
}
