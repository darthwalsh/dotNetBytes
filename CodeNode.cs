using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CodeNode : IEnumerable<string>
{
    public string Name = "oops!";
    public string Description = "";
    public string Value = "";

    public int Start;
    public int End;
    public List<CodeNode> Children = new List<CodeNode>();
    public List<string> Errors = new List<string>();

    public string LinkPath;
    CodeNode link;
    string path;
    public CodeNode Link { set { link = value; } }

    Func<IHaveValueNode> delayed;
    internal Func<IHaveValueNode> DelayedValueNode { set { delayed = value; } }
    public void UseDelayedValueNode()
    {
        if (delayed != null)
        {
            IHaveValueNode d = delayed();
            Value = d.Value.GetString();
            Link = d.Node;
        }
    }

    public void AssignPath()
    {
        AssignPath(null);
    }
    void AssignPath(string parentPath)
    {
        if (path != null)
            throw new InvalidOperationException($"path was already {path}");

        if (parentPath != null)
            parentPath += "/";
        
        path = parentPath + Name;

        foreach (var c in Children)
        {
            c.AssignPath(path);
        }
    }

    public static void AssignLink(CodeNode node)
    {
        if (node.link == null)
            return;

        if (node.link.path == null)
            throw new InvalidOperationException($"null link {node.link.Name}");

        node.LinkPath = node.link.path;
    }

    public void Add(CodeNode node)
    {
        Children.Add(node);
    }

    public void Add(IEnumerable<CodeNode> node)
    {
        Children.AddRange(node);
    }

    public void CallBack(Action<CodeNode> callback)
    {
        foreach (var c in Children)
        {
            c.CallBack(callback);
        }

        callback(this);
    }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, Yield());
    }

    IEnumerable<string> Yield(int indent = 0)
    {
        yield return new string(' ', indent) + string.Join(" ", new[]
        {
            Start.ToString("X").PadRight(4), End.ToString("X").PadRight(4), Name.PadRight(24), Value.PadRight(10), Description.Substring(0, Math.Min(Description.Length, 97))
        });

        foreach (var c in Children)
        {
            foreach (var s in c.Yield(indent + 2))
            {
                yield return s;
            }
        }
    }

    public IEnumerator<string> GetEnumerator()
    {
        return Yield().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}