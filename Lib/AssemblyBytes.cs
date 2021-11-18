using System;
using System.Linq;
using System.IO;

public class AssemblyBytes
{
  internal FileFormat fileFormat;

  public AssemblyBytes(Stream s) {
    this.Stream = s;

    fileFormat = new FileFormat { Bytes = this };
    fileFormat.Read();
    fileFormat.NodeName = "FileFormat";
    
    fileFormat.CallBack(n => {
      if (n.Children.Any()) {
        n.Start = Math.Min(n.Start, n.Children.Min(c => c.Start));
        n.End = Math.Max(n.End, n.Children.Max(c => c.End));
      }
    });
    fileFormat.CallBack(n => {
      n.Children = n.Children.OrderBy(c => c.Start).ToList();
    });

    FindOverLength(s, fileFormat);
    fileFormat.AssignPath();
  }

  static void FindOverLength(Stream s, CodeNode node) {
    node.CallBack(n => {
      if (n.End > s.Length) {
        throw new InvalidOperationException($"End was set beyond byte end to {n.End}");
      }
    });
  }

  public CodeNode Node => fileFormat;

  public Stream Stream { get; }
  internal Section CLIHeaderSection { get; set; }

  internal StringHeap StringHeap => CLIHeaderSection.StringHeap;
  internal UserStringHeap UserStringHeap => CLIHeaderSection.UserStringHeap;
  internal BlobHeap BlobHeap => CLIHeaderSection.BlobHeap;
  internal GuidHeap GuidHeap => CLIHeaderSection.GuidHeap;
  internal TildeStream TildeStream => CLIHeaderSection.TildeStream;
}
