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

        System.Console.Error.WriteLine(node.ToString());
    }

    public CodeNode Node => node;
}
