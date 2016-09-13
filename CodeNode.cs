using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

public class CodeNode : IEnumerable<string>
{
    public CodeNode() { }
    public CodeNode(string name) { Name = name; }

    public string Name = "oops!";
    public string Description = "";
    public string Value = "";

    // will be widened later
    public int Start = int.MaxValue;
    public int End = int.MinValue;

    public List<CodeNode> Children = new List<CodeNode>();

    List<string> errors = new List<string>();
    public string SingleError { set { AddError(value); } }
    public void AddError(string error)
    {
        OnError(error);
        errors.Add(error);
    }
    public IEnumerable<string> Errors => errors.AsReadOnly();

    public string LinkPath;
    CodeNode link;
    string path;
    public CodeNode Link { set { link = value; } }

    // TODO refactor these to be extra Action in some outer event loop?
    Func<IHaveValueNode> delayed;
    internal Func<IHaveValueNode> DelayedValueNode { set { delayed = value; } }
    public void UseDelayedValueNode()
    {
        if (delayed != null)
        {
            try
            {
                IHaveValueNode d = delayed();
                Value = d.Value.GetString();
                Link = d.Node;
            }
            catch (Exception e)
            {
                AddError("Using DelayedValueNode blew up with " + e.ToString());
            }
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
            Start.ToString("X").PadRight(4), End.ToString("X").PadRight(4), Name.PadRight(32),
            (link != null ? "-> " + link.Start.ToString("X").PadRight(4) : new string(' ', 7)),
            Value.PadRight(10), Description.Substring(0, Math.Min(Description.Length, 89))
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
    
    public string ToJson()
    {
        return CodeNodeConverter.ToJson(this);
    }

    class CodeNodeConverter : JavaScriptConverter
    {
        public static string ToJson(CodeNode node)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = 0x08000000;
            serializer.RegisterConverters(new[] { new CodeNodeConverter() });
            return serializer.Serialize(node);
        }

        CodeNodeConverter() { }

        public override IEnumerable<Type> SupportedTypes => new[] { typeof(CodeNode) };

        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            throw new InvalidOperationException();
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            var node = (CodeNode)obj;

            return new Dictionary<string, object>
            {
                { nameof(node.Name), node.Name },
                { nameof(node.Description), node.Description },
                { nameof(node.Value), node.Value },
                { nameof(node.Start), node.Start },
                { nameof(node.End), node.End },
                { nameof(node.LinkPath), node.LinkPath },
                { nameof(node.Errors), node.Errors },
                { nameof(node.Children), node.Children },
            };
        }
    }

    public static event Action<string> OnError = e => { };
}