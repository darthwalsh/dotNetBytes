using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


// Used to guarantee reflecting over class fields uses line number
// https://stackoverflow.com/a/17998371/771768
sealed class OrderedFieldAttribute : Attribute
{
  public int Order;
  public OrderedFieldAttribute([CallerLineNumber] int i = -1) {
    Order = i;
  }
}

sealed class ExpectedAttribute : Attribute
{
  public object Value;
  public int Line;
  public ExpectedAttribute(object v, [CallerLineNumber] int i = -1) {
    Value = v;
    Line = i;
  }
}

sealed class DescriptionAttribute : Attribute
{
  public string Description;
  public int Line;
  public DescriptionAttribute(string d, [CallerLineNumber] int i = -1) {
    Description = d;
    Line = i;
  }
}

sealed class EcmaAttribute : Attribute
{
  public string EcmaSection;
  public EcmaAttribute(string section) {
    EcmaSection = section;
  }
}

static class StreamExtensions
{
  // https://jonskeet.uk/csharp/readbinary.html
  public static void ReadWholeArray(this Stream stream, byte[] data) {
    if (!stream.TryReadWholeArray(data, out var error))
      throw new EndOfStreamException(error);
  }

  public static bool TryReadWholeArray(this Stream stream, byte[] data, out string error) {
    var offset = 0;
    var remaining = data.Length;
    while (remaining > 0) {
      var read = stream.Read(data, offset, remaining);
      if (read <= 0) {
        error = $"End of stream reached with {remaining} bytes left to read";
        return false;
      }
      remaining -= read;
      offset += read;
    }
    error = null;
    return true;
  }

  public static T ReadStruct<T>(this Stream stream) where T : struct => (T)stream.ReadStruct(typeof(T));

  public static object ReadStruct(this Stream stream, Type t) {
    // https://stackoverflow.com/a/4159279/771768
    var sz = t.GetSize();
    var buffer = new byte[sz];
    stream.ReadWholeArray(buffer);
    var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
    var ptype = t;
    if (ptype.IsEnum)
      ptype = ptype.GetEnumUnderlyingType();
    var o = Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), ptype);
    pinnedBuffer.Free();
    return o;
  }
}

static class TypeExtensions
{
  public static int GetInt32(this object o) {
    var convert = o as IConvertible;

    if (convert != null) {
      return convert.ToInt32(CultureInfo.InvariantCulture);
    }

    return o.GetType().GetFields()
      .Single(field => field.DeclaringType != typeof(CodeNode))
      .GetValue(o).GetInt32();
  }

  public static int GetSize(this object o) {
    var os = o as IEnumerable;
    if (os != null) {
      return os.Cast<object>().Sum(GetSize);
    }

    return o.GetType().GetSize();
  }

  public static int GetSize(this Type type) {
    if (type.IsEnum)
      return Enum.GetUnderlyingType(type).GetSize();

    if (!type.IsConstructedGenericType)
      return Marshal.SizeOf(type);

    return type.GetFields(BindingFlags.Public | BindingFlags.Instance).Sum(field => {
      return field.FieldType.GetSize();
    });
  }

  static Dictionary<char, string> escapes = new Dictionary<char, string> {
    ['\0'] = @"\0",
    ['\t'] = @"\t",
    ['\r'] = @"\r",
    ['\n'] = @"\n",
    ['\v'] = @"\v",
  };

  public static string EscapeControl(this string s) {
    return string.Concat(s.Select(c => {
      if (char.IsControl(c)) {
        if (escapes.TryGetValue(c, out var ans)) {
          return ans;
        }

        return @"\" + ((int)c).ToString("X");
      }

      return "" + c;
    }));
  }

  public static string GetString(this object o) {
    if (o == null) throw new ArgumentNullException();

    if (o is string s) return '"' + s.EscapeControl() + '"';
    if (o is char[] cs) return new string(cs).GetString();
    if (o is IEnumerable os) {
      return "{" + string.Join(", ", os.Cast<object>().Select(GetString)) + "}";
    }

    if (o is Enum en) return "0x" + en.ToString("X") + " " + en.ToString();
    if (o is Guid guid) return guid.ToString();
    if (o is float f) return f.ToString();
    if (o is double d) return d.ToString();

    var method = o.GetType().GetMethod("ToString", new[] { typeof(string) });
    if (method != null) {
      return "0x" + (string)method.Invoke(o, new object[] { "X" });
    }

    return o.ToString();
  }

  public static IEnumerable<string> Describe<T>(this T value) where T : Enum {
    var flags = typeof(T).IsDefined(typeof(FlagsAttribute));
    var values = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
      .Select(fi => new { fi, t = (T)fi.GetRawConstantValue() });
    if (flags) {
      foreach (var o in values) {
        if (value.HasFlag(o.t)) {
          if (o.fi.TryGetAttribute(out DescriptionAttribute desc)) {
            yield return $"{o.t.GetString()} {desc.Description}";
          } else {
            if (typeof(T) != typeof(MetadataTableFlags)) {
              throw new NotImplementedException($"OOPS {typeof(T)} {o.t.GetString()} Missing [Description] text!");
            }
          }
        }
      }
      yield break;
    }

    var fi = values.SingleOrDefault(o => value.Equals(o.t));
    if (fi == default) {
      //TODO(pedant) should there be an Error when enum value doesn't exist?
    } else if (fi.fi.TryGetAttribute(out DescriptionAttribute desc)) {
      yield return desc.Description;
    }
    // No problem if the enum value doesn't have a description
  }

  public static bool TryGetAttribute<T>(this MemberInfo member, out T attr) where T : Attribute {
    attr = (T)member.GetCustomAttributes(typeof(T), false).SingleOrDefault();
    return attr != default;
  }

  public static bool SmartEquals(object expected, object actual) {
    if (object.Equals(expected, actual)) {
      return true;
    }

    if (expected is int && !(actual is int)) {
      if (actual is ulong) {
        return ((ulong)(int)expected) == ((ulong)actual);
      }

      return ((int)expected).Equals(actual.GetInt32());
    }

    var expecteds = expected as IEnumerable;
    var actuals = actual as IEnumerable;

    if (expecteds != null && actuals != null) {
      return expecteds.Cast<object>().SequenceEqual(actuals.Cast<object>());
    }

    return false;
  }
}
