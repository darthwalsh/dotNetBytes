using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

public class AssemblyBytes
{
  public Stream Stream { get; }

  internal FileFormat fileFormat;

  public AssemblyBytes(Stream s) {
    this.Stream = s;

    fileFormat = new FileFormat { Bytes = this };
    fileFormat.Read();
    fileFormat.NodeName = "FileFormat";
    MyCodeNode node = fileFormat;

    // Widen any nodes to the width of their children
    node.CallBack(n => {
      if (n.Children.Any()) {
        n.Start = Math.Min(n.Start, n.Children.Min(c => c.Start));
        n.End = Math.Max(n.End, n.Children.Max(c => c.End));
      }
    });

    // Order child nodes by index, expected for Heaps and sections
    node.CallBack(n => {
      n.Children = n.Children.OrderBy(c => c.Start).ToList();
    });

    FindOverLength(s, node);

    node.AssignPath();
  }

  static void FindOverLength(Stream s, MyCodeNode node) {
    node.CallBack(n => {
      if (n.End > s.Length) {
        throw new InvalidOperationException($"End was set beyond byte end to {n.End}");
      }
    });
  }

  public MyCodeNode Node => fileFormat;

  internal Section CLIHeaderSection { get; set; }

  internal StringHeap StringHeap => CLIHeaderSection.StringHeap;
  internal UserStringHeap UserStringHeap => CLIHeaderSection.UserStringHeap;
  internal BlobHeap BlobHeap => CLIHeaderSection.BlobHeap;
  internal GuidHeap GuidHeap => CLIHeaderSection.GuidHeap;
  internal TildeStream TildeStream => CLIHeaderSection.TildeStream;
}

[JsonConverter(typeof(MyCodeNodeConverter))]
public abstract class MyCodeNode
{
  // TODO(solonode) probably best to rename this field to NodeName or something nonconflicting with subclass
  public virtual string NodeName { get; set; } = "oops!"; // Unique name for addressing from parent
  public virtual string Description { get; set; } = ""; // Notes about this node based on the language spec
  public virtual string NodeValue { get; set; } = ""; // A ToString() view of the node.

  // Will be widened later
  public int Start = int.MaxValue;
  public int End = int.MinValue;

  internal AssemblyBytes Bytes { get; set; }

  public List<MyCodeNode> Children = new List<MyCodeNode>();
  public List<string> Errors = new List<string>();

  public virtual MyCodeNode Link { get; set; } // TODO (solonode) this should probably be protected but need to figure out RVA
  public string SelfPath { get; private set; }
  public string LinkPath => Link?.SelfPath;

  public void CallBack(Action<MyCodeNode> action) {
    action(this);
    foreach (var child in Children) {
      child.CallBack(action);
    }
  }

  public void AssignPath() => AssignPath(null);
  void AssignPath(string parentPath) {
    if (SelfPath != null)
      throw new InvalidOperationException($"path was already {SelfPath}");

    if (parentPath != null)
      parentPath += "/";

    SelfPath = parentPath + NodeName;

    foreach (var c in Children) {
      c.AssignPath(SelfPath);
    }
  }

  public string ToJson(JsonSerializerOptions options = null) {
    if (options is null) {
      options = new JsonSerializerOptions();
    } else {
      options = new JsonSerializerOptions(options);
    }
    options.MaxDepth = 256;

    return JsonSerializer.Serialize(this, options);
  }

  sealed class MyCodeNodeConverter : JsonConverter<MyCodeNode>
  {
    public override MyCodeNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

    public override void Write(Utf8JsonWriter writer, MyCodeNode node, JsonSerializerOptions options) {
      writer.WriteStartObject();
      writer.WriteString("Name", node.NodeName);
      writer.WriteString(nameof(node.Description), node.Description);
      writer.WriteString("Value", node.NodeValue);
      writer.WriteNumber(nameof(node.Start), node.Start);
      writer.WriteNumber(nameof(node.End), node.End);
      writer.WriteString("LinkPath", node.Link?.SelfPath);

      writer.WritePropertyName(nameof(node.Errors));
      JsonSerializer.Serialize(writer, node.Errors);

      writer.WritePropertyName(nameof(node.Children));
      JsonSerializer.Serialize(writer, node.Children);

      writer.WriteEndObject();
    }
  }

  public virtual void Read() {
    MarkStarting();

    var orderedFields = this.GetType().GetFields()
        .Where(field => field.DeclaringType != typeof(MyCodeNode))
        .ToList();

    if (orderedFields.Count > 1) {
      orderedFields = orderedFields.OrderBy(field => {
        if (TryGetAttribute(field, out OrderedFieldAttribute o)) return o.Order;
        if (TryGetAttribute(field, out ExpectedAttribute e)) return e.Line;
        if (TryGetAttribute(field, out DescriptionAttribute d)) return d.Line;
        throw new InvalidOperationException($"{this.GetType().FullName}.{field.Name} is missing [OrderedField]");
      }).ToList();
    }

    foreach (var field in orderedFields) {
      AddChild(field.Name);
    }

    MarkEnding();
  }

  public void MarkStarting() => Start = (int)Bytes.Stream.Position;
  public void MarkEnding() => End = (int)Bytes.Stream.Position;

  protected void AddChild(string fieldName) {
    var field = GetType().GetField(fieldName);
    var type = field.FieldType;

    if (type.IsArray && type.GetElementType().IsSubclassOf(typeof(MyCodeNode))) {
      var len = ((Array)field.GetValue(this))?.Length ?? GetCount(fieldName);
      AddChildren(fieldName, len);
      return;
    }

    var child = ReadField(fieldName);
    Children.Add(child);
    child.NodeName = fieldName;
    if (TryGetAttribute(field, out DescriptionAttribute desc)) {
      switch (GetType().Name) { // TODO(solonode) hack for simpler diff
        case "StreamHeader":
        case "MetadataRoot":
          break;
        default:
          child.Description = desc.Description;
          break;
      }
    }
    CheckExpected(field);
  }

  protected void AddChildren(string fieldName, int length) {
    var field = GetType().GetField(fieldName);
    var arr = (MyCodeNode[])(field.GetValue(this) ?? Activator.CreateInstance(field.FieldType, length));
    field.SetValue(this, arr);
    var elType = field.FieldType.GetElementType();
    var ctorIndexed = elType.GetConstructor(new[] { typeof(int) }) != null;
    for (var i = 0; i < arr.Length; i++) {
      var param = ctorIndexed ? new object[] { i } : new object[] { };
      var o = arr[i] ?? (MyCodeNode)Activator.CreateInstance(elType, param);
      o.Bytes = Bytes;
      o.Read();
      arr[i] = o;

      Children.Add(o);
      o.NodeName = $"{fieldName}[{i}]";
    }
    CheckExpected(field);
  }

  protected virtual MyCodeNode ReadField(string fieldName) {
    var field = GetType().GetField(fieldName);
    var fieldType = field.FieldType;

    if (fieldType.IsArray) {
      var elementType = fieldType.GetElementType();
      if (elementType.IsValueType) {
        int len;
        if (TryGetAttribute<ExpectedAttribute>(field, out var e)) {
          if (e.Value is string s) {
            len = s.Length;
          } else {
            len = ((Array)e.Value).Length;
          }
        } else {
          len = GetCount(fieldName);
        }

        var sn = typeof(MyStructArrayNode<>).MakeGenericType(elementType);
        var o = (MyCodeNode)Activator.CreateInstance(sn, len);
        o.Bytes = Bytes;
        o.Read();

        var value = sn.GetField("arr").GetValue(o);
        field.SetValue(this, value);
        o.NodeValue = value.GetString();
        return o;
      }

      throw new InvalidOperationException($"{GetType().FullName}. {field.Name} is an array {elementType}[]");
    }
    if (fieldType.IsClass) {
      // var o = ((MyCodeNode)(field.GetValue(this) ?? (MyCodeNode)Activator.CreateInstance(fieldType)); TODO(solonode) hack for better exception messages
      MyCodeNode o;
      try {
        o = (MyCodeNode)(field.GetValue(this) ?? (MyCodeNode)Activator.CreateInstance(fieldType));
      } catch (System.Exception e) {
        throw new NotImplementedException($"{GetType().FullName}. {field.Name}", e); // TODO(solonode) remove!
      }
      o.Bytes = Bytes;
      o.Read();
      field.SetValue(this, o);
      return o;
    }
    if (fieldType.IsValueType) {
      var sn = typeof(MyStructNode<>).MakeGenericType(fieldType);
      var o = (MyCodeNode)Activator.CreateInstance(sn);
      o.Bytes = Bytes;
      o.Read();
      var value = sn.GetField("t").GetValue(o);
      field.SetValue(this, value);
      o.NodeValue = value.GetString();
      return o;
    }
    throw new InvalidOperationException(fieldType.Name);
  }

  protected virtual int GetCount(string field) =>
    throw new InvalidOperationException($"{GetType().Name} .{field}");

  void CheckExpected(FieldInfo field) {
    if (TryGetAttribute(field, out ExpectedAttribute expected)) {
      var actual = field.GetValue(this);
      if (!TypeExtensions.SmartEquals(expected.Value, actual)) {
        Errors.Add($"Expected {field.Name} to be {expected.Value.GetString()} but instead found {actual.GetString()} at address 0x{Start:X}");
      }
    }
  }

  static bool TryGetAttribute<T>(MemberInfo member, out T attr) where T : Attribute {
    var attrs = member.GetCustomAttributes(typeof(T), false);
    if (!attrs.Any()) {
      attr = null;
      return false;
    }
    attr = (T)attrs.Single();
    return true;
  }
}

public sealed class MyStructArrayNode<T> : MyCodeNode where T : struct
{
  public T[] arr;

  public int Length { get; }

  public MyStructArrayNode(int length) {
    Length = length;
  }

  public override void Read() {
    MarkStarting();

    arr = Enumerable.Range(0, Length).Select(_ => {
      var node = new MyStructNode<T> { Bytes = Bytes };
      node.Read();
      return node.t;
    }).ToArray();

    MarkEnding();
  }
}

public sealed class MyStructNode<T> : MyCodeNode where T : struct
{
  public T t;

  public override void Read() {
    MarkStarting();

    t = Bytes.Stream.ReadStruct<T>();
    NodeValue = t.GetString();

    MarkEnding();
  }
}
