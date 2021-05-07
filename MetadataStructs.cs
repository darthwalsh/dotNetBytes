using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

// II.23 Metadata logical format: other structures 

// ICanBeReadInOrder is written though reflection
#pragma warning disable 0649 // CS0649: Field '...' is never assigned to, and will always have its default value

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
class FieldAttributes : ICanRead, IHaveValue
{
    public Access access;
    public Flags flags;

    public CodeNode Read(Stream stream) {
        ushort value;
        var node = stream.ReadStruct(out value);

        access = (Access)(value & AccessMask);
        flags = (Flags)(value & FlagsMask);

        return node;
    }

    public object Value => new Enum[] { access, flags };

    const ushort AccessMask = 0x0007;

    public enum Access : ushort
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

    const ushort FlagsMask = unchecked((ushort)~AccessMask);
    [Flags]
    public enum Flags : ushort
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
class GenericParamAttributes : ICanRead, IHaveValue
{
    public Variance variance;
    public SpecialConstraint specialConstraint;

    public CodeNode Read(Stream stream) {
        ushort value;
        var node = stream.ReadStruct(out value);

        variance = (Variance)(value & VarianceMask);
        specialConstraint = (SpecialConstraint)(value & SpecialConstraintMask);

        return node;
    }

    public object Value => new Enum[] { variance, specialConstraint };

    const ushort VarianceMask = 0x0003;

    public enum Variance : ushort
    {
        [Description("The generic parameter is non-variant and has no special constraints")]
        None = 0x0000,
        [Description("The generic parameter is covariant")]
        Covariant = 0x0001,
        [Description("The generic parameter is contravariant")]
        Contravariant = 0x0002,
    }

    const ushort SpecialConstraintMask = 0x0004;

    public enum SpecialConstraint : ushort
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
class PInvokeAttributes : ICanRead, IHaveValue
{
    public CharacterSet characterSet;
    public CallingConvention callingConvention;
    public Flags flags;

    public CodeNode Read(Stream stream) {
        ushort value;
        var node = stream.ReadStruct(out value);

        characterSet = (CharacterSet)(value & CharacterSetMask);
        callingConvention = (CallingConvention)(value & CallingConventionMask);
        flags = (Flags)(value & FlagsMask);

        return node;
    }

    public object Value => new Enum[] { characterSet, callingConvention, flags };

    const ushort CharacterSetMask = 0x0007;

    public enum CharacterSet : ushort
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

    const ushort CallingConventionMask = 0x0100;

    public enum CallingConvention : ushort
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

    const ushort FlagsMask = unchecked((ushort)~CharacterSetMask & ~CallingConventionMask);
    [Flags]
    public enum Flags : ushort
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
class MethodAttributes : ICanRead, IHaveValue
{
    public MemberAccess memberAccess;
    public VtableLayout vtableLayout;
    public Flags flags;

    public CodeNode Read(Stream stream) {
        ushort value;
        var node = stream.ReadStruct(out value);

        memberAccess = (MemberAccess)(value & MemberAccessMask);
        vtableLayout = (VtableLayout)(value & VtableLayoutMask);
        flags = (Flags)(value & FlagsMask);

        return node;
    }

    public object Value => new Enum[] { memberAccess, vtableLayout, flags };

    const ushort MemberAccessMask = 0x0007;

    public enum MemberAccess : ushort
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

    const ushort VtableLayoutMask = 0x0100;

    public enum VtableLayout : ushort
    {
        [Description("Method reuses existing slot in vtable")]
        ReuseSlot = 0x0000,
        [Description("Method always gets a new slot in the vtable")]
        NewSlot = 0x0100,
    }

    const ushort FlagsMask = unchecked((ushort)~MemberAccessMask & ~VtableLayoutMask);
    [Flags]
    public enum Flags : ushort
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
class MethodImplAttributes : ICanRead, IHaveValue
{
    public CodeType codeType;
    public Managed managed;
    public Flags flags;

    public CodeNode Read(Stream stream) {
        ushort value;
        var node = stream.ReadStruct(out value);

        codeType = (CodeType)(value & CodeTypeMask);
        managed = (Managed)(value & ManagedMask);
        flags = (Flags)(value & FlagsMask);

        return node;
    }

    public object Value => new Enum[] { codeType, managed, flags };

    const ushort CodeTypeMask = 0x0003;

    public enum CodeType : ushort
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

    const ushort ManagedMask = 0x0004;

    public enum Managed : ushort
    {
        [Description("Method impl is unmanaged")]
        Unmanaged = 0x0004,
        [Description("Method impl is managed")]
        Managed = 0x0000,
    }

    const ushort FlagsMask = unchecked((ushort)~CodeTypeMask & ~ManagedMask);
    [Flags]
    public enum Flags : ushort
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
class TypeAttributes : ICanRead, IHaveValue
{
    public Visibility visibility;
    public Layout layout;
    public ClassSemantics classSemantics;
    public StringInteropFormat stringInteropFormat;
    public Flags flags;

    public CodeNode Read(Stream stream) {
        uint value;
        var node = stream.ReadStruct(out value);

        visibility = (Visibility)(value & VisibilityMask);
        layout = (Layout)(value & LayoutMask);
        classSemantics = (ClassSemantics)(value & ClassSemanticsMask);
        stringInteropFormat = (StringInteropFormat)(value & StringInteropFormatMask);
        flags = (Flags)(value & FlagsMask);

        return node;
    }

    public object Value => new Enum[] { visibility, layout, classSemantics, stringInteropFormat, flags };

    const uint VisibilityMask = 0x00000007;

    public enum Visibility : uint
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

    const uint LayoutMask = 0x00000018;

    public enum Layout : uint
    {
        [Description("Class fields are auto-laid out")]
        AutoLayout = 0x00000000,
        [Description("Class fields are laid out sequentially")]
        SequentialLayout = 0x00000008,
        [Description("Layout is supplied explicitly")]
        ExplicitLayout = 0x00000010,
    }

    const uint ClassSemanticsMask = 0x00000020;

    public enum ClassSemantics : uint
    {
        [Description("Type is a class")]
        Class = 0x00000000,
        [Description("Type is an interface")]
        Interface = 0x00000020,
    }

    const uint StringInteropFormatMask = 0x00030000;
    public enum StringInteropFormat : uint
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

    const uint FlagsMask = ~VisibilityMask & ~LayoutMask & ~ClassSemanticsMask & ~StringInteropFormatMask;
    [Flags]
    public enum Flags : uint
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
[Flags]
enum ElementType : byte
{
    [Description("Marks end of a list")]
    End = 0x00,
    [Description("")]
    Void = 0x01,
    [Description("")]
    Boolean = 0x02,
    [Description("")]
    Char = 0x03,
    [Description("")]
    Int1 = 0x04,
    [Description("")]
    UInt1 = 0x05,
    [Description("")]
    Int2 = 0x06,
    [Description("")]
    UInt2 = 0x07,
    [Description("")]
    Int4 = 0x08,
    [Description("")]
    UInt4 = 0x09,
    [Description("")]
    Int8 = 0x0a,
    [Description("")]
    UInt8 = 0x0b,
    [Description("")]
    Real4 = 0x0c,
    [Description("")]
    Real8 = 0x0d,
    [Description("")]
    String = 0x0e,
    [Description("Followed by type")]
    Ptr = 0x0f,
    [Description("Followed by type")]
    Byref = 0x10,
    [Description("Followed by TypeDef or TypeRef token")]
    Valuetype = 0x11,
    [Description("Followed by TypeDef or TypeRef token")]
    Class = 0x12,
    [Description("Generic parameter in a generic type definition, represented as number (compressed unsigned integer)")]
    Var = 0x13,
    [Description("type rank boundsCount bound1 ... loCount lo1 ...")]
    Array = 0x14,
    [Description("Generic type instantiation. Followed by type type-arg-count type-1 ... type-n")]
    Genericinst = 0x15,
    [Description("")]
    Typedbyref = 0x16,
    [Description("System.IntPtr")]
    IntPtr = 0x18,
    [Description("System.UIntPtr")]
    UIntPtr = 0x19,
    [Description("Followed by full method signature")]
    Fnptr = 0x1b,
    [Description("System.Object")]
    Object = 0x1c,
    [Description("Single-dim array with 0 lower bound")]
    Szarray = 0x1d,
    [Description("Generic parameter in a generic method definition, represented as number (compressed unsigned integer)")]
    Mvar = 0x1e,
    [Description("Required modifier : followed by a TypeDef or TypeRef token")]
    Cmod_reqd = 0x1f,
    [Description("Optional modifier : followed by a TypeDef or TypeRef token")]
    Cmod_opt = 0x20,
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

// II.24.2.1
sealed class MetadataRoot : ICanRead
{
    //TODO(descriptions)

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
    public string Version;
    [Description("Reserved, always 0 (§II.24.1).")]
    [Expected(0)]
    public ushort Flags;
    [Description("Number of streams.")]
    public ushort Streams;
    public StreamHeader[] StreamHeaders;

    public CodeNode Read(Stream stream) {
        return new CodeNode
        {
            stream.ReadStruct(out Signature, nameof(Signature)),
            stream.ReadStruct(out MajorVersion, nameof(MajorVersion)),
            stream.ReadStruct(out MinorVersion, nameof(MinorVersion)),
            stream.ReadStruct(out Reserved, nameof(Reserved)),
            stream.ReadStruct(out Length, nameof(Length)),
            stream.ReadAnything(out Version, StreamExtensions.ReadNullTerminated(Encoding.UTF8, 4), "Version"),
            stream.ReadStruct(out Flags, nameof(Flags)),
            stream.ReadStruct(out Streams, nameof(Streams)),
            stream.ReadClasses(ref StreamHeaders, Streams),
        };
    }
}

// II.24.2.2
sealed class StreamHeader : ICanRead
{
    //TODO(descriptions)

    [Description("Memory offset to start of this stream from start of the metadata root(§II.24.2.1)")]
    public uint Offset;
    [Description("Size of this stream in bytes, shall be a multiple of 4.")]
    public uint Size;
    [Description("Name of the stream as null-terminated variable length array of ASCII characters, padded to the next 4 - byte boundary with null characters.")]
    public string Name;

    public CodeNode Read(Stream stream) {
        return new CodeNode
        {
            stream.ReadStruct(out Offset, nameof(Offset)),
            stream.ReadStruct(out Size, nameof(Size)),
            stream.ReadAnything(out Name, StreamExtensions.ReadNullTerminated(Encoding.ASCII, 4), "Name"),
        };
    }
}

abstract class Heap<T> : ICanRead, IHaveAName
{
    readonly byte[] data;
    CodeNode parent;
    int offset;
    SortedList<int, Tuple<T, CodeNode>> children = new SortedList<int, Tuple<T, CodeNode>>();

    public Heap(int size) {
        data = new byte[size];
    }

    public CodeNode Read(Stream stream) {
        offset = (int)stream.Position;

        // Parsing the whole array now isn't sensible
        stream.ReadWholeArray(data);

        return parent = new CodeNode();
    }

    public string Name => GetType().Name;
    protected abstract CodeNode ReadChild(Stream stream, int index, out T t);

    protected Tuple<T, CodeNode> AddChild(IHaveIndex i) {
        var stream = new MemoryStream(data);
        var index = i.Index;
        stream.Position = index;

        Tuple<T, CodeNode> childpair;
        if (!children.TryGetValue(index, out childpair)) {
            T t;
            var child = ReadChild(stream, i.Index, out t);
            if (child == null) {
                return Tuple.Create(t, parent);
            }

            childpair = Tuple.Create(t, child);
            children.Add(index, childpair);

            child.Start += offset;
            child.End += offset;

            parent.Add(child);

            AdjustChildRanges(index, child);
        }

        return childpair;
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

    protected static void GetEncodedLength(Stream stream, out int length, out int offset) {
        var first = stream.ReallyReadByte();
        if ((first & 0x80) == 0) {
            length = first & 0x7F;
            offset = 1;
        } else if ((first & 0xC0) == 0x80) {
            var second = stream.ReallyReadByte();
            length = ((first & 0x3F) << 8) + second;
            offset = 2;
        } else if ((first & 0xE0) == 0xC0) {
            var second = stream.ReallyReadByte();
            var third = stream.ReallyReadByte();
            var fourth = stream.ReallyReadByte();
            length = ((first & 0x1F) << 24) + (second << 16) + (third << 8) + fourth;
            offset = 4;
        } else {
            throw new InvalidOperationException($"Heap byte {stream.Position} can't start with 1111...");
        }
    }
}

// II.24.2.3
sealed class StringHeap : Heap<string>
{
    public StringHeap(int size)
        : base(size) {
    }

    protected override CodeNode ReadChild(Stream stream, int index, out string s) {
        return stream.ReadAnything(out s, StreamExtensions.ReadNullTerminated(Encoding.UTF8, 1), $"StringHeap[{index}]");
    }

    public static string Get(StringHeapIndex i) {
        return Singletons.Instance.StringHeap.AddChild(i).Item1;
    }
    public static CodeNode GetNode(StringHeapIndex i) {
        return Singletons.Instance.StringHeap.AddChild(i).Item2;
    }
}

// II.24.2.4
sealed class UserStringHeap : Heap<string>
{
    public UserStringHeap(int size)
        : base(size) {
    }

    protected override CodeNode ReadChild(Stream stream, int index, out string s) {
        int length;
        int offset;
        GetEncodedLength(stream, out length, out offset);

        var error = "oops";
        var success = true;
        var node = stream.ReadAnything(out s, str => {
            var bytes = new byte[length - 1]; // skip terminal byte
            success = str.TryReadWholeArray(bytes, out error);
            return Encoding.Unicode.GetString(bytes);
        }, $"UserStringHeap[{index}]");

        if (!success)
            node.AddError(error);
        node.Description = $@"""{s}"", {offset} leading bits";
        node.Start -= offset;
        return node;
    }

    public static string Get(UserStringHeapIndex i) {
        return Singletons.Instance.UserStringHeap.AddChild(i).Item1;
    }
    public static CodeNode GetNode(UserStringHeapIndex i) {
        return Singletons.Instance.UserStringHeap.AddChild(i).Item2;
    }
}

sealed class BlobHeap : Heap<byte[]>
{
    public BlobHeap(int size)
        : base(size) {
    }

    protected override CodeNode ReadChild(Stream stream, int index, out byte[] b) {
        int length;
        int offset;
        GetEncodedLength(stream, out length, out offset);

        var node = stream.ReadAnything(out b, StreamExtensions.ReadByteArray(length), $"BlobHeap[{index}]");
        node.Description = $"{offset} leading bits";
        node.Start -= offset;
        return node;
    }

    public static byte[] Get(BlobHeapIndex i) {
        return Singletons.Instance.BlobHeap.AddChild(i).Item1;
    }
    public static CodeNode GetNode(BlobHeapIndex i) {
        return Singletons.Instance.BlobHeap.AddChild(i).Item2;
    }
}

// II.24.2.5
sealed class GuidHeap : Heap<Guid>
{
    public GuidHeap(int size)
        : base(size) {
    }

    protected override CodeNode ReadChild(Stream stream, int index, out Guid g) {
        if (index == 0) {
            g = Guid.Empty;
            return null;
        }

        const int size = 16;

        stream.Position = (index - 1) * size; // GuidHeap is indexed from 1

        return stream.ReadAnything(out g, s => new Guid(StreamExtensions.ReadByteArray(16)(s)), $"GuidHeap[{index}]");
    }

    public static Guid Get(GuidHeapIndex i) {
        return Singletons.Instance.GuidHeap.AddChild(i).Item1;
    }
    public static CodeNode GetNode(GuidHeapIndex i) {
        return Singletons.Instance.GuidHeap.AddChild(i).Item2;
    }
}

// II.24.2.6
sealed class TildeStream : ICanRead
{
    public Section Section { get; private set; }
    public TildeStream(Section section) {
        Section = section;
    }

    public TildeData TildeData;
    public uint[] Rows;

    public Module[] Modules;
    public TypeRef[] TypeRefs;
    public TypeDef[] TypeDefs;
    public Field[] Fields;
    public MethodDef[] MethodDefs;
    public Param[] Params;
    public InterfaceImpl[] InterfaceImpls;
    public MemberRef[] MemberRefs;
    public Constant[] Constants;
    public CustomAttribute[] CustomAttributes;
    public FieldMarshal[] FieldMarshals;
    public DeclSecurity[] DeclSecuritys;
    public ClassLayout[] ClassLayouts;
    public FieldLayout[] FieldLayouts;
    public StandAloneSig[] StandAloneSigs;
    public EventMap[] EventMaps;
    public Event[] Events;
    public PropertyMap[] PropertyMaps;
    public Property[] Properties;
    public MethodSemantics[] MethodSemantics;
    public MethodImpl[] MethodImpls;
    public ModuleRef[] ModuleRefs;
    public TypeSpec[] TypeSpecs;
    public ImplMap[] ImplMaps;
    public FieldRVA[] FieldRVAs;
    public Assembly[] Assemblies;
    public AssemblyProcessor[] AssemblyProcessors;
    public AssemblyOS[] AssemblyOSs;
    public AssemblyRef[] AssemblyRefs;
    public AssemblyRefProcessor[] AssemblyRefProcessors;
    public AssemblyRefOS[] AssemblyRefOSs;
    public FileTable[] Files;
    public ExportedType[] ExportedTypes;
    public ManifestResource[] ManifestResources;
    public NestedClass[] NestedClasses;
    public GenericParam[] GenericParams;
    public MethodSpec[] MethodSpecs;
    public GenericParamConstraint[] GenericParamConstraints;

    Dictionary<MetadataTableFlags, IEnumerable<CodeNode>> streamNodes = new Dictionary<MetadataTableFlags, IEnumerable<CodeNode>>();

    public CodeNode Read(Stream stream) {
        var node = new CodeNode
        {
            stream.ReadStruct(out TildeData),
            new CodeNode("Rows") {
                stream.ReadStructs(out Rows, ((ulong)TildeData.Valid).CountSetBits(), "Rows"),
            },
            Enum.GetValues(typeof(MetadataTableFlags))
                .Cast<MetadataTableFlags>()
                .Where(flag => TildeData.Valid.HasFlag(flag))
                .SelectMany((flag, row) => CapturingReadTable(stream, flag, row))
        };

        if (TildeData.HeapSizes != 0)
            throw new NotImplementedException("HeapSizes aren't 4-byte-aware");
        if (Rows.Max() >= (1 << 11))
            throw new NotImplementedException("CodeIndex aren't 4-byte-aware");

        return node;
    }

    IEnumerable<CodeNode> CapturingReadTable(Stream stream, MetadataTableFlags flag, int row) {
        var nodes = ReadTable(stream, flag, row);
        streamNodes.Add(flag, nodes);
        return nodes;
    }

    IEnumerable<CodeNode> ReadTable(Stream stream, MetadataTableFlags flag, int row) {
        var count = (int)Rows[row];

        switch (flag) {
            case MetadataTableFlags.Module:
                return stream.ReadClasses(ref Modules, count);
            case MetadataTableFlags.TypeRef:
                return stream.ReadClasses(ref TypeRefs, count);
            case MetadataTableFlags.TypeDef:
                return stream.ReadClasses(ref TypeDefs, count);
            case MetadataTableFlags.Field:
                return stream.ReadClasses(ref Fields, count);
            case MetadataTableFlags.MethodDef:
                return stream.ReadClasses(ref MethodDefs, count);
            case MetadataTableFlags.Param:
                return stream.ReadClasses(ref Params, count);
            case MetadataTableFlags.InterfaceImpl:
                return stream.ReadClasses(ref InterfaceImpls, count);
            case MetadataTableFlags.MemberRef:
                return stream.ReadClasses(ref MemberRefs, count);
            case MetadataTableFlags.Constant:
                return stream.ReadClasses(ref Constants, count);
            case MetadataTableFlags.CustomAttribute:
                return stream.ReadClasses(ref CustomAttributes, count);
            case MetadataTableFlags.FieldMarshal:
                return stream.ReadClasses(ref FieldMarshals, count);
            case MetadataTableFlags.DeclSecurity:
                return stream.ReadClasses(ref DeclSecuritys, count);
            case MetadataTableFlags.ClassLayout:
                return stream.ReadClasses(ref ClassLayouts, count);
            case MetadataTableFlags.FieldLayout:
                return stream.ReadClasses(ref FieldLayouts, count);
            case MetadataTableFlags.StandAloneSig:
                return stream.ReadClasses(ref StandAloneSigs, count);
            case MetadataTableFlags.EventMap:
                return stream.ReadClasses(ref EventMaps, count);
            case MetadataTableFlags.Event:
                return stream.ReadClasses(ref Events, count);
            case MetadataTableFlags.PropertyMap:
                return stream.ReadClasses(ref PropertyMaps, count);
            case MetadataTableFlags.Property:
                return stream.ReadClasses(ref Properties, count, nameof(Properties));
            case MetadataTableFlags.MethodSemantics:
                return stream.ReadClasses(ref MethodSemantics, count, nameof(MethodSemantics));
            case MetadataTableFlags.MethodImpl:
                return stream.ReadClasses(ref MethodImpls, count);
            case MetadataTableFlags.ModuleRef:
                return stream.ReadClasses(ref ModuleRefs, count);
            case MetadataTableFlags.TypeSpec:
                return stream.ReadClasses(ref TypeSpecs, count);
            case MetadataTableFlags.ImplMap:
                return stream.ReadClasses(ref ImplMaps, count);
            case MetadataTableFlags.FieldRVA:
                return stream.ReadClasses(ref FieldRVAs, count);
            case MetadataTableFlags.Assembly:
                return stream.ReadClasses(ref Assemblies, count, nameof(Assemblies));
            case MetadataTableFlags.AssemblyProcessor:
                return stream.ReadClasses(ref AssemblyProcessors, count);
            case MetadataTableFlags.AssemblyOS:
                return stream.ReadClasses(ref AssemblyOSs, count);
            case MetadataTableFlags.AssemblyRef:
                return stream.ReadClasses(ref AssemblyRefs, count);
            case MetadataTableFlags.AssemblyRefProcessor:
                return stream.ReadClasses(ref AssemblyRefProcessors, count);
            case MetadataTableFlags.AssemblyRefOS:
                return stream.ReadClasses(ref AssemblyRefOSs, count);
            case MetadataTableFlags.File:
                return stream.ReadClasses(ref Files, count, nameof(Files));
            case MetadataTableFlags.ExportedType:
                return stream.ReadClasses(ref ExportedTypes, count);
            case MetadataTableFlags.ManifestResource:
                return stream.ReadClasses(ref ManifestResources, count);
            case MetadataTableFlags.NestedClass:
                return stream.ReadClasses(ref NestedClasses, count, nameof(NestedClasses));
            case MetadataTableFlags.GenericParam:
                return stream.ReadClasses(ref GenericParams, count);
            case MetadataTableFlags.MethodSpec:
                return stream.ReadClasses(ref MethodSpecs, count);
            case MetadataTableFlags.GenericParamConstraint:
                return stream.ReadClasses(ref GenericParamConstraints, count);
            default:
                throw new InvalidOperationException("Not a real MetadataTableFlags " + flag);
        }
    }

    public CodeNode GetCodeNode(MetadataTableFlags flag, int i) {
        return streamNodes[flag].Skip(i).First();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct TildeData
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
    public TildeDateHeapSizes HeapSizes;
    [Description("Reserved, always 1 (§II.24.1).")]
    [Expected(1)]
    public byte Reserved2;
    [Description("Bit vector of present tables, let n be the number of bits that are 1.")]
    public MetadataTableFlags Valid;
    [Description("Bit vector of sorted tables.")]
    public MetadataTableFlags Sorted;
}

[Flags]
enum TildeDateHeapSizes : byte
{
    StringHeapIndexWide = 0x01,
    GuidHeapIndexWide = 0x02,
    BlobHeapIndexWide = 0x04,
}

sealed class StringHeapIndex : ICanRead, IHaveLiteralValue, IHaveIndex
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index => (int)(intIndex ?? shortIndex);

    public string StringValue => StringHeap.Get(this);
    public object Value => StringValue;

    public CodeNode Read(Stream stream) {
        ushort index;
        var node = stream.ReadStruct(out index, nameof(index));
        shortIndex = index;

        node.Link = StringHeap.GetNode(this);
        node.Description = $"String Heap index {index:X}";

        return node;
    }
}

sealed class UserStringHeapIndex : ICanRead, IHaveValue, IHaveIndex
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index => (int)(intIndex ?? shortIndex);

    public object Value => UserStringHeap.Get(this);

    public CodeNode Read(Stream stream) {
        ushort index;
        var node = stream.ReadStruct(out index, nameof(index));
        shortIndex = index;

        node.Link = UserStringHeap.GetNode(this);
        node.Description = $"User String Heap index {index:X}";

        return node;
    }
}

sealed class BlobHeapIndex : ICanRead, IHaveValue, IHaveIndex
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index => (int)(intIndex ?? shortIndex);

    public object Value => BlobHeap.Get(this);

    public CodeNode Read(Stream stream) {
        ushort index;
        var node = stream.ReadStruct(out index, nameof(index));
        shortIndex = index;

        node.Link = BlobHeap.GetNode(this);
        node.Description = $"Blob Heap index {index:X}";

        return node;
    }
}

sealed class GuidHeapIndex : ICanRead, IHaveValue, IHaveIndex
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index => (int)(intIndex ?? shortIndex);
    public object Value => GuidHeap.Get(this);

    public CodeNode Read(Stream stream) {
        ushort index;
        var node = stream.ReadStruct(out index, nameof(index));
        shortIndex = index;

        node.Link = GuidHeap.GetNode(this);
        node.Description = $"Guid Heap index {index:X}";

        return node;
    }
}

//TODO(links) implement all CodedIndex
sealed class UnknownCodedIndex : ICanRead
{
    public CodeNode Read(Stream stream) {
        ushort index;
        return stream.ReadStruct(out index, nameof(index));
    }
}

abstract class CodedIndex : ICanRead
{
    private CodedIndex() { } // Don't allow subclassing 

    public int Index { get; private set; }

    public CodeNode Read(Stream stream) {
        ushort readData;
        var node = stream.ReadStruct(out readData, "index");

        Index = GetIndex(readData);

        node.DelayedValueNode = GetLink;

        return node;
    }

    protected abstract int GetIndex(int readData);

    protected abstract IHaveLiteralValueNode GetLink();

    public class TypeDefOrRef : CodedIndex
    {
        IHaveLiteralValueNode extendsNothing;

        Tag tag;

        protected override int GetIndex(int readData) {
            if (readData == 0) {
                extendsNothing = new ExtendsNothing();
                return -1;
            }

            tag = (Tag)(readData & 0x3);
            return (readData >> 2) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            if (extendsNothing != null) {
                return extendsNothing;
            }

            switch (tag) {
                case Tag.TypeDef: return Singletons.Instance.TildeStream.TypeDefs[Index];
                case Tag.TypeRef: return Singletons.Instance.TildeStream.TypeRefs[Index];
                case Tag.TypeSpec: return Singletons.Instance.TildeStream.TypeSpecs[Index];
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

    public class HasConstant : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x3);
            return (readData >> 2) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.Field: return Singletons.Instance.TildeStream.Fields[Index];
                case Tag.Param: return Singletons.Instance.TildeStream.Params[Index];
                case Tag.Property: return Singletons.Instance.TildeStream.Properties[Index];
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

    public class HasCustomAttribute : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x1F);
            return (readData >> 5) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.MethodDef: return Singletons.Instance.TildeStream.MethodDefs[Index];
                case Tag.Field: return Singletons.Instance.TildeStream.Fields[Index];
                case Tag.TypeRef: return Singletons.Instance.TildeStream.TypeRefs[Index];
                case Tag.TypeDef: return Singletons.Instance.TildeStream.TypeDefs[Index];
                case Tag.Param: return Singletons.Instance.TildeStream.Params[Index];
                case Tag.InterfaceImpl: return Singletons.Instance.TildeStream.InterfaceImpls[Index];
                case Tag.MemberRef: return Singletons.Instance.TildeStream.MemberRefs[Index];
                case Tag.Module: return Singletons.Instance.TildeStream.Modules[Index];
                //case Tag.Permission: return Singletons.Instance.TildeStream.Permissions[Index]; //TODO(pedant) DeclSecuritys?
                case Tag.Property: return Singletons.Instance.TildeStream.Properties[Index];
                case Tag.Event: return Singletons.Instance.TildeStream.Events[Index];
                case Tag.StandAloneSig: return Singletons.Instance.TildeStream.StandAloneSigs[Index];
                case Tag.ModuleRef: return Singletons.Instance.TildeStream.ModuleRefs[Index];
                case Tag.TypeSpec: return Singletons.Instance.TildeStream.TypeSpecs[Index];
                case Tag.Assembly: return Singletons.Instance.TildeStream.Assemblies[Index];
                case Tag.AssemblyRef: return Singletons.Instance.TildeStream.AssemblyRefs[Index];
                case Tag.File: return Singletons.Instance.TildeStream.Files[Index];
                case Tag.ExportedType: return Singletons.Instance.TildeStream.ExportedTypes[Index];
                case Tag.ManifestResource: return Singletons.Instance.TildeStream.ManifestResources[Index];
                case Tag.GenericParam: return Singletons.Instance.TildeStream.GenericParams[Index];
                case Tag.GenericParamConstraint: return Singletons.Instance.TildeStream.GenericParamConstraints[Index];
                case Tag.MethodSpec: return Singletons.Instance.TildeStream.MethodSpecs[Index];
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

    public class HasFieldMarshall : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x1);
            return (readData >> 1) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.Field: return Singletons.Instance.TildeStream.Fields[Index];
                case Tag.Param: return Singletons.Instance.TildeStream.Params[Index];
            }
            throw new InvalidOperationException(tag.ToString());
        }

        enum Tag
        {
            Field = 0,
            Param = 1,
        }
    }

    public class HasDeclSecurity : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x3);
            return (readData >> 2) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.TypeDef: return Singletons.Instance.TildeStream.TypeDefs[Index];
                case Tag.MethodDef: return Singletons.Instance.TildeStream.MethodDefs[Index];
                case Tag.Assembly: return Singletons.Instance.TildeStream.Assemblies[Index];
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

    public class MemberRefParent : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x7);
            return (readData >> 3) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.TypeDef: return Singletons.Instance.TildeStream.TypeDefs[Index];
                case Tag.TypeRef: return Singletons.Instance.TildeStream.TypeRefs[Index];
                case Tag.ModuleRef: return Singletons.Instance.TildeStream.ModuleRefs[Index];
                case Tag.MethodDef: return Singletons.Instance.TildeStream.MethodDefs[Index];
                case Tag.TypeSpec: return Singletons.Instance.TildeStream.TypeSpecs[Index];
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

    public class HasSemantics : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x1);
            return (readData >> 1) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.Event: return Singletons.Instance.TildeStream.Events[Index];
                case Tag.Property: return Singletons.Instance.TildeStream.Properties[Index];
            }
            throw new InvalidOperationException(tag.ToString());
        }

        enum Tag
        {
            Event = 0,
            Property = 1,
        }
    }

    public class MethodDefOrRef : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x1);
            return (readData >> 1) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.MethodDef: return Singletons.Instance.TildeStream.MethodDefs[Index];
                case Tag.MemberRef: return Singletons.Instance.TildeStream.MemberRefs[Index];
            }
            throw new InvalidOperationException(tag.ToString());
        }

        enum Tag
        {
            MethodDef = 0,
            MemberRef = 1,
        }
    }

    public class MemberForwarded : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x1);
            return (readData >> 1) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.Field: return Singletons.Instance.TildeStream.Fields[Index];
                case Tag.MethodDef: return Singletons.Instance.TildeStream.MethodDefs[Index];
            }
            throw new InvalidOperationException(tag.ToString());
        }

        enum Tag
        {
            Field = 0,
            MethodDef = 1,
        }
    }

    public class Implementation : CodedIndex
    {
        IHaveLiteralValueNode extendsNothing;

        Tag tag;

        protected override int GetIndex(int readData) {
            if (readData == 0) {
                extendsNothing = new ExtendsNothing();
                return -1;
            }

            tag = (Tag)(readData & 0x3);
            return (readData >> 2) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            if (extendsNothing != null) {
                return extendsNothing;
            }

            switch (tag) {
                case Tag.File: return Singletons.Instance.TildeStream.Files[Index];
                case Tag.AssemblyRef: return Singletons.Instance.TildeStream.AssemblyRefs[Index];
                case Tag.ExportedType: return Singletons.Instance.TildeStream.ExportedTypes[Index];
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

    public class CustomAttributeType : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x7);
            return (readData >> 3) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.MethodDef: return Singletons.Instance.TildeStream.MethodDefs[Index];
                case Tag.MemberRef: return Singletons.Instance.TildeStream.MemberRefs[Index];
            }
            throw new InvalidOperationException(tag.ToString());
        }

        enum Tag
        {
            MethodDef = 2,
            MemberRef = 3,
        }
    }

    public class ResolutionScope : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x3);
            return (readData >> 2) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.Module: return Singletons.Instance.TildeStream.Modules[Index];
                case Tag.ModuleRef: return Singletons.Instance.TildeStream.ModuleRefs[Index];
                case Tag.AssemblyRef: return Singletons.Instance.TildeStream.AssemblyRefs[Index];
                case Tag.TypeRef: return Singletons.Instance.TildeStream.TypeRefs[Index];
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

    public class TypeOrMethodDef : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData) {
            tag = (Tag)(readData & 0x1);
            return (readData >> 1) - 1;
        }

        protected override IHaveLiteralValueNode GetLink() {
            switch (tag) {
                case Tag.TypeDef: return Singletons.Instance.TildeStream.TypeDefs[Index];
                case Tag.MethodDef: return Singletons.Instance.TildeStream.MethodDefs[Index];
            }
            throw new InvalidOperationException(tag.ToString());
        }

        enum Tag
        {
            TypeDef = 0,
            MethodDef = 1,
        }
    }

    class ExtendsNothing : IHaveLiteralValueNode
    {
        public object Value => "(Nothing)";
        public CodeNode Node => null;
    }
}
