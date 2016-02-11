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

        node.CallBack(n =>
        {
            if (n.Children.Any())
            {
                n.Start = Math.Min(n.Start, n.Children.Min(c => c.Start));
                n.End = Math.Max(n.End, n.Children.Max(c => c.End));
            }
        });

        node.CallBack(n =>
        {
            n.Children = n.Children.OrderBy(c => c.Start).ToList();
        });

        FindOverLength(s, node);

        node.CallBack(n => n.UseDelayedValueNode());

        node.AssignPath();
        node.CallBack(CodeNode.AssignLink);

        System.Console.Error.WriteLine(node.ToString()); // TODO move to Program, based on command line?
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
                    n.Errors.Add($"End was set beyond byte end to {n.End}");
                    n.End = (int)length;
                }
            });
        }
    }

    public CodeNode Node => node;
}
