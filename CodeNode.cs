using System;
using System.Collections.Generic;

class CodeNode
{
    public string Name;
    public string Description;
    public string Value;

    public int Start;
    public int End;
    public List<CodeNode> Children = new List<CodeNode>();
    public List<string> Errors = new List<string>();

    public IEnumerable<string> Yield(int indent = 0)
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
}