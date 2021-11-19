using System;
using System.Linq;
using System.IO;

public class AssemblyBytes
{
  public AssemblyBytes(Stream s) {
    this.Stream = s;

    FileFormat = new FileFormat { Bytes = this };
    FileFormat.Read();
    FileFormat.NodeName = "FileFormat";
    
    FileFormat.CallBack(n => {
      n.Children = n.Children.OrderBy(c => c.Start).ToList();
    });

    FileFormat.CallBack(n => {
      if (n.End > s.Length) throw new InvalidOperationException($"End was set beyond byte end to {n.End}");
      if (n.Start < 0) throw new InvalidOperationException($"Start was set to {n.Start}");
    });

    FileFormat.AssignPath();
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
}
