using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web.Script.Serialization;

public class AssemblyBytes
{
    FileFormat FileFormat;

    CodeNode node;

    public AssemblyBytes(Stream s)
    {
        node = s.ReadClass(ref FileFormat);

        // Widen any nodes to the width of their children
        node.CallBack(n =>
        {
            if (n.Children.Any())
            {
                n.Start = Math.Min(n.Start, n.Children.Min(c => c.Start));
                n.End = Math.Max(n.End, n.Children.Max(c => c.End));
            }
        });

        // Order child nodes by index, expected for Heaps and sections
        node.CallBack(n =>
        {
            n.Children = n.Children.OrderBy(c => c.Start).ToList();
        });

        FindOverLength(s, node);

        node.CallBack(n => n.UseDelayedValueNode());

        node.AssignPath();
        node.CallBack(CodeNode.AssignLink);
    }

    static void FindOverLength(Stream s, CodeNode node)
    {
        long? length = null;
        try
        {
            length = s.Length;
        }
        catch
        { }

        if (length.HasValue)
        {
            node.CallBack(n =>
            {
                if (n.End > length)
                {
                    throw new InvalidOperationException($"End was set beyond byte end to {n.End}");
                }
            });
        }
    }

    public CodeNode Node => node;
}
