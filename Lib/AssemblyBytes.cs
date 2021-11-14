using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

public class AssemblyBytes
{
  public Stream Stream { get; }

  internal FileFormat fileFormat;

  public AssemblyBytes(Stream s) {
    this.Stream = s;

    fileFormat = new FileFormat();
    fileFormat.Read(this);
    fileFormat.Name = "FileFormat";
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

    // node.CallBack(n => n.UseDelayedValueNode());

    // LinkMethodDefRVA();

    // node.AssignPath();
    // node.CallBack(CodeNode.AssignLink);
  }

  static void LinkMethodDefRVA() {
    var tildeStream = Singletons.Instance.TildeStream;
    if (tildeStream.MethodDefs == null) return;

    foreach (var def in tildeStream.MethodDefs.Where(def => def.RVA != 0)) {
      def.RVANode.Link = tildeStream.Section.MethodsByRVA[def.RVA].Node;
    }
  }

  static void FindOverLength(Stream s, MyCodeNode node) {
    node.CallBack(n => {
      if (n.End > s.Length) {
        throw new InvalidOperationException($"End was set beyond byte end to {n.End}");
      }
    });
  }

  public MyCodeNode Node => fileFormat;

  // SetOnce<StringHeap> stringHeap = new SetOnce<StringHeap>();
  // internal StringHeap StringHeap { get { return stringHeap.Value; } set { stringHeap.Value = value; } }

  // SetOnce<UserStringHeap> userStringHeap = new SetOnce<UserStringHeap>();
  // internal UserStringHeap UserStringHeap { get { return userStringHeap.Value; } set { userStringHeap.Value = value; } }

  // SetOnce<BlobHeap> blobHeap = new SetOnce<BlobHeap>();
  // internal BlobHeap BlobHeap { get { return blobHeap.Value; } set { blobHeap.Value = value; } }

  // SetOnce<GuidHeap> guidHeap = new SetOnce<GuidHeap>();
  // internal GuidHeap GuidHeap { get { return guidHeap.Value; } set { guidHeap.Value = value; } }

  // SetOnce<TildeStream> tildeStream = new SetOnce<TildeStream>();
  // internal TildeStream TildeStream { get { return tildeStream.Value; } set { tildeStream.Value = value; } }

  // class SetOnce<T> where T : class
  // {
  //   T t;
  //   public T Value {
  //     get {
  //       return t;
  //     }
  //     set {
  //       if (t != null) throw new InvalidOperationException();
  //       t = value;
  //     }
  //   }
  // }
}

[JsonConverter(typeof(MyCodeNodeConverter))]
[StructLayout(LayoutKind.Sequential)]
public abstract class MyCodeNode
{
  public string Name = "oops!"; // Unique name for addressing from parent
  public string Description = ""; // Notes about this node based on the language spec
  public string Value = ""; // A ToString() view of the node.

  // Will be widened later
  public int Start = int.MaxValue;
  public int End = int.MinValue;

  public List<MyCodeNode> Children = new List<MyCodeNode>();
  public List<string> Errors = new List<string>();
  public virtual MyCodeNode Link { get; } // TODO need to figure this out.
  public string LinkPath;

  public void CallBack(Action<MyCodeNode> action) {
    action(this);
    foreach (var child in Children) {
      child.CallBack(action);
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

  public virtual void Read(AssemblyBytes bytes) {
    this.Start = (int)bytes.Stream.Position;

    var ordedFields = this.GetType().GetFields()
        .Where(field => field.DeclaringType != typeof(MyCodeNode))
        .OrderBy(field => {
          if (TryGetAttribute(field, out OrderedFieldAttribute o)) return o.Order;
          if (TryGetAttribute(field, out ExpectedAttribute e)) return e.Line;
          if (TryGetAttribute(field, out DescriptionAttribute d)) return d.Line;
          throw new InvalidOperationException($"{this.GetType().FullName}.{field.Name} is missing [OrderedField]");
        }).ToList();

    foreach (var field in ordedFields) {
      AddChild(bytes, field.Name);
    }

    this.End = (int)bytes.Stream.Position;
  }

  protected void AddChild(AssemblyBytes bytes, string fieldName) {
    var field = GetType().GetField(fieldName);
    var type = field.FieldType;

    if (type.IsArray && type.GetElementType().IsSubclassOf(typeof(MyCodeNode))) {
      var arr = (MyCodeNode[])Activator.CreateInstance(type, GetCount(fieldName));
      field.SetValue(this, arr);
      for (var i = 0; i < arr.Length; i++) {
        var o = (MyCodeNode)Activator.CreateInstance(type.GetElementType(), i);
        o.Read(bytes);
        arr[i] = o;

        Children.Add(o);
        o.Name = $"{fieldName}[{i}]";
      }
      return;
    }

    var child = ReadField(bytes, fieldName);
    Children.Add(child);
    child.Name = fieldName;
    if (TryGetAttribute(field, out DescriptionAttribute desc)) {
      child.Description = desc.Description;
    }
  }

  protected virtual MyCodeNode ReadField(AssemblyBytes bytes, string fieldName) {
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
        o.Read(bytes);

        var value = sn.GetField("arr").GetValue(o);
        field.SetValue(this, value);
        o.Value = value.GetString();
        return o;
      }

      throw new InvalidOperationException($"{GetType().FullName}.{field.Name} is an array {elementType}[]");
    }
    if (fieldType.IsClass) {
      var o = (MyCodeNode)Activator.CreateInstance(fieldType);
      o.Read(bytes);
      field.SetValue(this, o);
      return o;
    }
    if (fieldType.IsValueType) {
      var sn = typeof(MyStructNode<>).MakeGenericType(fieldType);
      var o = (MyCodeNode)Activator.CreateInstance(sn);
      o.Read(bytes);
      var value = sn.GetField("t").GetValue(o);
      field.SetValue(this, value);
      o.Value = value.GetString();
      return o;
    }
    throw new InvalidOperationException(fieldType.Name);
  }

  protected virtual int GetCount(string field) => throw new InvalidOperationException(GetType().Name);

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

  public override void Read(AssemblyBytes bytes) {
    this.Start = (int)bytes.Stream.Position;

    arr = Enumerable.Range(0, Length).Select(_ => {
      var node = new MyStructNode<T>();
      node.Read(bytes);
      return node.t;
    }).ToArray();

    this.End = (int)bytes.Stream.Position;
  }
}


public sealed class MyStructNode<T> : MyCodeNode where T : struct
{
  public T t;

  public override void Read(AssemblyBytes bytes) {
    this.Start = (int)bytes.Stream.Position;

    // http://stackoverflow.com/a/4159279/771768
    var sz = typeof(T).GetSize();
    var buffer = new byte[sz];
    bytes.Stream.ReadWholeArray(buffer);
    var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
    var ptype = typeof(T);
    if (ptype.IsEnum)
      ptype = ptype.GetEnumUnderlyingType();
    t = (T)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), ptype);
    pinnedBuffer.Free();

    this.End = (int)bytes.Stream.Position;
  }
}
