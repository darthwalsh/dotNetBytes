using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

[JsonConverter(typeof(CodeNodeConverter))]
public abstract class CodeNode
{
  internal const string OOPS_NAME = "oops!";
  public virtual string NodeName { get; set; } = OOPS_NAME; // Unique name for addressing from parent
  public virtual string Description { get; set; } = ""; // Notes about this node based on the language spec
  public virtual string NodeValue { get; set; } = ""; // A ToString() view of the node.

  const int START_END_NOT_SET = -1;
  public int Start = START_END_NOT_SET;
  public int End = START_END_NOT_SET; // exclusive range

  internal AssemblyBytes Bytes { get; set; }

  public List<CodeNode> Children = new List<CodeNode>();
  public List<string> Errors = new List<string>();

  public virtual CodeNode Link { get; set; } // MAYBE this should probably be protected, but need to figure out RVA
  public string SelfPath { get; private set; }

  public void CallBack(Action<CodeNode> action) {
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

  public void Read() {
    var reposition = BeforeReposition;
    if (reposition != START_END_NOT_SET) {
      Bytes.CLIHeaderSection.Reposition(reposition);
    }

    Start = (int)Bytes.Stream.Position;
    InnerRead();
    if (End == START_END_NOT_SET) {
      End = (int)Bytes.Stream.Position;
    }
  }

  protected virtual long BeforeReposition => START_END_NOT_SET; 

  protected virtual void InnerRead() {
    var orderedFields = this.GetType().GetFields()
        .Where(field => field.DeclaringType != typeof(CodeNode))
        .ToList();

    if (orderedFields.Count > 1) {
      orderedFields = orderedFields.OrderBy(field => {
        if (field.TryGetAttribute(out OrderedFieldAttribute o)) return o.Order;
        if (field.TryGetAttribute(out ExpectedAttribute e)) return e.Line;
        if (field.TryGetAttribute(out DescriptionAttribute d)) return d.Line;
        throw new InvalidOperationException($"{this.GetType().FullName}.{field.Name} is missing [OrderedField]");
      }).ToList();
    }

    foreach (var field in orderedFields) {
      AddChild(field.Name);
    }
  }

  protected void AddChild(string fieldName) {
    var field = GetType().GetField(fieldName);
    var type = field.FieldType;

    if (type.IsArray && type.GetElementType().IsSubclassOf(typeof(CodeNode))) {
      var len = ((Array)field.GetValue(this))?.Length ?? GetCount(fieldName);
      AddChildren(fieldName, len);
      return;
    }

    var child = ReadField(fieldName);
    Children.Add(child);
    child.NodeName = fieldName;
    if (field.TryGetAttribute(out DescriptionAttribute desc)) {
      child.Description = (desc.Description + "\n" + child.Description).Trim();
    }
    CheckExpected(field);
  }

  protected void ResizeLastChild() {
    var child = Children.Last();
    if (child.End == child.Start) {
      Children.Remove(child);
    }
    if (child.Children.Count == 1) {
      Children.Remove(child);
      Children.Add(child.Children.Single());
    }
  }

  protected void AddChildren(string fieldName, int length) {
    var field = GetType().GetField(fieldName);
    var arr = (CodeNode[])(field.GetValue(this) ?? Activator.CreateInstance(field.FieldType, length));
    field.SetValue(this, arr);
    var elType = field.FieldType.GetElementType();
    var ctorIndexed = elType.GetConstructor(new[] { typeof(int) }) != null;
    for (var i = 0; i < arr.Length; i++) {
      var param = ctorIndexed ? new object[] { i } : new object[] { };
      var o = arr[i] ?? (CodeNode)Activator.CreateInstance(elType, param);
      o.Bytes = Bytes;
      o.Read();
      arr[i] = o;

      Children.Add(o);
      o.NodeName = $"{fieldName}[{i}]";
    }
    CheckExpected(field);
  }

  protected virtual CodeNode ReadField(string fieldName) {
    var field = GetType().GetField(fieldName);
    var fieldType = field.FieldType;

    if (fieldType.IsArray) {
      var elementType = fieldType.GetElementType();
      if (elementType.IsValueType) {
        int len;
        if (field.TryGetAttribute(out ExpectedAttribute e)) {
          if (e.Value is string s) {
            len = s.Length;
          } else {
            len = ((Array)e.Value).Length;
          }
        } else {
          len = GetCount(fieldName);
        }

        var san = typeof(StructArrayNode<>).MakeGenericType(elementType);
        var o = (CodeNode)Activator.CreateInstance(san, len);
        o.Bytes = Bytes;
        o.Read();

        var value = san.GetField("arr").GetValue(o);
        field.SetValue(this, value);
        o.NodeValue = value.GetString();
        return o;
      }

      throw new InvalidOperationException($"{GetType().FullName}. {field.Name} is an array {elementType}[]");
    }
    if (fieldType.IsClass) {
      var o = (CodeNode)(field.GetValue(this) ?? (CodeNode)Activator.CreateInstance(fieldType));
      o.Bytes = Bytes;
      o.Read();
      field.SetValue(this, o);
      return o;
    }
    Type sn;
    if (fieldType.IsEnum) {
      sn = typeof(EnumNode<>).MakeGenericType(fieldType); ;
    } else if (fieldType.IsValueType) {
      sn = typeof(StructNode<>).MakeGenericType(fieldType);
    } else {
      throw new InvalidOperationException(fieldType.Name);
    }
    {
      CodeNode o = (CodeNode)Activator.CreateInstance(sn);
      o.Bytes = Bytes;
      o.Read();
      var value = sn.GetField("t").GetValue(o);
      field.SetValue(this, value);
      o.NodeValue = value.GetString();
      return o;
    }
  }

  protected virtual int GetCount(string field) =>
    throw new InvalidOperationException($"{GetType().Name} .{field}");

  void CheckExpected(FieldInfo field) {
    if (field.TryGetAttribute(out ExpectedAttribute expected)) {
      var actual = field.GetValue(this);
      if (!TypeExtensions.SmartEquals(expected.Value, actual)) {
        Errors.Add($"Expected {field.Name} to be {expected.Value.GetString()} but instead found {actual.GetString()} at address 0x{Start:X}");
      }
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

  sealed class CodeNodeConverter : JsonConverter<CodeNode>
  {
    public override CodeNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

    public override void Write(Utf8JsonWriter writer, CodeNode node, JsonSerializerOptions options) {
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
}

public sealed class StructArrayNode<T> : CodeNode where T : struct
{
  public T[] arr;

  public int Length { get; }

  public StructArrayNode(int length) {
    Length = length;
  }

  protected override void InnerRead() {
    arr = Enumerable.Range(0, Length).Select(_ => {
      var node = new StructNode<T> { Bytes = Bytes };
      node.Read();
      return node.t;
    }).ToArray();
  }
}

public sealed class StructNode<T> : CodeNode where T : struct
{
  public T t;

  protected override void InnerRead() {
    t = Bytes.Read<T>();
    NodeValue = t.GetString();
  }
}

public sealed class EnumNode<T> : CodeNode where T : struct, Enum
{
  public T t;

  protected override void InnerRead() {
    t = Bytes.Read<T>();
    NodeValue = t.GetString();

    Description = string.Join('\n', t.Describe());
  }
  // TODO(fixme) think about this pattern: DON'T override Description because CodeNode expects to use the setter if the enum fieldinfo also has a description
}
