using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

// II.23 Metadata logical format: other structures 

// CodeNode is written though reflection
#pragma warning disable 0649 // CS0649: Field '...' is never assigned to

// II.23.1.1
enum AssemblyHashAlgorithm : uint
{
  None = 0x0000,
  Reserved_MD5 = 0x8003,
  SHA1 = 0x8004,
}

// II.23.1.2 
[Flags]
enum AssemblyFlags : uint
{
  [Description("The assembly reference holds the full (unhashed) public key.")]
  PublicKey = 0x0001,
  [Description("The implementation of this assembly used at runtime is not expected to match the version seen at compile time. (See the text following this table.)")]
  Retargetable = 0x0100,
  [Description("Reserved (a conforming implementation of the CLI can ignore this setting on read; some implementations might use this bit to indicate that a CIL-to-native-code compiler should not generate optimized code)")]
  DisableJITcompileOptimizer = 0x4000,
  [Description("Reserved (a conforming implementation of the CLI can ignore this setting on read; some implementations might use this bit to indicate that a CIL-to-native-code compiler should generate CIL-to-native code map)")]
  EnableJITcompileTracking = 0x8000,
}

// II.23.1.4
[Flags]
enum EventAttributes : ushort
{
  [Description("Event is special")]
  SpecialName = 0x0200,
  [Description("CLI provides 'special' behavior, depending upon the name of the event")]
  RTSpecialName = 0x0400,
}

// II.23.1.5
sealed class FieldAttributes : CodeNode
{
  // MAYBE add props for all the attrs below
  public AccessAttributes Access { get; private set; }
  public AdditionalFlags Flags { get; private set; }

  protected override void InnerRead() {
    var data = Bytes.Read<ushort>();
    Access = (AccessAttributes)(data & accessMask);
    Flags = (AdditionalFlags)(data & flagsMask);
  }

  public override string NodeValue => (new Enum[] { Access, Flags }).GetString();
  public override string Description => string.Join("\n", Access.Describe().Concat(Flags.Describe()));


  const ushort accessMask = 0x0007;

  public enum AccessAttributes : ushort
  {
    [Description("Member not referenceable")]
    CompilerControlled = 0x0000,
    [Description("Accessible only by the parent type")]
    Private = 0x0001,
    [Description("Accessible by sub-types only in this Assembly")]
    FamANDAssem = 0x0002,
    [Description("Accessibly by anyone in the Assembly")]
    Assembly = 0x0003,
    [Description("Accessible only by type and sub-types")]
    Family = 0x0004,
    [Description("Accessibly by sub-types anywhere, plus anyone in assembly")]
    FamORAssem = 0x0005,
    [Description("Accessibly by anyone who has visibility to this scope field contract attributes")]
    Public = 0x0006,
  }

  const ushort flagsMask = unchecked((ushort)~accessMask);
  [Flags]
  public enum AdditionalFlags : ushort
  {
    [Description("Defined on type, else per instance")]
    Static = 0x0010,
    [Description("Field can only be initialized, not written to after init")]
    InitOnly = 0x0020,
    [Description("Value is compile time constant")]
    Literal = 0x0040,
    [Description("Reserved (to indicate this field should not be serialized when type is remoted)")]
    NotSerialized = 0x0080,
    [Description("Field is special")]
    SpecialName = 0x0200,
    [Description("Implementation is forwarded through PInvoke.")]
    PInvokeImpl = 0x2000,
    [Description("CLI provides 'special' behavior, depending upon the name of the field")]
    RTSpecialName = 0x0400,
    [Description("Field has marshalling information")]
    HasFieldMarshal = 0x1000,
    [Description("Field has default")]
    HasDefault = 0x8000,
    [Description("Field has RVA")]
    HasFieldRVA = 0x0100,
  }
}

// II.23.1.6
[Flags]
enum FileAttributes : uint
{
  [Description("This is not a resource file")]
  ContainsMetaData = 0x0000,
  [Description("This is a resource file or other non-metadata-containing file")]
  ContainsNoMetaData = 0x0001,
}

// II.23.1.7
sealed class GenericParamAttributes : CodeNode
{
  public VarianceAttributes Variance { get; private set; }
  public SpecialConstraintAttributes SpecialConstraint { get; private set; }

  protected override void InnerRead() {
    var data = Bytes.Read<ushort>();
    Variance = (VarianceAttributes)(data & varianceMask);
    SpecialConstraint = (SpecialConstraintAttributes)(data & specialConstraintMask);
  }

  public override string NodeValue => (new Enum[] { Variance, SpecialConstraint }).GetString();
  public override string Description => string.Join("\n", Variance.Describe().Concat(SpecialConstraint.Describe()));


  const ushort varianceMask = 0x0003;

  public enum VarianceAttributes : ushort
  {
    [Description("The generic parameter is non-variant and has no special constraints")]
    None = 0x0000,
    [Description("The generic parameter is covariant")]
    Covariant = 0x0001,
    [Description("The generic parameter is contravariant")]
    Contravariant = 0x0002,
  }

  const ushort specialConstraintMask = 0x0004;

  public enum SpecialConstraintAttributes : ushort
  {
    [Description("The generic parameter has the class special constraint")]
    ReferenceTypeConstraint = 0x0004,
    [Description("The generic parameter has the valuetype special constraint")]
    NotNullableValueTypeConstraint = 0x0008,
    [Description("The generic parameter has the .ctor special constraint")]
    DefaultConstructorConstraint = 0x0010,
  }
}

// II.23.1.8
sealed class PInvokeAttributes : CodeNode
{
  public CharacterSetAttributes CharacterSet { get; private set; }
  public CallingConventionAttributes CallingConvention { get; private set; }
  public AdditionalFlags Flags { get; private set; }

  protected override void InnerRead() {
    var data = Bytes.Read<ushort>();
    CharacterSet = (CharacterSetAttributes)(data & characterSetMask);
    CallingConvention = (CallingConventionAttributes)(data & callingConventionMask);
    Flags = (AdditionalFlags)(data & flagsMask);
  }

  public override string NodeValue => (new Enum[] { CharacterSet, CallingConvention, Flags }).GetString();
  public override string Description => string.Join("\n", CharacterSet.Describe()
    .Concat(CallingConvention.Describe())
    .Concat(Flags.Describe()));

  const ushort characterSetMask = 0x0007;

  public enum CharacterSetAttributes : ushort
  {
    [Description("")]
    NotSpec = 0x0000,
    [Description("")]
    Ansi = 0x0002,
    [Description("")]
    Unicode = 0x0004,
    [Description("")]
    Auto = 0x0006,
  }

  const ushort callingConventionMask = 0x0100;

  public enum CallingConventionAttributes : ushort
  {
    [Description("")]
    PlatformAPI = 0x0100,
    [Description("")]
    Cdecl = 0x0200,
    [Description("")]
    StdCall = 0x0300,
    [Description("")]
    ThisCall = 0x0400,
    [Description("")]
    FastCall = 0x0500,
  }

  const ushort flagsMask = unchecked((ushort)~characterSetMask & ~callingConventionMask);
  [Flags]
  public enum AdditionalFlags : ushort
  {
    [Description("PInvoke is to use the member name as specified")]
    NoMangle = 0x0001,
    [Description("Information about target function. Not relevant for fields")]
    SupportsLastError = 0x0040,
  }
}

// II.23.1.9
enum ManifestResourceAttributes : uint
{
  [Description("The Resource is exported from the Assembly")]
  Public = 0x0001,
  [Description("The Resource is private to the Assembly")]
  Private = 0x0002,
}

// II.23.1.10
sealed class MethodAttributes : CodeNode
{
  public MemberAccessAttributes MemberAccess { get; private set; }
  public VtableLayoutAttributes VtableLayout { get; private set; }
  public AdditionalFlags Flags { get; private set; }

  protected override void InnerRead() {
    var data = Bytes.Read<ushort>();
    MemberAccess = (MemberAccessAttributes)(data & memberAccessMask);
    VtableLayout = (VtableLayoutAttributes)(data & vtableLayoutMask);
    Flags = (AdditionalFlags)(data & flagsMask);
  }

  public override string NodeValue => (new Enum[] { MemberAccess, VtableLayout, Flags }).GetString();

  public override string Description => string.Join("\n", MemberAccess.Describe()
    .Concat(VtableLayout.Describe())
    .Concat(Flags.Describe()));

  const ushort memberAccessMask = 0x0007;

  public enum MemberAccessAttributes : ushort
  {
    [Description("Member not referenceable")]
    CompilerControlled = 0x0000,
    [Description("Accessible only by the parent type")]
    Private = 0x0001,
    [Description("Accessible by sub-types only in this Assembly")]
    FamANDAssem = 0x0002,
    [Description("Accessibly by anyone in the Assembly")]
    Assem = 0x0003,
    [Description("Accessible only by type and sub-types")]
    Family = 0x0004,
    [Description("Accessibly by sub-types anywhere, plus anyone in assembly")]
    FamORAssem = 0x0005,
    [Description("Accessibly by anyone who has visibility to this scope")]
    Public = 0x0006,
  }

  const ushort vtableLayoutMask = 0x0100;

  public enum VtableLayoutAttributes : ushort
  {
    [Description("Method reuses existing slot in vtable")]
    ReuseSlot = 0x0000,
    [Description("Method always gets a new slot in the vtable")]
    NewSlot = 0x0100,
  }

  const ushort flagsMask = unchecked((ushort)~memberAccessMask & ~vtableLayoutMask);
  [Flags]
  public enum AdditionalFlags : ushort
  {
    [Description("Defined on type, else per instance")]
    Static = 0x0010,
    [Description("Method cannot be overridden")]
    Final = 0x0020,
    [Description("Method is virtual")]
    Virtual = 0x0040,
    [Description("Method hides by name+sig, else just by name")]
    HideBySig = 0x0080,
    [Description("Method can only be overriden if also accessible")]
    Strict = 0x0200,
    [Description("Method does not provide an implementation")]
    Abstract = 0x0400,
    [Description("Method is special")]
    SpecialName = 0x0800,
    [Description("Implementation is forwarded through PInvoke")]
    PInvokeImpl = 0x2000,
    [Description("Reserved: shall be zero for conforming implementations")]
    UnmanagedExport = 0x0008,
    [Description("CLI provides 'special' behavior, depending upon the name of the method")]
    RTSpecialName = 0x1000,
    [Description("Method has security associate with it")]
    HasSecurity = 0x4000,
    [Description("Method calls another method containing security code.")]
    RequireSecObject = 0x8000,
  }
}

// II.23.1.11
sealed class MethodImplAttributes : CodeNode
{
  public CodeTypeAttributes CodeType { get; private set; }
  public ManagedAttributes Managed { get; private set; }
  public AdditionalFlags Flags { get; private set; }

  protected override void InnerRead() {
    var data = Bytes.Read<ushort>();
    CodeType = (CodeTypeAttributes)(data & codeTypeMask);
    Managed = (ManagedAttributes)(data & managedMask);
    Flags = (AdditionalFlags)(data & flagsMask);
  }

  public override string NodeValue => (new Enum[] { CodeType, Managed, Flags }).GetString();
  public override string Description => string.Join("\n", CodeType.Describe()
    .Concat(Managed.Describe())
    .Concat(Flags.Describe()));

  const ushort codeTypeMask = 0x0003;

  public enum CodeTypeAttributes : ushort
  {
    [Description("Method impl is CIL")]
    IL = 0x0000,
    [Description("Method impl is native")]
    Native = 0x0001,
    [Description("Reserved: shall be zero in conforming implementations")]
    OPTIL = 0x0002,
    [Description("Method impl is provided by the runtime")]
    Runtime = 0x0003,
  }

  const ushort managedMask = 0x0004;

  public enum ManagedAttributes : ushort
  {
    [Description("Method impl is unmanaged")]
    Unmanaged = 0x0004,
    [Description("Method impl is managed")]
    Managed = 0x0000,
  }

  const ushort flagsMask = unchecked((ushort)~codeTypeMask & ~managedMask);
  [Flags]
  public enum AdditionalFlags : ushort
  {
    [Description("Method cannot be inlined")]
    NoInlining = 0x0008,
    [Description("Indicates method is defined; used primarily in merge scenarios")]
    ForwardRef = 0x0010,
    [Description("Method is single threaded through the body")]
    Synchronized = 0x0020,
    [Description("Method will not be optimized when generating native code")]
    NoOptimization = 0x0040,
    [Description("Reserved: conforming implementations can ignore")]
    PreserveSig = 0x0080,
    [Description("Reserved: shall be zero in conforming implementations")]
    InternalCall = 0x1000,
  }
}

// II.23.1.12
[Flags]
enum MethodSemanticsAttributes : ushort
{
  [Description("Setter for property")]
  Setter = 0x0001,
  [Description("Getter for property")]
  Getter = 0x0002,
  [Description("Other method for property or event")]
  Other = 0x0004,
  [Description("AddOn method for event. This refers to the required add_ method for events. (§22.13)")]
  AddOn = 0x0008,
  [Description("RemoveOn method for event. This refers to the required remove_ method for events. (§22.13)")]
  RemoveOn = 0x0010,
  [Description("Fire method for event. This refers to the optional raise_ method for events. (§22.13)")]
  Fire = 0x0020,
}

// II.23.1.13
[Flags]
public enum ParamAttributes : ushort
{
  [Description("Param is [In]")]
  In = 0x0001,
  [Description("Param is [out]")]
  Out = 0x0002,
  [Description("Param is optional")]
  Optional = 0x0010,
  [Description("Param has default value")]
  HasDefault = 0x1000,
  [Description("Param has FieldMarshal")]
  HasFieldMarshal = 0x2000,
}

// II.23.1.14
[Flags]
enum PropertyAttributes : ushort
{
  [Description("Property is special")]
  SpecialName = 0x0200,
  [Description("Runtime(metadata internal APIs) should check name encoding")]
  RTSpecialName = 0x0400,
  [Description("Property has default")]
  HasDefault = 0x1000,
}


// II.23.1.15
sealed class TypeAttributes : CodeNode
{
  public uint Data;
  public VisibilityAttributes Visibility { get; private set; }
  public LayoutAttributes Layout { get; private set; }
  public ClassSemanticsAttributes ClassSemantics { get; private set; }
  public StringInteropFormatAttributes StringInteropFormat { get; private set; }
  public AdditionalFlags Flags { get; private set; }

  protected override void InnerRead() {
    var data = Bytes.Read<uint>();
    Visibility = (VisibilityAttributes)(data & visibilityMask);
    Layout = (LayoutAttributes)(data & layoutMask);
    ClassSemantics = (ClassSemanticsAttributes)(data & classSemanticsMask);
    StringInteropFormat = (StringInteropFormatAttributes)(data & stringInteropFormatMask);
    Flags = (AdditionalFlags)(data & flagsMask);
  }

  public override string NodeValue => (new Enum[] { Visibility, Layout, ClassSemantics, StringInteropFormat, Flags }).GetString();

  public override string Description => string.Join("\n", Visibility.Describe()
    .Concat(Layout.Describe())
    .Concat(ClassSemantics.Describe())
    .Concat(StringInteropFormat.Describe())
    .Concat(Flags.Describe()));

  const uint visibilityMask = 0x00000007;

  public enum VisibilityAttributes : uint
  {
    [Description("Class has no public scope")]
    NotPublic = 0x00000000,
    [Description("Class has public scope")]
    Public = 0x00000001,
    [Description("Class is nested with public visibility")]
    NestedPublic = 0x00000002,
    [Description("Class is nested with private visibility")]
    NestedPrivate = 0x00000003,
    [Description("Class is nested with family visibility")]
    NestedFamily = 0x00000004,
    [Description("Class is nested with assembly visibility")]
    NestedAssembly = 0x00000005,
    [Description("Class is nested with family and assembly visibility")]
    NestedFamANDAssem = 0x00000006,
    [Description("Class is nested with family or assembly visibility")]
    NestedFamORAssem = 0x00000007,
  }

  const uint layoutMask = 0x00000018;

  public enum LayoutAttributes : uint
  {
    [Description("Class fields are auto-laid out")]
    AutoLayout = 0x00000000,
    [Description("Class fields are laid out sequentially")]
    SequentialLayout = 0x00000008,
    [Description("Layout is supplied explicitly")]
    ExplicitLayout = 0x00000010,
  }

  const uint classSemanticsMask = 0x00000020;

  public enum ClassSemanticsAttributes : uint
  {
    [Description("Type is a class")]
    Class = 0x00000000,
    [Description("Type is an interface")]
    Interface = 0x00000020,
  }

  const uint stringInteropFormatMask = 0x00030000;
  public enum StringInteropFormatAttributes : uint
  {
    [Description("LPSTR is interpreted as ANSI")]
    AnsiClass = 0x00000000,
    [Description("LPSTR is interpreted as Unicode")]
    UnicodeClass = 0x00010000,
    [Description("LPSTR is interpreted automatically")]
    AutoClass = 0x00020000,
    [Description("A non-standard encoding specified by CustomStringFormatMask, look at bits masked by 0x00C00000 for meaning, unspecified")]
    CustomFormatClass = 0x00030000,
  }

  const uint flagsMask = ~visibilityMask & ~layoutMask & ~classSemanticsMask & ~stringInteropFormatMask;
  [Flags]
  public enum AdditionalFlags : uint
  {
    [Description("Class is abstract")]
    Abstract = 0x00000080,
    [Description("Class cannot be extended")]
    Sealed = 0x00000100,
    [Description("Class name is special")]
    SpecialName = 0x00000400,
    [Description("Class/Interface is imported")]
    Import = 0x00001000,
    [Description("Reserved (Class is serializable)")]
    Serializable = 0x00002000,
    [Description("Initialize the class before first static field access")]
    BeforeFieldInit = 0x00100000,
    [Description("CLI provides 'special' behavior, depending upon the name of the Type")]
    RTSpecialName = 0x00000800,
    [Description("Type has security associate with it")]
    HasSecurity = 0x00040000,
    [Description("This ExportedType entry is a type forwarder")]
    IsTypeForwarder = 0x00200000,
  }
}

// II.23.1.16
enum ElementType : byte
{
  [Description("Marks end of a list")]
  End = 0x00,
  [Description("void")]
  Void = 0x01,
  [Description("bool")]
  Boolean = 0x02,
  [Description("char")]
  Char = 0x03,
  [Description("sbyte")]
  Int1 = 0x04,
  [Description("byte")]
  UInt1 = 0x05,
  [Description("short")]
  Int2 = 0x06,
  [Description("ushort")]
  UInt2 = 0x07,
  [Description("int")]
  Int4 = 0x08,
  [Description("uint")]
  UInt4 = 0x09,
  [Description("long")]
  Int8 = 0x0a,
  [Description("ulong")]
  UInt8 = 0x0b,
  [Description("float")]
  Real4 = 0x0c,
  [Description("double")]
  Real8 = 0x0d,
  [Description("string")]
  String = 0x0e,
  [Description("Followed by type")]
  Ptr = 0x0f,
  [Description("Followed by type")]
  ByRef = 0x10,
  [Description("Followed by TypeDef or TypeRef token")]
  ValueType = 0x11,
  [Description("Followed by TypeDef or TypeRef token")]
  Class = 0x12,
  [Description("Generic parameter in a generic type definition, represented as number (compressed unsigned integer)")]
  Var = 0x13,
  [Description("type rank boundsCount bound1 ... loCount lo1 ...")]
  Array = 0x14,
  [Description("Generic type instantiation. Followed by type type-arg-count type-1 ... type-n")]
  GenericInst = 0x15,
  [Description("System.TypedReference")]
  TypedByRef = 0x16,
  [Description("System.IntPtr")]
  IntPtr = 0x18,
  [Description("System.UIntPtr")]
  UIntPtr = 0x19,
  [Description("Followed by full method signature")]
  Fnptr = 0x1b,
  [Description("System.Object")]
  Object = 0x1c,
  [Description("Single-dim array with 0 lower bound")]
  SzArray = 0x1d,
  [Description("Generic parameter in a generic method definition, represented as number (compressed unsigned integer)")]
  MVar = 0x1e,
  [Description("Required modifier : followed by a TypeDef or TypeRef token")]
  CModReqd = 0x1f,
  [Description("Optional modifier : followed by a TypeDef or TypeRef token")]
  CModOpt = 0x20,
  [Description("Implemented within the CLI")]
  Internal = 0x21,
  [Description("Or'd with following element types")]
  Modifier = 0x40,
  [Description("Sentinel for vararg method signature")]
  Sentinel = 0x41,
  [Description("Denotes a local variable that points at a pinned object")]
  Pinned = 0x45,
  [Description("Indicates an argument of type System.Type.")]
  Unknown1 = 0x50,
  [Description("Used in custom attributes to specify a boxed object (§II.23.3).")]
  Unknown2 = 0x51,
  [Description("Reserved")]
  Unknown3 = 0x52,
  [Description("Used in custom attributes to indicate a FIELD (§II.22.10, II.23.3).")]
  Unknown4 = 0x53,
}
static class ElementTypeExtensions
{
  // Print enum values the way you'd expect in C#
  public static string S(this ElementType type) => type switch {
    // ElementType.End => "",
    ElementType.Void => "void",
    ElementType.Boolean => "bool",
    ElementType.Char => "char",
    ElementType.Int1 => "sbyte",
    ElementType.UInt1 => "byte",
    ElementType.Int2 => "short",
    ElementType.UInt2 => "ushort",
    ElementType.Int4 => "int",
    ElementType.UInt4 => "uint",
    ElementType.Int8 => "long",
    ElementType.UInt8 => "ulong",
    ElementType.Real4 => "float",
    ElementType.Real8 => "double",

    ElementType.String => "string",
    // ElementType.Ptr => "",
    // ElementType.ByRef => "",
    ElementType.ValueType => "valuetype",
    ElementType.Class => "class",
    // ElementType.Var => "",
    // ElementType.Array => "",
    // ElementType.GenericInst => "",
    ElementType.TypedByRef => "typedref",
    ElementType.IntPtr => "IntPtr",
    ElementType.UIntPtr => "UIntPtr",
    // ElementType.Fnptr => "",
    ElementType.Object => "object",
    // ElementType.SzArray => "",
    // ElementType.MVar => "",
    // ElementType.CModReqd => "",
    // ElementType.CModOpt => "",
    // ElementType.Internal => "",
    // ElementType.Modifier => "",
    // ElementType.Sentinel => "",
    ElementType.Pinned => "pinned",
    // ElementType.Unknown1 => "",
    // ElementType.Unknown2 => "",
    // ElementType.Unknown3 => "",
    // ElementType.Unknown4 => "",

    _ => type.ToString(),
  };
}

// II.24.2.1
// MAYBE split to a new file https://devblogs.microsoft.com/oldnewthing/20190916-00/?p=102892
sealed class MetadataRoot : CodeNode
{
  [Description("Magic signature for physical metadata : 0x424A5342.")]
  public uint Signature;
  [Description("Major version, 1 (ignore on read)")]
  [Expected(1)]
  public ushort MajorVersion;
  [Description("Minor version, 1 (ignore on read)")]
  [Expected(1)]
  public ushort MinorVersion;
  [Description("Reserved, always 0 (§II.24.1).")]
  public uint Reserved;
  [Description("Number of bytes allocated to hold version string, rounded up to a multiple of four.")]
  public uint Length;
  [Description("UTF8-encoded null-terminated version string.")]
  public NullTerminatedString Version = new NullTerminatedString(Encoding.UTF8, 4);
  [Description("Reserved, always 0 (§II.24.1).")]
  [Expected(0)]
  public ushort Flags;
  [Description("Number of streams.")]
  public ushort Streams;
  [OrderedField]
  public StreamHeader[] StreamHeaders;

  protected override int GetCount(string field) => field switch {
    nameof(StreamHeaders) => Streams,
    _ => base.GetCount(field),
  };
}

// II.24.2.2
sealed class StreamHeader : CodeNode
{
  [Description("Memory offset to start of this stream from start of the metadata root(§II.24.2.1)")]
  public uint Offset;
  [Description("Size of this stream in bytes, shall be a multiple of 4.")]
  public uint Size;
  [Description("Name of the stream as null-terminated variable length array of ASCII characters, padded to the next 4 - byte boundary with null characters.")]
  public NullTerminatedString Name = new NullTerminatedString(Encoding.ASCII, 4);
}

abstract class Heap<T> : CodeNode
{
  int size;
  SortedList<int, (T, CodeNode)> children = new SortedList<int, (T, CodeNode)>();

  public Heap(int size) {
    this.size = size;
  }

  protected override void InnerRead() {
    // Parsing the data now isn't possible
    Bytes.Stream.Position += size;
  }

  protected abstract (T, CodeNode) ReadChild(int index);

  protected (T t, CodeNode node) AddChild(int index) {
    using (Bytes.TempReposition(Start + index)) {
      if (!children.TryGetValue(index, out var childpair)) {
        var (t, child) = ReadChild(index);
        if (child == null) {
          return (t, this);
        }
        child.NodeName = $"{GetType().Name}[{index}]";

        childpair = (t, child);
        children.Add(index, childpair);

        Children.Add(child);

        AdjustChildRanges(index, child);
      }

      return childpair;
    }
  }

  //TODO(pedant) Binary heaps members are allowed to overlap to save space, allow for this in javascript
  void AdjustChildRanges(int index, CodeNode child) {
    var chI = children.IndexOfKey(index);
    if (chI != 0) {
      AdjustChildren(children.Values[chI - 1].Item2, child);
    }

    if (chI + 1 != children.Count) {
      AdjustChildren(child, children.Values[chI + 1].Item2);
    }
  }

  static void AdjustChildren(CodeNode before, CodeNode after) {
    if (before.End > after.Start) {
      before.Description = @"(Sharing bytes with the next element...)";
      before.End = after.Start;
    }
  }

  public T Get(int i) => AddChild(i).t;
  public CodeNode GetNode(int i) => AddChild(i).node;

  public sealed class EncodedLength : CodeNode
  {
    public int length;

    public override string NodeValue => length.ToString();

    protected override void InnerRead() {
      var first = Bytes.Read<byte>();
      if ((first & 0b1000_0000) == 0) {
        length = first & 0x7F;
        Description = "Starts with bit pattern 0 so Length is 1 byte";
      } else if ((first & 0b1100_0000) == 0b1000_0000) {
        var second = Bytes.Read<byte>();
        length = ((first & 0x3F) << 8) + second;
        Description = "Starts with bit pattern 10 so Length is 2 bytes";
      } else if ((first & 0b1110_0000) == 0b1100_0000) {
        var second = Bytes.Read<byte>();
        var third = Bytes.Read<byte>();
        var fourth = Bytes.Read<byte>();
        length = ((first & 0x1F) << 24) + (second << 16) + (third << 8) + fourth;
        Description = "Starts with bit pattern 110 so Length is 4 bytes";
      } else {
        throw new InvalidOperationException($"Heap byte {Bytes.Stream.Position - 1:X} can't start with bits 1111...");
      }
    }
  }
}

// II.24.2.3
sealed class StringHeap : Heap<string>
{
  public StringHeap(int size)
      : base(size) {
  }

  protected override (string, CodeNode) ReadChild(int index) {
    var s = new NullTerminatedString(Encoding.UTF8, 1) { Bytes = Bytes };
    s.Read();

    return (s.Str, s);
  }
}

// II.24.2.4
sealed class UserStringHeap : Heap<string>
{
  public UserStringHeap(int size)
      : base(size) {
  }

  protected override (string, CodeNode) ReadChild(int index) {
    var entry = Bytes.ReadClass<Entry>();
    return (entry.String.Str, entry);
  }

  sealed class Entry : CodeNode
  {
    public EncodedLength Length;
    public FixedLengthString String;

    public override string NodeValue => String.NodeValue;

    protected override void InnerRead() {
      AddChild(nameof(Length));
      String = new FixedLengthString(Length.length);
      AddChild(nameof(String));
    }
  }

  sealed class FixedLengthString : CodeNode // MAYBE refactor all to record types
  {
    public string Str { get; private set; } = "oops unset!!";

    int length;
    public FixedLengthString(int length) {
      this.length = length;
    }

    protected override void InnerRead() {
      var arr = new byte[length - 1]; // skip terminal byte
      if (!Bytes.Stream.TryReadWholeArray(arr, out var error)) {
        Errors.Add(error);
        return;
      }
      Str = Encoding.Unicode.GetString(arr);
      NodeValue = Str.GetString();
    }
  }
}

sealed class BlobHeap : Heap<object>
{
  public BlobHeap(int size)
      : base(size) {
  }

  CodeNode customEntry; // hack so GetCustom() can pass type info into ReadChild()
  protected override (object, CodeNode) ReadChild(int index) {
    if (customEntry != null) {
      var custom = customEntry;
      customEntry = null; // set null immediately so renentrant GetCustom calls don't see customEntry
      custom.Read();
      var ret = (((IEntry)custom).IValue, custom);
      return ret;
    }
    var entry = Bytes.ReadClass<BytesEntry>();

    if (entry.Blob.arr.Length == 0) {
      entry.Description = "Empty blob";
      entry.Children.Clear();
    }
    return (entry.Blob.arr, entry);
  }

  public byte GetByte(int index) {
    using (Bytes.TempReposition(Start + index)) {
      Bytes.ReadClass<EncodedLength>();
      return Bytes.Read<byte>();
    }
  }

  public T GetCustom<T>(int i) where T : CodeNode, new() {
    if (customEntry != null) throw new InvalidOperationException();
    customEntry = new CustomEntry<T> { Bytes = Bytes };
    var o = AddChild(i);

    if (customEntry != null) {
      if (o.t is T) {
        customEntry = null; // Reading the same bytes as the same type again should be idempotent
      } else {
        throw new NotImplementedException("Custom read of data overlaps with another type");
      }
    }
    return (T)o.t;
  }

  sealed class BytesEntry : CodeNode
  {
    public EncodedLength Length;
    public ByteArrayNode Blob;

    public override string NodeValue => Blob.NodeValue;

    protected override void InnerRead() {
      AddChild(nameof(Length));
      Blob = new ByteArrayNode(Length.length);
      AddChild(nameof(Blob));
    }
  }

  sealed class CustomEntry<T> : CodeNode, IEntry where T : CodeNode, new()
  {
    public EncodedLength Length;
    public T Value;
    public object IValue => Value;

    public override string NodeValue => Value.NodeValue;

    protected override void InnerRead() {
      AddChild(nameof(Length));
      AddChild(nameof(Value));
      if (Value.End != Value.Start + Length.length) {
        Errors.Add($"Custom data {typeof(T).Name} isn't size of entire blob");
      }
      Children.Last().NodeName = typeof(T).Name;
    }
  }

  interface IEntry
  {
    object IValue { get; }
  }

  public sealed class ByteArrayNode : CodeNode
  {
    public byte[] arr;
    public ByteArrayNode(int length) {
      arr = new byte[length];
    }
    protected override void InnerRead() {
      Bytes.Stream.ReadWholeArray(arr);
      NodeValue = arr.GetString();
    }
  }

}

// II.24.2.5
sealed class GuidHeap : Heap<Guid>
{
  public GuidHeap(int size)
      : base(size) {
  }

  protected override (Guid, CodeNode) ReadChild(int index) {
    if (index == 0) return (Guid.Empty, null);

    Bytes.Stream.Position -= index; // Undo ReadChild offset

    const int size = 16;
    Bytes.Stream.Position += (index - 1) * size; // GuidHeap is indexed from 1

    var g = Bytes.ReadClass<StructNode<Guid>>();
    return (g.t, g);
  }
}

sealed class TildeStreamRows : CodeNode
{
  int count;
  public TildeStreamRows(int count) {
    this.count = count;
  }
  //TODO(Descriptions) give a name for each row. Using StructNode<uint> keeps each row its own size
  public StructNode<uint>[] Rows;

  protected override int GetCount(string field) => count;
}

// II.24.2.6
sealed class TildeStream : CodeNode
{
  public Section Section { get; private set; }
  public TildeStream(Section section) {
    Section = section;
  }

  [OrderedField] public TildeData TildeData;
  [OrderedField] public TildeStreamRows Rows;

  [OrderedField] public Module[] Modules;
  [OrderedField] public TypeRef[] TypeRefs;
  [OrderedField] public TypeDef[] TypeDefs;
  [OrderedField] public Field[] Fields;
  [OrderedField] public MethodDef[] MethodDefs;
  [OrderedField] public Param[] Params;
  [OrderedField] public InterfaceImpl[] InterfaceImpls;
  [OrderedField] public MemberRef[] MemberRefs;
  [OrderedField] public Constant[] Constants;
  [OrderedField] public CustomAttribute[] CustomAttributes;
  [OrderedField] public FieldMarshal[] FieldMarshals;
  [OrderedField] public DeclSecurity[] DeclSecuritys;
  [OrderedField] public ClassLayout[] ClassLayouts;
  [OrderedField] public FieldLayout[] FieldLayouts;
  [OrderedField] public StandAloneSig[] StandAloneSigs;
  [OrderedField] public EventMap[] EventMaps;
  [OrderedField] public Event[] Events;
  [OrderedField] public PropertyMap[] PropertyMaps;
  [OrderedField] public Property[] Properties;
  [OrderedField] public MethodSemantics[] MethodSemantics;
  [OrderedField] public MethodImpl[] MethodImpls;
  [OrderedField] public ModuleRef[] ModuleRefs;
  [OrderedField] public TypeSpec[] TypeSpecs;
  [OrderedField] public ImplMap[] ImplMaps;
  [OrderedField] public FieldRVA[] FieldRVAs;
  [OrderedField] public Assembly[] Assemblies;
  [OrderedField] public AssemblyProcessor[] AssemblyProcessors;
  [OrderedField] public AssemblyOS[] AssemblyOSs;
  [OrderedField] public AssemblyRef[] AssemblyRefs;
  [OrderedField] public AssemblyRefProcessor[] AssemblyRefProcessors;
  [OrderedField] public AssemblyRefOS[] AssemblyRefOSs;
  [OrderedField] public FileTable[] Files;
  [OrderedField] public ExportedType[] ExportedTypes;
  [OrderedField] public ManifestResource[] ManifestResources;
  [OrderedField] public Nestedclass[] NestedClasses;
  [OrderedField] public GenericParam[] GenericParams;
  [OrderedField] public MethodSpec[] MethodSpecs;
  [OrderedField] public GenericParamConstraint[] GenericParamConstraints;

  Dictionary<MetadataTableFlags, IEnumerable<CodeNode>> streamNodes = new Dictionary<MetadataTableFlags, IEnumerable<CodeNode>>();

  protected override void InnerRead() {
    AddChild(nameof(TildeData));
    Rows = new TildeStreamRows(((ulong)TildeData.Valid).CountSetBits());
    AddChild(nameof(Rows));

    int row = 0;
    foreach (var value in Enum.GetValues(typeof(MetadataTableFlags))) {
      var flag = (MetadataTableFlags)value;
      if (!TildeData.Valid.HasFlag(flag))
        continue;
      ReadTables(flag, row);
      ++row;
    }

    if (TildeData.HeapSizes != 0)
      throw new NotImplementedException("HeapSizes aren't 4-byte-aware"); //TODO(Index4Bytes)
    if (Rows.Rows.Max(r => r.t) >= (1 << 11))
      throw new NotImplementedException("CodedIndex aren't 4-byte-aware"); //TODO(Index4Bytes)
  }

  void ReadTables(MetadataTableFlags flag, int row) {
    var count = (int)Rows.Rows[row].t;

    var name = GetFieldName(flag);
    AddChildren(name, count);

    var nodes = (IEnumerable<CodeNode>)GetType().GetField(name).GetValue(this);
    streamNodes.Add(flag, nodes);
  }

  string GetFieldName(MetadataTableFlags flag) => flag switch {
    MetadataTableFlags.Module => nameof(Modules),
    MetadataTableFlags.TypeRef => nameof(TypeRefs),
    MetadataTableFlags.TypeDef => nameof(TypeDefs),
    MetadataTableFlags.Field => nameof(Fields),
    MetadataTableFlags.MethodDef => nameof(MethodDefs),
    MetadataTableFlags.Param => nameof(Params),
    MetadataTableFlags.InterfaceImpl => nameof(InterfaceImpls),
    MetadataTableFlags.MemberRef => nameof(MemberRefs),
    MetadataTableFlags.Constant => nameof(Constants),
    MetadataTableFlags.CustomAttribute => nameof(CustomAttributes),
    MetadataTableFlags.FieldMarshal => nameof(FieldMarshals),
    MetadataTableFlags.DeclSecurity => nameof(DeclSecuritys),
    MetadataTableFlags.ClassLayout => nameof(ClassLayouts),
    MetadataTableFlags.FieldLayout => nameof(FieldLayouts),
    MetadataTableFlags.StandAloneSig => nameof(StandAloneSigs),
    MetadataTableFlags.EventMap => nameof(EventMaps),
    MetadataTableFlags.Event => nameof(Events),
    MetadataTableFlags.PropertyMap => nameof(PropertyMaps),
    MetadataTableFlags.Property => nameof(Properties),
    MetadataTableFlags.MethodSemantics => nameof(MethodSemantics),
    MetadataTableFlags.MethodImpl => nameof(MethodImpls),
    MetadataTableFlags.ModuleRef => nameof(ModuleRefs),
    MetadataTableFlags.TypeSpec => nameof(TypeSpecs),
    MetadataTableFlags.ImplMap => nameof(ImplMaps),
    MetadataTableFlags.FieldRVA => nameof(FieldRVAs),
    MetadataTableFlags.Assembly => nameof(Assemblies),
    MetadataTableFlags.AssemblyProcessor => nameof(AssemblyProcessors),
    MetadataTableFlags.AssemblyOS => nameof(AssemblyOSs),
    MetadataTableFlags.AssemblyRef => nameof(AssemblyRefs),
    MetadataTableFlags.AssemblyRefProcessor => nameof(AssemblyRefProcessors),
    MetadataTableFlags.AssemblyRefOS => nameof(AssemblyRefOSs),
    MetadataTableFlags.File => nameof(Files),
    MetadataTableFlags.ExportedType => nameof(ExportedTypes),
    MetadataTableFlags.ManifestResource => nameof(ManifestResources),
    MetadataTableFlags.NestedClass => nameof(NestedClasses),
    MetadataTableFlags.GenericParam => nameof(GenericParams),
    MetadataTableFlags.MethodSpec => nameof(MethodSpecs),
    MetadataTableFlags.GenericParamConstraint => nameof(GenericParamConstraints),
    _ => throw new InvalidOperationException("Not a real MetadataTableFlags " + flag),
  };


  public CodeNode GetCodeNode(MetadataTableFlags flag, int i) => streamNodes[flag].Skip(i).First();
}

sealed class TildeData : CodeNode
{
  [Description("Reserved, always 0 (§II.24.1).")]
  [Expected(0)]
  public uint Reserved;
  [Description("Major version of table schemata; shall be 2 (§II.24.1).")]
  [Expected(2)]
  public byte MajorVersion;
  [Description("Minor version of table schemata; shall be 0 (§II.24.1).")]
  [Expected(0)]
  public byte MinorVersion;
  [Description("Bit vector for heap sizes. (Allowed to be non-zero but I haven't implemented that...)")]
  [Expected(0)]
  public TildeDataHeapSizes HeapSizes;
  [Description("Reserved, always 1 (§II.24.1).")]
  [Expected(1)]
  public byte Reserved2;
  [Description("Bit vector of present tables, let n be the number of bits that are 1.")]
  public MetadataTableFlags Valid;
  [Description("Bit vector of sorted tables.")]
  public MetadataTableFlags Sorted;
}

[Flags]
enum TildeDataHeapSizes : byte
{
  StringHeapIndexWide = 0x01,
  GuidHeapIndexWide = 0x02,
  BlobHeapIndexWide = 0x04,
}

sealed class StringHeapIndex : CodeNode
{
  public short Index;
  // ushort? shortIndex;
  // uint? intIndex; //TODO(Index4Bytes) not implemented: Make all Index generic for 4-byte index; ditto ALL HeapIndexes
  // public int Index => (int)(intIndex ?? shortIndex);


  protected override void InnerRead() {
    Index = Bytes.Read<short>();

    NodeValue = Bytes.StringHeap.Get(Index);
    Description = $"String Heap index {Index:X}";
    Link = Bytes.StringHeap.GetNode(Index);
  }
}

sealed class UserStringHeapIndex : CodeNode
{
  public short Index;

  protected override void InnerRead() {
    Index = Bytes.Read<short>();

    NodeValue = Bytes.UserStringHeap.Get(Index).GetString();
    Description = $"User String Heap index {Index:X}";
    Link = Bytes.UserStringHeap.GetNode(Index);
  }
}

sealed class BlobHeapIndex : CodeNode
{
  public short Index;

  protected override void InnerRead() {
    Index = Bytes.Read<short>();

    NodeValue = Bytes.BlobHeap.Get(Index).GetString();
    Description = $"Blob Heap index {Index:X}";
    Link = Bytes.BlobHeap.GetNode(Index);
  }
}

sealed class GuidHeapIndex : CodeNode
{
  public short Index;

  protected override void InnerRead() {
    Index = Bytes.Read<short>();

    NodeValue = Bytes.GuidHeap.Get(Index).GetString();
    Description = $"Guid Heap index {Index:X}";
    Link = Bytes.GuidHeap.GetNode(Index);
  }
}

//TODO(link) implement all CodedIndex
sealed class UnknownCodedIndex : CodeNode
{
  public ushort Index;

  public override string NodeValue => Index.GetString();

  protected override void InnerRead() {
    Index = Bytes.Read<ushort>();
  }
}

sealed class TableIndex<T> : SizedTableIndex<short, T> where T : CodeNode
{

}

sealed class FatTableIndex<T> : SizedTableIndex<int, T> where T : CodeNode
{

}

abstract class SizedTableIndex<Ti, Ts> : CodeNode where Ti : struct where Ts : CodeNode
{
  static FieldInfo fieldInfo; // unique to each generic class
  static SizedTableIndex() {
    // A little ugliness here makes each use of TableIndex very readable.
    fieldInfo = typeof(TildeStream).GetFields()
      .Where(field => field.FieldType.IsArray && field.FieldType.GetElementType() == typeof(Ts))
      .Single();
  }

  public int Index;

  // Can be null
  public Ts Value {
    get {
      if (Index == 0) {
        return null;
      }

      var table = (Ts[])fieldInfo.GetValue(Bytes.TildeStream);
      return table[Index - 1];
    }
  }

  public override CodeNode Link => Value;
  public override string NodeValue => Value?.NodeValue ?? "(null)";

  protected override void InnerRead() {
    Index = Bytes.Read<Ti>().GetInt32();
  }
}

abstract class CodedIndex : CodeNode
{
  CodedIndex() { } // Don't allow subclassing from other types

  NullRow nullRow;

  public int Index { get; private set; }

  // Don't invoke GetLink() until after *all* TildeStream rows have been read.
  public override string NodeValue => (nullRow ?? GetLink()).NodeValue;
  public override CodeNode Link => nullRow ?? GetLink();

  protected override void InnerRead() {
    var readData = Bytes.Read<ushort>();
    if (readData == 0) {
      nullRow = new NullRow();
    } else {
      Index = GetIndex(readData);
    }
  }

  protected abstract int GetIndex(int readData);

  protected abstract CodeNode GetLink();

  public sealed class TypeDefOrRef : CodedIndex
  {

    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x3);
      return (readData >> 2) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.TypeDef: return Bytes.TildeStream.TypeDefs[Index];
        case Tag.TypeRef: return Bytes.TildeStream.TypeRefs[Index];
        case Tag.TypeSpec: return Bytes.TildeStream.TypeSpecs[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      TypeDef = 0,
      TypeRef = 1,
      TypeSpec = 2,
    }
  }

  public sealed class HasConstant : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x3);
      return (readData >> 2) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.Field: return Bytes.TildeStream.Fields[Index];
        case Tag.Param: return Bytes.TildeStream.Params[Index];
        case Tag.Property: return Bytes.TildeStream.Properties[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      Field = 0,
      Param = 1,
      Property = 2,
    }
  }

  public sealed class HasCustomAttribute : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x1F);
      return (readData >> 5) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.MethodDef: return Bytes.TildeStream.MethodDefs[Index];
        case Tag.Field: return Bytes.TildeStream.Fields[Index];
        case Tag.TypeRef: return Bytes.TildeStream.TypeRefs[Index];
        case Tag.TypeDef: return Bytes.TildeStream.TypeDefs[Index];
        case Tag.Param: return Bytes.TildeStream.Params[Index];
        case Tag.InterfaceImpl: return Bytes.TildeStream.InterfaceImpls[Index];
        case Tag.MemberRef: return Bytes.TildeStream.MemberRefs[Index];
        case Tag.Module: return Bytes.TildeStream.Modules[Index];
        //case Tag.Permission: return Bytes.TildeStream.Permissions[Index]; //TODO(pedant) DeclSecuritys?
        case Tag.Property: return Bytes.TildeStream.Properties[Index];
        case Tag.Event: return Bytes.TildeStream.Events[Index];
        case Tag.StandAloneSig: return Bytes.TildeStream.StandAloneSigs[Index];
        case Tag.ModuleRef: return Bytes.TildeStream.ModuleRefs[Index];
        case Tag.TypeSpec: return Bytes.TildeStream.TypeSpecs[Index];
        case Tag.Assembly: return Bytes.TildeStream.Assemblies[Index];
        case Tag.AssemblyRef: return Bytes.TildeStream.AssemblyRefs[Index];
        case Tag.File: return Bytes.TildeStream.Files[Index];
        case Tag.ExportedType: return Bytes.TildeStream.ExportedTypes[Index];
        case Tag.ManifestResource: return Bytes.TildeStream.ManifestResources[Index];
        case Tag.GenericParam: return Bytes.TildeStream.GenericParams[Index];
        case Tag.GenericParamConstraint: return Bytes.TildeStream.GenericParamConstraints[Index];
        case Tag.MethodSpec: return Bytes.TildeStream.MethodSpecs[Index];
      }
      throw new NotImplementedException(tag.ToString());
    }

    enum Tag
    {
      MethodDef = 0,
      Field = 1,
      TypeRef = 2,
      TypeDef = 3,
      Param = 4,
      InterfaceImpl = 5,
      MemberRef = 6,
      Module = 7,
      Permission = 8,
      Property = 9,
      Event = 10,
      StandAloneSig = 11,
      ModuleRef = 12,
      TypeSpec = 13,
      Assembly = 14,
      AssemblyRef = 15,
      File = 16,
      ExportedType = 17,
      ManifestResource = 18,
      GenericParam = 19,
      GenericParamConstraint = 20,
      MethodSpec = 21,
    }
  }

  public sealed class HasFieldMarshall : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x1);
      return (readData >> 1) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.Field: return Bytes.TildeStream.Fields[Index];
        case Tag.Param: return Bytes.TildeStream.Params[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      Field = 0,
      Param = 1,
    }
  }

  public sealed class HasDeclSecurity : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x3);
      return (readData >> 2) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.TypeDef: return Bytes.TildeStream.TypeDefs[Index];
        case Tag.MethodDef: return Bytes.TildeStream.MethodDefs[Index];
        case Tag.Assembly: return Bytes.TildeStream.Assemblies[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      TypeDef = 0,
      MethodDef = 1,
      Assembly = 2,
    }
  }

  public sealed class MemberRefParent : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x7);
      return (readData >> 3) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.TypeDef: return Bytes.TildeStream.TypeDefs[Index];
        case Tag.TypeRef: return Bytes.TildeStream.TypeRefs[Index];
        case Tag.ModuleRef: return Bytes.TildeStream.ModuleRefs[Index];
        case Tag.MethodDef: return Bytes.TildeStream.MethodDefs[Index];
        case Tag.TypeSpec: return Bytes.TildeStream.TypeSpecs[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      TypeDef = 0,
      TypeRef = 1,
      ModuleRef = 2,
      MethodDef = 3,
      TypeSpec = 4,
    }
  }

  public sealed class HasSemantics : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x1);
      return (readData >> 1) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.Event: return Bytes.TildeStream.Events[Index];
        case Tag.Property: return Bytes.TildeStream.Properties[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      Event = 0,
      Property = 1,
    }
  }

  public sealed class MethodDefOrRef : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x1);
      return (readData >> 1) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.MethodDef: return Bytes.TildeStream.MethodDefs[Index];
        case Tag.MemberRef: return Bytes.TildeStream.MemberRefs[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      MethodDef = 0,
      MemberRef = 1,
    }
  }

  public sealed class MemberForwarded : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x1);
      return (readData >> 1) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.Field: return Bytes.TildeStream.Fields[Index];
        case Tag.MethodDef: return Bytes.TildeStream.MethodDefs[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      Field = 0,
      MethodDef = 1,
    }
  }

  public sealed class Implementation : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x3);
      return (readData >> 2) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.File: return Bytes.TildeStream.Files[Index];
        case Tag.AssemblyRef: return Bytes.TildeStream.AssemblyRefs[Index];
        case Tag.ExportedType: return Bytes.TildeStream.ExportedTypes[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      File = 0,
      AssemblyRef = 1,
      ExportedType = 2,
    }
  }

  public sealed class CustomAttributeType : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x7);
      return (readData >> 3) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.MethodDef: return Bytes.TildeStream.MethodDefs[Index];
        case Tag.MemberRef: return Bytes.TildeStream.MemberRefs[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      MethodDef = 2,
      MemberRef = 3,
    }
  }

  public sealed class ResolutionScope : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x3);
      return (readData >> 2) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.Module: return Bytes.TildeStream.Modules[Index];
        case Tag.ModuleRef: return Bytes.TildeStream.ModuleRefs[Index];
        case Tag.AssemblyRef: return Bytes.TildeStream.AssemblyRefs[Index];
        case Tag.TypeRef: return Bytes.TildeStream.TypeRefs[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      Module = 0,
      ModuleRef = 1,
      AssemblyRef = 2,
      TypeRef = 3,
    }
  }

  public sealed class TypeOrMethodDef : CodedIndex
  {
    Tag tag;

    protected override int GetIndex(int readData) {
      tag = (Tag)(readData & 0x1);
      return (readData >> 1) - 1;
    }

    protected override CodeNode GetLink() {
      switch (tag) {
        case Tag.TypeDef: return Bytes.TildeStream.TypeDefs[Index];
        case Tag.MethodDef: return Bytes.TildeStream.MethodDefs[Index];
      }
      throw new InvalidOperationException(tag.ToString());
    }

    enum Tag
    {
      TypeDef = 0,
      MethodDef = 1,
    }
  }

  class NullRow : CodeNode
  {
    public override string NodeValue => "(null)";
  }
}
