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

  MyCodeNode node;

  public AssemblyBytes(Stream s) {
    this.Stream = s;

    FileFormat fileFormat = new FileFormat();
    fileFormat.Read(this);;
    node = fileFormat;

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

  public MyCodeNode Node => node;
}

[JsonConverter(typeof(MyCodeNodeConverter))]
public abstract class MyCodeNode
{
  public string Name = "oops!"; // Unique name for addressing from parent
  public string Description = ""; // Notes about this node based on the language spec
  public string Value = ""; // A ToString() view of the node.

  // Will be widened later
  public int Start = int.MaxValue;
  public int End = int.MinValue;

  public List<MyCodeNode> Children = new List<MyCodeNode>(); // TODO reflect these probably?
  public List<string> Errors = new List<string>();
  public virtual MyCodeNode Link { get; } // TODO not sure
  public string LinkPath;


  public static event Action<string> OnError = e => { }; // MAYBE foreach children

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
      var fieldType = field.FieldType;

      if (fieldType.IsArray) {
        if (fieldType.GetElementType() == typeof(byte) && TryGetAttribute(field, out ExpectedAttribute e)) {
          var value = (byte[])e.Value;
          var array = new byte[value.Length];

          bytes.Stream.ReadWholeArray(array);
          field.SetValue(this, array); // TODO make CodeNode???
        } else if (fieldType.GetElementType() == typeof(char) && TryGetAttribute(field, out ExpectedAttribute e2)) {
          var value = (string)e2.Value;
          var array = new byte[value.Length];

          bytes.Stream.ReadWholeArray(array);
          field.SetValue(this, array.Select(b=>(char)b).ToArray()); // TODO make CodeNode???
        } else {
          throw new InvalidOperationException($"{this.GetType().FullName}.{field.Name} is an array {fieldType.GetElementType()}[]");
        }
      } else if (fieldType.IsClass) {
        var o = (MyCodeNode)Activator.CreateInstance(fieldType);
        o.Read(bytes);
        field.SetValue(this, o);
        Children.Add(o);
      }
      else if (fieldType.IsValueType) {
        // TODO capture Node metadata somewhere, like in a dict, or a 
        var sn = typeof(MyStructNode<>).MakeGenericType(fieldType);
        var o = (MyCodeNode)Activator.CreateInstance(sn);
        o.Read(bytes);
        field.SetValue(this, sn.GetField("t").GetValue(o));
      }
      else
        throw new InvalidOperationException(fieldType.Name);

    }

    this.End = (int)bytes.Stream.Position;
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




sealed class ArrLengthAttribute : Attribute
{
  public int Len { get; }
  public ArrLengthAttribute(int len) {
    Len = len;
  }
}
