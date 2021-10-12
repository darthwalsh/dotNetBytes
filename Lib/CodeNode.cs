using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonConverter(typeof(CodeNodeConverter))]
public class CodeNode : IEnumerable<string>
{
  public CodeNode() { }
  public CodeNode(string name) { Name = name; }

  public string Name = "oops!"; // Unique name for addressing from parent
  public string Description = ""; // Notes about this node based on the language spec
  public string Value = ""; // A ToString() view of the node. Can be multiple lines

  // will be widened later
  public int Start = int.MaxValue;
  public int End = int.MinValue;

  public List<CodeNode> Children = new List<CodeNode>();

  List<string> errors = new List<string>();
  public void AddError(string error) {
    OnError(error);
    errors.Add(error);
  }
  public IEnumerable<string> Errors => errors.AsReadOnly();

  public string LinkPath;
  CodeNode link;
  string path;
  public CodeNode Link { set { link = value; } }

  //TODO(cleanup) have multiple stages of reading, where StringHeaps are parsed first, then metadata, then methods.
  Func<IHaveLiteralValueNode> delayed;
  internal Func<IHaveLiteralValueNode> DelayedValueNode { set { delayed = value; } } //TODO(cleanup) remove this hack
  public void UseDelayedValueNode() {
    if (delayed != null) {
      try {
        var d = delayed();
        Value = (string)d.Value;
        Link = d.Node;
      } catch (Exception e) {
        AddError("Using DelayedValueNode blew up with " + e.ToString());
      }
    }
  }

  public void AssignPath() => AssignPath(null);
  void AssignPath(string parentPath) {
    if (path != null)
      throw new InvalidOperationException($"path was already {path}");

    if (parentPath != null)
      parentPath += "/";

    path = parentPath + Name;

    foreach (var c in Children) {
      c.AssignPath(path);
    }
  }

  public static void AssignLink(CodeNode node) {
    if (node.link == null)
      return;

    if (node.link.path == null)
      throw new InvalidOperationException($"null link {node.link.Name}");

    node.LinkPath = node.link.path;
  }

  public void Add(CodeNode node) => Children.Add(node);

  public void Add(IEnumerable<CodeNode> node) => Children.AddRange(node);

  public void CallBack(Action<CodeNode> callback) {
    foreach (var c in Children) {
      c.CallBack(callback);
    }

    callback(this);
  }

  public override string ToString() => string.Join(Environment.NewLine, Yield());

  IEnumerable<string> Yield(int indent = 0) {
    yield return new string(' ', indent) + string.Join(" ", new[]
    {
            Start.ToString("X").PadRight(4), End.ToString("X").PadRight(4), Name.PadRight(32),
            (link != null ? "-> " + link.Start.ToString("X").PadRight(4) : new string(' ', 7)),
            Value.PadRight(10), Description.Substring(0, Math.Min(Description.Length, 89))
        });

    foreach (var c in Children) {
      foreach (var s in c.Yield(indent + 2)) {
        yield return s;
      }
    }
  }

  public IEnumerator<string> GetEnumerator() => Yield().GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public string ToJson(JsonSerializerOptions options = null) {
    if (options is null) {
      options = new JsonSerializerOptions();
    } else {
      options = new JsonSerializerOptions(options);
    }
    options.MaxDepth = 256;

    return JsonSerializer.Serialize(this, options);
  }

  sealed class CodeNodeConverter : JsonConverter<CodeNode>
  {
    public override CodeNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

    public override void Write(Utf8JsonWriter writer, CodeNode node, JsonSerializerOptions options) {
      writer.WriteStartObject();
      writer.WriteString(nameof(node.Name), node.Name);
      writer.WriteString(nameof(node.Name), node.Name);
      writer.WriteString(nameof(node.Description), node.Description);
      writer.WriteString(nameof(node.Value), node.Value);
      writer.WriteNumber(nameof(node.Start), node.Start);
      writer.WriteNumber(nameof(node.End), node.End);
      writer.WriteString(nameof(node.LinkPath), node.LinkPath);

      writer.WritePropertyName(nameof(node.Errors));
      JsonSerializer.Serialize(writer, node.Errors);

      writer.WritePropertyName(nameof(node.Children));
      JsonSerializer.Serialize(writer, node.Children);

      writer.WriteEndObject();
    }
  }

  public static event Action<string> OnError = e => { };
}
