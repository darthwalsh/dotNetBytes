using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable 0649 // CS0649: Field '...' is never assigned to, and will always have its default value

sealed class ExpectedAttribute : Attribute
{
    public object Value;
    public ExpectedAttribute(object v)
    {
        Value = v;
    }
}

sealed class DescriptionAttribute : Attribute
{
    public string Description;
    public DescriptionAttribute(string d)
    {
        Description = d;
    }
}

// II.22
[Flags]
public enum MetadataTableFlags : ulong
{
    Module = 1L << 0x00,
    TypeRef = 1L << 0x01,
    TypeDef = 1L << 0x02,
    Field = 1L << 0x04,
    MethodDef = 1L << 0x06,
    Param = 1L << 0x08,
    InterfaceImpl = 1L << 0x09,
    MemberRef = 1L << 0x0A,
    Constant = 1L << 0x0B,
    CustomAttribute = 1L << 0x0C,
    FieldMarshal = 1L << 0x0D,
    DeclSecurity = 1L << 0x0E,
    ClassLayout = 1L << 0x0F,
    FieldLayout = 1L << 0x10,
    StandAloneSig = 1L << 0x11,
    EventMap = 1L << 0x12,
    Event = 1L << 0x14,
    PropertyMap = 1L << 0x15,
    Property = 1L << 0x17,
    MethodSemantics = 1L << 0x18,
    MethodImpl = 1L << 0x19,
    ModuleRef = 1L << 0x1A,
    TypeSpec = 1L << 0x1B,
    ImplMap = 1L << 0x1C,
    FieldRVA = 1L << 0x1D,
    Assembly = 1L << 0x20,
    AssemblyProcessor = 1L << 0x21,
    AssemblyOS = 1L << 0x22,
    AssemblyRef = 1L << 0x23,
    AssemblyRefProcessor = 1L << 0x24,
    AssemblyRefOS = 1L << 0x25,
    File = 1L << 0x26,
    ExportedType = 1L << 0x27,
    ManifestResource = 1L << 0x28,
    NestedClass = 1L << 0x29,
    GenericParam = 1L << 0x2A,
    MethodSpec = 1L << 0x2B,
    GenericParamConstraint = 1L << 0x2C,
}

// II.22.2
sealed class Assembly : ICanRead, IHaveValueNode
{
    public AssemblyHashAlgorithmBlittableWrapper HashAlgId;
    public ushort MajorVersion;
    public ushort MinorVersion;
    public ushort BuildNumber;
    public ushort RevisionNumber;
    public AssemblyFlagsHolderBlittableWrapper Flags;
    public BlobHeapIndex PublicKey;
    public StringHeapIndex Name;
    public StringHeapIndex Culture;

    public object Value => Name.StringValue + " " + new Version(MajorVersion, MinorVersion, BuildNumber, RevisionNumber).ToString();

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadStruct(out HashAlgId, nameof(HashAlgId)).Children.Single(),
            stream.ReadStruct(out MajorVersion, nameof(MajorVersion)),
            stream.ReadStruct(out MinorVersion, nameof(MinorVersion)),
            stream.ReadStruct(out BuildNumber, nameof(BuildNumber)),
            stream.ReadStruct(out RevisionNumber, nameof(RevisionNumber)),
            stream.ReadStruct(out Flags, nameof(Flags)).Children.Single(),
            stream.ReadClass(ref PublicKey, nameof(PublicKey)),
            stream.ReadClass(ref Name, nameof(Name)),
            stream.ReadClass(ref Culture, nameof(Culture)),
        };
    }
}

// II.22.5
sealed class AssemblyRef : ICanRead, IHaveValueNode
{
    public ushort MajorVersion;
    public ushort MinorVersion;
    public ushort BuildNumber;
    public ushort RevisionNumber;
    public AssemblyFlagsHolderBlittableWrapper Flags;
    public BlobHeapIndex PublicKeyOrToken;
    public StringHeapIndex Name;
    public StringHeapIndex Culture;
    public BlobHeapIndex HashValue;

    public object Value => Name.StringValue + " " + new Version(MajorVersion, MinorVersion, BuildNumber, RevisionNumber).ToString();

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadStruct(out MajorVersion, nameof(MajorVersion)),
            stream.ReadStruct(out MinorVersion, nameof(MinorVersion)),
            stream.ReadStruct(out BuildNumber, nameof(BuildNumber)),
            stream.ReadStruct(out RevisionNumber, nameof(RevisionNumber)),
            stream.ReadStruct(out Flags, nameof(Flags)).Children.Single(),
            stream.ReadClass(ref PublicKeyOrToken, nameof(PublicKeyOrToken)),
            stream.ReadClass(ref Name, nameof(Name)),
            stream.ReadClass(ref Culture, nameof(Culture)),
            stream.ReadClass(ref HashValue, nameof(HashValue)),
        };
    }
}

// II.22.9
sealed class Constant : ICanRead, IHaveValueNode
{
    public UnknownCodedIndex Type;
    public UnknownCodedIndex Parent;
    public BlobHeapIndex _Value;

    public object Value => "";

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadClass(ref Type, nameof(Type)),
            stream.ReadClass(ref Parent, nameof(Parent)),
            stream.ReadClass(ref _Value, "Value"),
        };
    }
}

// II.22.10
sealed class CustomAttribute : ICanRead, IHaveValueNode
{
    public CodedIndex.HasCustomAttribute Parent;
    public CodedIndex.CustomAttributeType Type;
    public BlobHeapIndex _Value;

    public object Value => "";

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadClass(ref Parent, nameof(Parent)),
            stream.ReadClass(ref Type, nameof(Type)),
            stream.ReadClass(ref _Value, "Value"),
        };
    }
}

// II.22.16
sealed class Field : ICanRead, IHaveValueNode
{
    public ushort Flags; // TODO (flags)
    public StringHeapIndex Name;
    public BlobHeapIndex Signature;

    public object Value => Name.Value;

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadStruct(out Flags, nameof(Flags)),
            stream.ReadClass(ref Name, nameof(Name)),
            stream.ReadClass(ref Signature, nameof(Signature)),
        };
    }
}

// II.22.25
sealed class MemberRef : ICanRead, IHaveValueNode
{
    public CodedIndex.MemberRefParent Class;
    public StringHeapIndex Name;
    public BlobHeapIndex Signature;

    public object Value => Name.Value;

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadClass(ref Class, nameof(Class)),
            stream.ReadClass(ref Name, nameof(Name)),
            stream.ReadClass(ref Signature, nameof(Signature)),
        };
    }
}

// II.22.26
sealed class MethodDef : ICanRead, IHaveValueNode
{
    public uint RVA;
    public ushort ImplFlags;
    public ushort Flags;  //TODO(flags)
    public StringHeapIndex Name;
    public BlobHeapIndex Signature; //TODO(Signature) parse these, ditto below
    public UnknownCodedIndex ParamList;

    public object Value => Name.Value;

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        CodeNode rva;

        Node = new CodeNode
        {
            (rva = stream.ReadStruct(out RVA, nameof(RVA))),
            stream.ReadStruct(out ImplFlags, nameof(ImplFlags)),
            stream.ReadStruct(out Flags, nameof(Flags)),
            stream.ReadClass(ref Name, nameof(Name)),
            stream.ReadClass(ref Signature, nameof(Signature)),
            stream.ReadClass(ref ParamList, nameof(ParamList)),
        };

        rva.DelayedValueNode = () => new DefaultValueNode(rva.Value, Method.MethodsByRVA[RVA].Node);

        return Node;
    }
}

// II.22.28
sealed class MethodSemantics : ICanRead, IHaveValueNode
{
    public ushort Semantics; // TODO flags
    public UnknownCodedIndex Method;
    public UnknownCodedIndex Association;

    public object Value => "";

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadStruct(out Semantics, nameof(Semantics)),
            stream.ReadClass(ref Method, nameof(Method)),
            stream.ReadClass(ref Association, nameof(Association)),
        };
    }
}

// II.22.30
sealed class Module : ICanRead, IHaveValueNode
{
    public ushort Generation;
    public StringHeapIndex Name;
    public GuidHeapIndex Mvid;
    public GuidHeapIndex EncId;
    public GuidHeapIndex EncBaseId;

    public object Value => Name.Value;

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadStruct(out Generation, nameof(Generation)),
            stream.ReadClass(ref Name, nameof(Name)),
            stream.ReadClass(ref Mvid, nameof(Mvid)),
            stream.ReadClass(ref EncId, nameof(EncId)),
            stream.ReadClass(ref EncBaseId, nameof(EncBaseId)),
        };
    }
}

// II.22.33
sealed class Param : ICanRead, IHaveValueNode
{
    public ushort Flags; // TODO (flags)
    public ushort Sequence;
    public StringHeapIndex Name;

    public object Value => Name.Value;

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadStruct(out Flags, nameof(Flags)),
            stream.ReadStruct(out Sequence, nameof(Sequence)),
            stream.ReadClass(ref Name, nameof(Name)),
        };
    }
}

// II.22.34
sealed class Property : ICanRead, IHaveValueNode
{
    public ushort Flags; // TODO (flags)
    public StringHeapIndex Name;
    public BlobHeapIndex Signature;

    public object Value => Name.Value;

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadStruct(out Flags, nameof(Flags)),
            stream.ReadClass(ref Name, nameof(Name)),
            stream.ReadClass(ref Signature, nameof(Signature)),
        };
    }
}

// II.22.35
sealed class PropertyMap : ICanRead, IHaveValueNode
{
    public UnknownCodedIndex Parent;
    public UnknownCodedIndex PropertyList;

    public object Value => "";

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadClass(ref Parent, nameof(Parent)),
            stream.ReadClass(ref PropertyList, nameof(PropertyList)),
        };
    }
}

// II.22.37
sealed class TypeDef : ICanRead, IHaveValueNode
{
    public TypeAttributes Flags;
    public StringHeapIndex TypeName;
    public StringHeapIndex TypeNamespace;
    public CodedIndex.TypeDefOrRef Extends;
    public UnknownCodedIndex FieldList;
    public UnknownCodedIndex MethodList;

    public object Value => TypeNamespace.StringValue + "." + TypeName.StringValue;

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadClass(ref Flags, nameof(Flags)),
            stream.ReadClass(ref TypeName, nameof(TypeName)),
            stream.ReadClass(ref TypeNamespace, nameof(TypeNamespace)),
            stream.ReadClass(ref Extends, nameof(Extends)),
            stream.ReadClass(ref FieldList, nameof(FieldList)),
            stream.ReadClass(ref MethodList, nameof(MethodList)),
        };
    }
}

// II.22.38
sealed class TypeRef : ICanRead, IHaveValueNode
{
    public CodedIndex.ResolutionScope ResolutionScope;
    public StringHeapIndex TypeName;
    public StringHeapIndex TypeNamespace;

    public object Value => TypeNamespace.StringValue + "." + TypeName.StringValue;

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadClass(ref ResolutionScope, nameof(ResolutionScope)),
            stream.ReadClass(ref TypeName, nameof(TypeName)),
            stream.ReadClass(ref TypeNamespace, nameof(TypeNamespace)),
        };
    }
}

// II.22.39
sealed class TypeSpec : ICanRead, IHaveValueNode
{
    public BlobHeapIndex Signature;

    public object Value => "";

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        return Node = new CodeNode
        {
            stream.ReadClass(ref Signature, nameof(Signature)),
        };
    }
}

//TODO remove from graph, or make a ReadStruct overload that reads inner enum type
// Makes the enum blittable. 
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct AssemblyHashAlgorithmBlittableWrapper
{
    public AssemblyHashAlgorithm AssemblyHashAlgorithm;
}

// II.23.1.1
enum AssemblyHashAlgorithm : uint
{
    None = 0x0000,
    Reserved_MD5 = 0x8003,
    SHA1 = 0x8004,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct AssemblyFlagsHolderBlittableWrapper
{
    public AssemblyFlags AssemblyFlags;
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

// II.23.1.15
class TypeAttributes : ICanRead, IHaveValue
{
    public Visibility visibility;
    public Layout layout;
    public ClassSemantics classSemantics;
    public StringInteropFormat stringInteropFormat;
    public Flags flags;

    public CodeNode Read(Stream stream)
    {
        uint value;
        var node = stream.ReadStruct(out value, "data");

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

    public CodeNode Read(Stream stream)
    {
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

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out Offset, nameof(Offset)),
            stream.ReadStruct(out Size, nameof(Size)),
            stream.ReadAnything(out Name, StreamExtensions.ReadNullTerminated(Encoding.ASCII, 4), "Name"),
        };
    }
}

// II.24.2.3
sealed class StringHeap : ICanRead
{
    byte[] data;

    public StringHeap(int size)
    {
        data = new byte[size];

        instance = this;
    }

    public CodeNode Read(Stream stream)
    {
        // Parsing the whole array now isn't sensible
        stream.ReadWholeArray(data);

        return Node = new CodeNode();
    }

    public static CodeNode Node { get; private set; }

    static StringHeap instance;
    public static string Get(StringHeapIndex i)
    {
        var stream = new MemoryStream(instance.data);
        stream.Position = i.Index;
        return StreamExtensions.ReadNullTerminated(Encoding.UTF8, 1)(stream);
    }
}


// II.24.2.4
sealed class UserStringHeap : ICanRead
{
    byte[] data;

    public UserStringHeap(int size)
    {
        data = new byte[size];

        instance = this;
    }

    public CodeNode Read(Stream stream)
    {
        // Parsing the whole array now isn't sensible
        stream.ReadWholeArray(data);

        return Node = new CodeNode();
    }

    public static CodeNode Node { get; private set; }

    static UserStringHeap instance;
    public static string Get(UserStringHeapIndex i)
    {
        throw new NotImplementedException("UserStringHeap");
    }
}

sealed class BlobHeap : ICanRead
{
    byte[] data;

    public BlobHeap(int size)
    {
        data = new byte[size];

        instance = this;
    }

    public CodeNode Read(Stream stream)
    {
        // Parsing the whole array now isn't sensible
        stream.ReadWholeArray(data);

        return Node = new CodeNode();
    }

    public static CodeNode Node { get; private set; }

    static BlobHeap instance;
    public static byte[] Get(BlobHeapIndex i)
    {
        int length;
        int offset;
        byte firstByte = instance.data[i.Index];
        if ((firstByte & 0x80) == 0)
        {
            length = firstByte & 0x7F;
            offset = 1;
        }
        else if ((firstByte & 0xC0) == 0x80)
        {
            length = ((firstByte & 0x3F) << 8) + instance.data[i.Index + 1];
            offset = 2;
        }
        else if ((firstByte & 0xE0) == 0xC0)
        {
            length = ((firstByte & 0x1F) << 24) + (instance.data[i.Index + 1] << 16) + (instance.data[i.Index + 2] << 8) + instance.data[i.Index + 3];
            offset = 4;
        }
        else
        {
            throw new InvalidOperationException("Blob heap byte " + i.Index + " can't start with 1111...");
        }

        var ans = new byte[length];
        Array.Copy(instance.data, i.Index + offset, ans, 0, length);
        return ans;
    }
}

sealed class GuidHeap : ICanRead
{
    byte[] data;

    public GuidHeap(int size)
    {
        data = new byte[size];

        instance = this;
    }

    public CodeNode Read(Stream stream)
    {
        // Parsing the whole array now isn't sensible
        stream.ReadWholeArray(data);

        return Node = new CodeNode();
    }

    public static CodeNode Node { get; private set; }

    static GuidHeap instance;
    public static Guid Get(GuidHeapIndex i)
    {
        const int size = 16;
        int startAt = (i.Index - 1) * size; // GuidHeap is indexed from 1

        return new Guid(instance.data.Skip(startAt).Take(size).ToArray());
    }
}

// II.24.2.6
sealed class TildeStream : ICanRead
{
    public TildeData TildeData;
    public uint[] Rows;

    public Module[] Modules;
    public TypeRef[] TypeRefs;
    public TypeDef[] TypeDefs;
    public Field[] Fields;
    public MethodDef[] MethodDefs;
    public Param[] Params;
    //public InterfaceImpl[] InterfaceImpls;
    public MemberRef[] MemberRefs;
    public Constant[] Constants;
    public CustomAttribute[] CustomAttributes;
    //public FieldMarshal[] FieldMarshals;
    //public DeclSecurity[] DeclSecuritys;
    //public ClassLayout[] ClassLayouts;
    //public FieldLayout[] FieldLayouts;
    //public StandAloneSig[] StandAloneSigs;
    //public EventMap[] EventMaps;
    //public Event[] Events;
    public PropertyMap[] PropertyMaps;
    public Property[] Properties;
    public MethodSemantics[] MethodSemantics;
    //public MethodImpl[] MethodImpls;
    //public ModuleRef[] ModuleRefs;
    public TypeSpec[] TypeSpecs;
    //public ImplMap[] ImplMaps;
    //public FieldRVA[] FieldRVAs;
    public Assembly[] Assemblies;
    //public AssemblyProcessor[] AssemblyProcessors;
    //public AssemblyOS[] AssemblyOSs;
    public AssemblyRef[] AssemblyRefs;
    //public AssemblyRefProcessor[] AssemblyRefProcessors;
    //public AssemblyRefOS[] AssemblyRefOSs;
    //public File[] Files;
    //public ExportedType[] ExportedTypes;
    //public ManifestResource[] ManifestResources;
    //public NestedClass[] NestedClasss;
    //public GenericParam[] GenericParams;
    //public MethodSpec[] MethodSpecs;
    //public GenericParamConstraint[] GenericParamConstraints;

    public CodeNode Read(Stream stream)
    {
        var node = new CodeNode
        {
            stream.ReadStruct(out TildeData),
            stream.ReadStructs(out Rows, ((ulong)TildeData.Valid).CountSetBits(), "Rows"),
            Enum.GetValues(typeof(MetadataTableFlags))
                .Cast<MetadataTableFlags>()
                .Where(flag => TildeData.Valid.HasFlag(flag))
                .SelectMany((flag, row) => ReadTable(stream, flag, row))
        };

        if (TildeData.HeapSizes != 0)
            throw new NotImplementedException("HeapSizes aren't 4-byte-aware");
        if (Rows.Max() >= (1 << 11))
            throw new NotImplementedException("CodeIndex aren't 4-byte-aware");

        return node;
    }

    IEnumerable<CodeNode> ReadTable(Stream stream, MetadataTableFlags flag, int row)
    {
        int count = (int)Rows[row];

        switch (flag)
        {
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
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref InterfaceImpls, count);
            case MetadataTableFlags.MemberRef:
                return stream.ReadClasses(ref MemberRefs, count);
            case MetadataTableFlags.Constant:
                return stream.ReadClasses(ref Constants, count);
            case MetadataTableFlags.CustomAttribute:
                return stream.ReadClasses(ref CustomAttributes, count);
            case MetadataTableFlags.FieldMarshal:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref FieldMarshals, count);
            case MetadataTableFlags.DeclSecurity:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref DeclSecuritys, count);
            case MetadataTableFlags.ClassLayout:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref ClassLayouts, count);
            case MetadataTableFlags.FieldLayout:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref FieldLayouts, count);
            case MetadataTableFlags.StandAloneSig:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref StandAloneSigs, count);
            case MetadataTableFlags.EventMap:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref EventMaps, count);
            case MetadataTableFlags.Event:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref Events, count);
            case MetadataTableFlags.PropertyMap:
                return stream.ReadClasses(ref PropertyMaps, count);
            case MetadataTableFlags.Property:
                return stream.ReadClasses(ref Properties, count, nameof(Properties));
            case MetadataTableFlags.MethodSemantics:
                return stream.ReadClasses(ref MethodSemantics, count);
            case MetadataTableFlags.MethodImpl:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref MethodImpls, count);
            case MetadataTableFlags.ModuleRef:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref ModuleRefs, count);
            case MetadataTableFlags.TypeSpec:
                return stream.ReadClasses(ref TypeSpecs, count);
            case MetadataTableFlags.ImplMap:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref ImplMaps, count);
            case MetadataTableFlags.FieldRVA:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref FieldRVAs, count);
            case MetadataTableFlags.Assembly:
                return stream.ReadClasses(ref Assemblies, count, nameof(Assemblies));
            case MetadataTableFlags.AssemblyProcessor:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref AssemblyProcessors, count);
            case MetadataTableFlags.AssemblyOS:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref AssemblyOSs, count);
            case MetadataTableFlags.AssemblyRef:
                return stream.ReadClasses(ref AssemblyRefs, count);
            case MetadataTableFlags.AssemblyRefProcessor:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref AssemblyRefProcessors, count);
            case MetadataTableFlags.AssemblyRefOS:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref AssemblyRefOSs, count);
            case MetadataTableFlags.File:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref Files, count);
            case MetadataTableFlags.ExportedType:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref ExportedTypes, count);
            case MetadataTableFlags.ManifestResource:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref ManifestResources, count);
            case MetadataTableFlags.NestedClass:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref NestedClasss, count);
            case MetadataTableFlags.GenericParam:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref GenericParams, count);
            case MetadataTableFlags.MethodSpec:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref MethodSpecs, count);
            case MetadataTableFlags.GenericParamConstraint:
                throw new NotImplementedException(flag.ToString()); //return stream.ReadClasses(ref GenericParamConstraints, count);
            default:
                throw new InvalidOperationException("Not a real MetadataTableFlags " + flag);
        }
    }

    public static TildeStream Instance;
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

sealed class StringHeapIndex : ICanRead, IHaveValue
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index => (int)(intIndex ?? shortIndex);

    public string StringValue => StringHeap.Get(this);
    public object Value => StringValue;

    public CodeNode Read(Stream stream)
    {
        ushort index;
        var node = stream.ReadStruct(out index, nameof(index));
        shortIndex = index;

        node.Link = StringHeap.Node;
        node.Description = $"String Heap index {index:X}";

        return node;
    }
}

sealed class UserStringHeapIndex : ICanRead, IHaveValue
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index => (int)(intIndex ?? shortIndex);

    public object Value => UserStringHeap.Get(this);

    public CodeNode Read(Stream stream)
    {
        ushort index;
        var node = stream.ReadStruct(out index, nameof(index));
        shortIndex = index;

        node.Link = UserStringHeap.Node;
        node.Description = $"User String Heap index {index:X}";

        return node;
    }
}

sealed class BlobHeapIndex : ICanRead, IHaveValue
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index => (int)(intIndex ?? shortIndex);

    public object Value => BlobHeap.Get(this);

    public CodeNode Read(Stream stream)
    {
        // TODO add sub-children as signatures are read (for all Heap*) (maybe use description to glob together entries if read out-of-band???)

        ushort index;
        var node = stream.ReadStruct(out index, nameof(index));
        shortIndex = index;

        node.Link = BlobHeap.Node;
        node.Description = $"Blob Heap index {index:X}";

        return node;
    }
}

sealed class GuidHeapIndex : ICanRead, IHaveValue
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index => (int)(intIndex ?? shortIndex);
    public object Value => GuidHeap.Get(this);

    public CodeNode Read(Stream stream)
    {
        ushort index;
        var node = stream.ReadStruct(out index, nameof(index));
        shortIndex = index;

        node.Link = GuidHeap.Node;
        node.Description = $"Guid Heap index {index:X}";

        return node;
    }
}

//TODO implement all CodedIndex
sealed class UnknownCodedIndex : ICanRead
{
    public CodeNode Read(Stream stream)
    {
        ushort index;
        return stream.ReadStruct(out index, nameof(index));
    }
}

abstract class CodedIndex : ICanRead
{
    private CodedIndex() { } // Don't allow subclassing 

    public int Index { get; private set; }

    public CodeNode Read(Stream stream)
    {
        ushort readData;
        var node = stream.ReadStruct(out readData, "index");

        Index = GetIndex(readData);

        node.DelayedValueNode = GetLink;

        return node;
    }

    protected abstract int GetIndex(int readData);

    protected abstract IHaveValueNode GetLink();

    public class ResolutionScope : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData)
        {
            tag = (Tag)(readData & 0x3);
            return (readData >> 2) - 1;
        }

        protected override IHaveValueNode GetLink()
        {
            switch (tag)
            {
                case Tag.Module: return TildeStream.Instance.Modules[Index];
                //case Tag.ModuleRef: return TildeStream.Instance.ModuleRefs[Index];
                case Tag.AssemblyRef: return TildeStream.Instance.AssemblyRefs[Index];
                case Tag.TypeRef: return TildeStream.Instance.TypeRefs[Index];
            }
            throw new NotImplementedException(tag.ToString());
        }

        enum Tag
        {
            Module = 0,
            ModuleRef = 1,
            AssemblyRef = 2,
            TypeRef = 3,
        }
    }

    public class TypeDefOrRef : CodedIndex
    {
        IHaveValueNode extendsNothing;

        Tag tag;

        protected override int GetIndex(int readData)
        {
            if (readData == 0)
            {
                extendsNothing = new ExtendsNothing();
                return -1;
            }

            tag = (Tag)(readData & 0x3);
            return (readData >> 2) - 1;
        }

        protected override IHaveValueNode GetLink()
        {
            if (extendsNothing != null)
            {
                return extendsNothing;
            }

            switch (tag)
            {
                case Tag.TypeDef: return TildeStream.Instance.TypeDefs[Index];
                case Tag.TypeRef: return TildeStream.Instance.TypeRefs[Index];
                case Tag.TypeSpec: return TildeStream.Instance.TypeSpecs[Index];
            }
            throw new InvalidOperationException(tag.ToString());
        }

        enum Tag
        {
            TypeDef = 0,
            TypeRef = 1,
            TypeSpec = 2,
        }

        class ExtendsNothing : IHaveValueNode
        {
            public object Value => "(Nothing)";
            public CodeNode Node => null;
        }
    }

    public class HasCustomAttribute : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData)
        {
            tag = (Tag)(readData & 0x1F);
            return (readData >> 5) - 1;
        }

        protected override IHaveValueNode GetLink()
        {
            switch (tag)
            {
                case Tag.MethodDef: return TildeStream.Instance.MethodDefs[Index];
                case Tag.Field: return TildeStream.Instance.Fields[Index];
                case Tag.TypeRef: return TildeStream.Instance.TypeRefs[Index];
                case Tag.TypeDef: return TildeStream.Instance.TypeDefs[Index];
                case Tag.Param: return TildeStream.Instance.Params[Index];
                //case Tag.InterfaceImpl: return TildeStream.Instance.InterfaceImpls[Index];
                case Tag.MemberRef: return TildeStream.Instance.MemberRefs[Index];
                case Tag.Module: return TildeStream.Instance.Modules[Index];
                //case Tag.Permission: return TildeStream.Instance.Permissions[Index];
                case Tag.Property: return TildeStream.Instance.Properties[Index];
                //case Tag.Event: return TildeStream.Instance.Events[Index];
                //case Tag.StandAloneSig: return TildeStream.Instance.StandAloneSigs[Index];
                //case Tag.ModuleRef: return TildeStream.Instance.ModuleRefs[Index];
                case Tag.TypeSpec: return TildeStream.Instance.TypeSpecs[Index];
                case Tag.Assembly: return TildeStream.Instance.Assemblies[Index];
                case Tag.AssemblyRef: return TildeStream.Instance.AssemblyRefs[Index];
                    //case Tag.File: return TildeStream.Instance.Files[Index];
                    //case Tag.ExportedType: return TildeStream.Instance.ExportedTypes[Index];
                    //case Tag.ManifestResource: return TildeStream.Instance.ManifestResources[Index];
                    //case Tag.GenericParam: return TildeStream.Instance.GenericParams[Index];
                    //case Tag.GenericParamConstraint: return TildeStream.Instance.GenericParamConstraints[Index];
                    //case Tag.MethodSpec: return TildeStream.Instance.MethodSpecs[Index];
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

    public class MemberRefParent : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData)
        {
            tag = (Tag)(readData & 0x7);
            return (readData >> 3) - 1;
        }

        protected override IHaveValueNode GetLink()
        {
            switch (tag)
            {
                case Tag.TypeDef: return TildeStream.Instance.TypeDefs[Index];
                case Tag.TypeRef: return TildeStream.Instance.TypeRefs[Index];
                //case Tag.ModuleRef: return TildeStream.Instance.ModuleRefs[Index];
                case Tag.MethodDef: return TildeStream.Instance.MethodDefs[Index];
                case Tag.TypeSpec: return TildeStream.Instance.TypeSpecs[Index];
            }
            throw new NotImplementedException(tag.ToString());
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

    public class CustomAttributeType : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData)
        {
            tag = (Tag)(readData & 0x7);
            return (readData >> 3) - 1;
        }

        protected override IHaveValueNode GetLink()
        {
            switch (tag)
            {
                case Tag.MethodDef: return TildeStream.Instance.MethodDefs[Index];
                case Tag.MemberRef: return TildeStream.Instance.MemberRefs[Index];
            }
            throw new InvalidOperationException(tag.ToString());
        }

        enum Tag
        {
            MethodDef = 2,
            MemberRef = 3,
        }
    }
}

// II.25
sealed class FileFormat : ICanRead
{
    public PEHeader PEHeader;
    public Section[] Sections;

    public CodeNode Read(Stream stream)
    {
        Method.MethodsByRVA.Clear();

        CodeNode node = new CodeNode
        {
            stream.ReadClass(ref PEHeader),
        };

        Sections = PEHeader.SectionHeaders.Select(header =>
            new Section(header, PEHeader.PEOptionalHeader.PEHeaderHeaderDataDirectories, PEHeader.PEOptionalHeader.PEHeaderStandardFields.EntryPointRVA)).ToArray();

        IEnumerable<CodeNode> sections;
        node.Add(sections = stream.ReadClasses(ref Sections));
        var ss = sections.ToArray();

        for (int i = 0; i < Sections.Length; ++i)
        {
            Sections[i].CallBack(ss[i]);
        }

        return node;
    }
}

// II.25.2
sealed class PEHeader : ICanRead
{
    public DosHeader DosHeader;
    public PESignature PESignature;
    public PEFileHeader PEFileHeader;
    public PEOptionalHeader PEOptionalHeader;
    public SectionHeader[] SectionHeaders;

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out DosHeader),
            stream.ReadStruct(out PESignature),
            stream.ReadStruct(out PEFileHeader),
            stream.ReadClass(ref PEOptionalHeader),
            stream.ReadStructs(out SectionHeaders, PEFileHeader.NumberOfSections),
        };
    }
}

// II.25.2.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct DosHeader
{
    [Expected('M')]
    public char MagicM;
    [Expected('Z')]
    public char MagicZ;
    [Expected(0x90)]
    public ushort BytesOnLastPageOfFile;
    [Expected(3)]
    public ushort PagesInFile;
    [Expected(0)]
    public ushort Relocations;
    [Expected(4)]
    public ushort SizeOfHeaderInParagraphs;
    [Expected(0)]
    public ushort MinimumExtraParagraphsNeeded;
    [Expected(0xFFFF)]
    public ushort MaximumExtraParagraphsNeeded;
    [Expected(0)]
    public ushort InitialRelativeStackSegmentValue;
    [Expected(0xB8)]
    public ushort InitialSP;
    [Expected(0)]
    public ushort Checksum;
    [Expected(0)]
    public ushort InitialIP;
    [Expected(0)]
    public ushort InitialRelativeCS;
    [Expected(0x40)]
    public ushort RawAddressOfRelocation; // TODO link all Raw Address, RVA, (sizes?) from understandingCIL
    [Expected(0)]
    public ushort OverlayNumber;
    [Expected(0)]
    public ulong Reserved;
    [Expected(0)]
    public ushort OemIdentifier;
    [Expected(0)]
    public ushort OemInformation;
    [Expected(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                           0x00, 0x00, 0x00, 0x00})]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] Reserved2;
    [Expected(0x80)]
    public uint LfaNew;
    [Expected(new byte[] { 0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD,
                           0x21, 0xb8, 0x01, 0x4C, 0xCD, 0x21 })]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
    public byte[] DosCode;
    [Expected("This program cannot be run in DOS mode.\r\r\n$")]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 43)]
    public char[] Message;
    [Expected(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
    public byte[] Reserved3;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PESignature
{
    [Expected('P')]
    public char MagicP;
    [Expected('E')]
    public char MagicE;
    [Expected(0)]
    public ushort Reserved;
}

// II.25.2.2 
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEFileHeader
{
    [Description("0x14c is I386.")]
    public ushort Machine; //TODO enum
    [Description("Number of sections; indicates size of the Section Table, which immediately follows the headers.")]
    public ushort NumberOfSections;
    [Description("Time and date the file was created in seconds since January 1st 1970 00:00:00 or 0.")]
    public uint TimeDateStamp;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public uint PointerToSymbolTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public uint NumberOfSymbols;
    [Description("Size of the optional header, the format is described below.")]
    public ushort OptionalHeaderSize;
    [Description("Flags indicating attributes of the file, see §II.25.2.2.1.")]
    public ushort Characteristics;
}

// II.25.2.3
class PEOptionalHeader : ICanRead
{
    // TODO (Descriptions)

    public PEHeaderStandardFields PEHeaderStandardFields;
    [Description("RVA of the data section. (This is a hint to the loader.) Only present in PE32, not PE32+")]
    public int? BaseOfData;
    public PEHeaderWindowsNtSpecificFields32? PEHeaderWindowsNtSpecificFields32;
    public PEHeaderWindowsNtSpecificFields64? PEHeaderWindowsNtSpecificFields64;
    public PEHeaderHeaderDataDirectories PEHeaderHeaderDataDirectories;

    public CodeNode Read(Stream stream)
    {
        var node = new CodeNode
        {
            stream.ReadStruct(out PEHeaderStandardFields),
        };

        switch (PEHeaderStandardFields.Magic)
        {
            case 0x10B: //TODO enum
                node.Add(stream.ReadStruct(out BaseOfData, nameof(BaseOfData)));
                node.Add(stream.ReadStruct(out PEHeaderWindowsNtSpecificFields32).Children.Single());
                break;
            case 0x20B:
                node.Add(stream.ReadStruct(out PEHeaderWindowsNtSpecificFields64).Children.Single());
                break;
            default:
                throw new InvalidOperationException($"Magic not recognized: {PEHeaderStandardFields.Magic:X}");
        }

        node.Add(stream.ReadStruct(out PEHeaderHeaderDataDirectories));

        return node;
    }
}

// II.25.2.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderStandardFields
{
    [Description("Always 0x10B.")]
    public ushort Magic; // TODO enum
    [Description("Spec says always 6, sometimes more (§II.24.1).")]
    public byte LMajor;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public byte LMinor;
    [Description("Size of the code (text) section, or the sum of all code sections if there are multiple sections.")]
    public uint CodeSize;
    [Description("Size of the initialized data section, or the sum of all such sections if there are multiple data sections.")]
    public uint InitializedDataSize;
    [Description("Size of the uninitialized data section, or the sum of all such sections if there are multiple unitinitalized data sections.")]
    public uint UninitializedDataSize;
    [Description("RVA of entry point , needs to point to bytes 0xFF 0x25 followed by the RVA in a section marked execute/read for EXEs or 0 for DLLs")]
    public uint EntryPointRVA;
    [Description("RVA of the code section. (This is a hint to the loader.)")]
    public uint BaseOfCode;
}

// II.25.2.3.2
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderWindowsNtSpecificFields<Tint>
{
    [Description("Shall be a multiple of 0x10000.")]
    public Tint ImageBase;
    [Description("Shall be greater than File Alignment.")]
    public uint SectionAlignment;
    [Description("Should be 0x200 (§II.24.1).")]
    [Expected(0x200)]
    public uint FileAlignment;
    [Description("Should be 5 (§II.24.1).")]
    public ushort OSMajor;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort OSMinor;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort UserMajor;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort UserMinor;
    [Description("Should be 5 (§II.24.1).")]
    //[Expected(5)]
    public ushort SubSysMajor;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort SubSysMinor;
    [Description("Shall be zero")]
    [Expected(0)]
    public uint Reserved;
    [Description("Size, in bytes, of image, including all headers and padding; shall be a multiple of Section Alignment.")]
    public uint ImageSize;
    [Description("Combined size of MS-DOS Header, PE Header, PE Optional Header and padding; shall be a multiple of the file alignment.")]
    public uint HeaderSize;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public uint FileChecksum;
    [Description("Subsystem required to run this image. Shall be either IMAGE_SUBSYSTEM_WINDOWS_CUI (0x3) or IMAGE_SUBSYSTEM_WINDOWS_GUI (0x2).")]
    public ushort SubSystem;
    [Description("Bits 0x100f shall be zero.")]
    public DllCharacteristics DLLFlags;
    [Description("Often 1Mb for x86 or 4Mb for x64 (§II.24.1).")]
    public Tint StackReserveSize;
    [Description("Often 4Kb for x86 or 16Kb for x64 (§II.24.1).")]
    public Tint StackCommitSize;
    [Description("Should be 0x100000 (1Mb) (§II.24.1).")]
    [Expected(0x100000)]
    public Tint HeapReserveSize;
    [Description("Often 4Kb for x86 or 8Kb for x64 (§II.24.1).")]
    public Tint HeapCommitSize;
    [Description("Shall be 0")]
    [Expected(0)]
    public uint LoaderFlags;
    [Description("Shall be 0x10")]
    [Expected(0x10)]
    public uint NumberOfDataDirectories;
}

// Generic structs aren't copyable by Marshal.PtrToStructure, so make non-generic "subclasses"
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderWindowsNtSpecificFields32
{
    public PEHeaderWindowsNtSpecificFields<uint> PEHeaderWindowsNtSpecificFields;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderWindowsNtSpecificFields64
{
    public PEHeaderWindowsNtSpecificFields<ulong> PEHeaderWindowsNtSpecificFields;
}

[Flags]
enum DllCharacteristics : ushort
{
    Reserved1 = 0x0001,
    Reserved2 = 0x0002,
    Reserved3 = 0x0004,
    Reserved4 = 0x0008,
    [Description("The DLL can be relocated at load time.")]
    DYNAMIC_BASE = 0x0040,
    [Description("Code integrity checks are forced. If you set this flag and a section contains only uninitialized data, set the PointerToRawData member of IMAGE_SECTION_HEADER for that section to zero; otherwise, the image will fail to load because the digital signature cannot be verified.")]
    FORCE_INTEGRITY = 0x0080,
    [Description("The image is compatible with data execution prevention (DEP).")]
    NX_COMPAT = 0x0100,
    [Description("The image is isolation aware, but should not be isolated.")]
    NO_ISOLATION = 0x0200,
    [Description("The image does not use structured exception handling (SEH). No handlers can be called in this image.")]
    NO_SEH = 0x0400,
    [Description("Do not bind the image. ")]
    NO_BIND = 0x0800,
    Reserved5 = 0x1000,
    [Description("A WDM driver. ")]
    WDM_DRIVER = 0x2000,
    Reserved6 = 0x4000,
    [Description("The image is terminal server aware.")]
    TERMINAL_SERVER_AWARE = 0x8000,
}

// II.25.2.3.3
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderHeaderDataDirectories
{
    // TODO RVAandSize all

    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ExportTable;
    [Description("RVA and Size of Import Table, (§II.25.3.1).")]
    public RVAandSize ImportTable;
    [Description("Always 0, unless resources are compiled in (§II.24.1).")]
    public ulong ResourceTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ExceptionTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong CertificateTable;
    [Description("Relocation Table; set to 0 if unused (§).")]
    public RVAandSize BaseRelocationTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong Debug;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong Copyright;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong GlobalPtr;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong TLSTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong LoadConfigTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong BoundImport;
    [Description("RVA and Size of Import Address Table,(§II.25.3.1).")]
    public RVAandSize ImportAddressTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong DelayImportDescriptor;
    [Description("CLI Header with directories for runtime data,(§II.25.3.1).")]
    public RVAandSize CLIHeader;
    [Description("Always 0 (§II.24.1)")]
    [Expected(0)]
    public ulong Reserved;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct RVAandSize
{
    public uint RVA;
    public uint Size;
}

// II.25.3
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct SectionHeader
{
    [Description("An 8-byte, null-padded ASCII string. There is no terminating null if the string is exactly eight characters long.")]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public char[] Name;
    [Description("Total size of the section in bytes. If this value is greater than SizeOfRawData, the section is zero-padded.")]
    public uint VirtualSize;
    [Description("For executable images this is the address of the first byte of the section, when loaded into memory, relative to the image base.")]
    public uint VirtualAddress;
    [Description("Size of the initialized data on disk in bytes, shall be a multiple of FileAlignment from the PE header. If this is less than VirtualSize the remainder of the section is zero filled. Because this field is rounded while the VirtualSize field is not it is possible for this to be greater than VirtualSize as well. When a section contains only uninitialized data, this field should be 0.")]
    public uint SizeOfRawData;
    [Description("Offset of section's first page within the PE file. This shall be a multiple of FileAlignment from the optional header. When a section contains only uninitialized data, this field should be 0.")]
    public uint PointerToRawData;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public uint PointerToRelocations;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public uint PointerToLinenumbers;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort NumberOfRelocations;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort NumberOfLinenumbers;
    [Description("Flags describing section’s characteristics.")]
    public SectionHeaderCharacteristics Characteristics;
}

[Flags]
enum SectionHeaderCharacteristics : uint
{
    ShouldNotBePadded = 0x00000008,
    ContainsCode = 0x00000020,
    ContainsInitializedData = 0x00000040,
    ContainsUninitializedData = 0x00000080,
    LinkContainsComments = 0x00000200,
    LinkShouldBeRemoved = 0x00000800,
    Link_COMDAT = 0x00001000,
    MemoryCanBeDiscarded = 0x02000000,
    MemoryCannotBeCached = 0x04000000,
    MemoryCannotBePaged = 0x08000000,
    MemoryCanBeShared = 0x10000000,
    MemoryCanBeExecutedAsCode = 0x20000000,
    MemoryCanBeRead = 0x40000000,
    MemoryCanBeWrittenTo = 0x80000000,
}

sealed class Section : ICanRead
{
    int start;
    int end;
    int rva;
    string name;
    PEHeaderHeaderDataDirectories data;
    uint entryPointRVA;

    public Section(SectionHeader header, PEHeaderHeaderDataDirectories data, uint entryPointRVA)
    {
        start = (int)header.PointerToRawData;
        end = start + (int)header.SizeOfRawData;
        rva = (int)header.VirtualAddress;
        name = new string(header.Name);
        this.data = data;
        this.entryPointRVA = entryPointRVA;
    }

    //TODO reorder children in order 
    public CodeNode Read(Stream stream)
    {
        CodeNode node = new CodeNode
        {
            Description = name,
        };

        foreach (var nr in data.GetType().GetFields()
            .Where(field => field.FieldType == typeof(RVAandSize))
            .Select(field => new { name = field.Name, rva = (RVAandSize)field.GetValue(data) })
            .Where(nr => nr.rva.RVA > 0)
            .Where(nr => rva <= nr.rva.RVA && nr.rva.RVA < rva + end - start)
            .OrderBy(nr => nr.rva.RVA))
        {
            Reposition(stream, nr.rva.RVA);

            switch (nr.name)
            {
                case "CLIHeader":
                    CLIHeader CLIHeader;
                    node.Add(stream.ReadStruct(out CLIHeader));
                    CLIHeader.Instance = CLIHeader;

                    Reposition(stream, CLIHeader.MetaData.RVA);
                    MetadataRoot MetadataRoot = null;
                    node.Add(stream.ReadClass(ref MetadataRoot));

                    foreach (var streamHeader in MetadataRoot.StreamHeaders.OrderBy(h => h.Name.IndexOf('~'))) // Read #~ after heaps
                    {
                        Reposition(stream, streamHeader.Offset + CLIHeader.MetaData.RVA);

                        switch (streamHeader.Name)
                        {
                            case "#Strings":
                                StringHeap StringHeap = new StringHeap((int)streamHeader.Size);
                                node.Add(stream.ReadClass(ref StringHeap));
                                break;
                            case "#US":
                                UserStringHeap UserStringHeap = new UserStringHeap((int)streamHeader.Size);
                                node.Add(stream.ReadClass(ref UserStringHeap));
                                break;
                            case "#Blob":
                                BlobHeap BlobHeap = new BlobHeap((int)streamHeader.Size);
                                node.Add(stream.ReadClass(ref BlobHeap));
                                break;
                            case "#GUID":
                                GuidHeap GuidHeap = new GuidHeap((int)streamHeader.Size);
                                node.Add(stream.ReadClass(ref GuidHeap));
                                break;
                            case "#~":
                                TildeStream TildeStream = null;
                                node.Add(stream.ReadClass(ref TildeStream));
                                TildeStream.Instance = TildeStream;

                                CodeNode methods = new CodeNode
                                {
                                    Name = "Methods",
                                };

                                foreach (var rva in TildeStream.MethodDefs.Select(def => def.RVA).Distinct().OrderBy(rva => rva))
                                {
                                    Reposition(stream, rva);

                                    Method method = null;
                                    methods.Add(stream.ReadClass(ref method));
                                    Method.MethodsByRVA.Add(rva, method);
                                }

                                if (methods.Children.Any())
                                {
                                    node.Add(methods);
                                }
                                break;
                            default:
                                node.AddError("Unexpected stream name: " + streamHeader.Name);
                                break;
                        }
                    }

                    break;
                case "ImportTable":
                    ImportTable ImportTable;
                    node.Add(stream.ReadStruct(out ImportTable));

                    Reposition(stream, ImportTable.ImportLookupTable);
                    ImportLookupTable ImportLookupTable;
                    node.Add(stream.ReadStruct(out ImportLookupTable));

                    Reposition(stream, ImportLookupTable.HintNameTableRVA);
                    ImportAddressHintNameTable ImportAddressHintNameTable;
                    node.Add(stream.ReadStruct(out ImportAddressHintNameTable));

                    Reposition(stream, ImportTable.Name);
                    string RuntimeEngineName;
                    node.Add(stream.ReadAnything(out RuntimeEngineName, StreamExtensions.ReadNullTerminated(Encoding.ASCII, 1), "RuntimeEngineName"));

                    Reposition(stream, entryPointRVA);
                    NativeEntryPoint NativeEntryPoint;
                    node.Add(stream.ReadStruct(out NativeEntryPoint));
                    break;
                case "ImportAddressTable":
                    ImportAddressTable ImportAddressTable;
                    node.Add(stream.ReadStruct(out ImportAddressTable));
                    break;
                case "BaseRelocationTable":
                    Relocations Relocations = null;
                    node.Add(stream.ReadClass(ref Relocations));
                    break;
                default:
                    node.AddError("Unexpected data directoriy name: " + nr.name);
                    break;
            }
        }

        return node;
    }

    void Reposition(Stream stream, long dataRVA)
    {
        stream.Position = start + dataRVA - rva;
    }

    public void CallBack(CodeNode node)
    {
        node.Start = start;
        node.End = end;
    }
}


// II.25.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportTable
{
    [Description("RVA of the Import Lookup Table")]
    public uint ImportLookupTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public uint DateTimeStamp;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public uint ForwarderChain;
    [Description("RVA of null-terminated ASCII string “mscoree.dll”.")]
    public uint Name;
    [Description("RVA of Import Address Table (this is the same as the RVA of the IAT descriptor in the optional header).")]
    public uint ImportAddressTableRVA;
    [Description("End of Import Table. Shall be filled with zeros.")]
    [Expected(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                           0x00, 0x00, 0x00, 0x00})]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] Reserved;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportAddressTable
{
    public uint HintNameTableRVA;
    [Expected(0)]
    public uint NullTerminated;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportLookupTable
{
    public uint HintNameTableRVA;
    [Expected(0)]
    public uint NullTerminated;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportAddressHintNameTable
{
    [Description("Shall be 0.")]
    [Expected(0)]
    public ushort Hint;
    [Description("Case sensitive, null-terminated ASCII string containing name to import. Shall be “_CorExeMain” for a.exe file and “_CorDllMain” for a.dll file.")]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public char[] Message;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct NativeEntryPoint
{
    [Description("JMP op code in X86")]
    [Expected(0xFF)]
    public byte JMP;
    [Description("Specifies the jump is in absolute indirect mode")]
    [Expected(0x25)]
    public byte Mod;
    [Description("Jump target RVA.")]
    public uint JumpTarget;
}

// II.25.3.2
class Relocations : ICanRead
{
    public BaseRelocationTable BaseRelocationTable;
    public Fixup[] Fixups;

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out BaseRelocationTable),
            stream.ReadClasses(ref Fixups, ((int)BaseRelocationTable.BlockSize - 8) / 2),
        };
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BaseRelocationTable
{
    public uint PageRVA;
    public uint BlockSize;
}

class Fixup : ICanRead
{
    //TODO (Descriptions)

    [Description("Stored in high 4 bits of word, type IMAGE_REL_BASED_HIGHLOW (0x3).")]
    public byte Type;
    [Description("Stored in remaining 12 bits of word. Offset from starting address specified in the Page RVA field for the block. This offset specifies where the fixup is to be applied.")]
    public short Offset;

    public CodeNode Read(Stream stream)
    {
        byte tmp = 0xCC;

        return new CodeNode
        {
            stream.ReadStruct(out Type, nameof(Type), (byte b) => {
                tmp = b;
                return (byte)(b >> 4);
            }),
            stream.ReadStruct(out Offset, nameof(Offset), (byte b) => {
                return (short)(((tmp << 8) & 0x0F00) | b);
            }),
        };
    }
}

// II.25.3.3
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct CLIHeader
{
    [Description("Size of the header in bytes")]
    public uint Cb;
    [Description("The minimum version of the runtime required to run this program, currently 2.")]
    public ushort MajorRuntimeVersion;
    [Description("The minor portion of the version, currently 0.")]
    public ushort MinorRuntimeVersion;
    [Description("RVA and size of the physical metadata (§II.24).")]
    public RVAandSize MetaData;
    [Description("Flags describing this runtime image. (§II.25.3.3.1).")]
    public CliHeaderFlags Flags;
    [Description("Token for the MethodDef or File of the entry point for the image")]
    public uint EntryPointToken;
    [Description("RVA and size of implementation-specific resources.")]
    public RVAandSize Resources;
    [Description("RVA of the hash data for this PE file used by the CLI loader for binding and versioning")]
    public RVAandSize StrongNameSignature;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong CodeManagerTable;
    [Description("RVA of an array of locations in the file that contain an array of function pointers (e.g., vtable slots), see below.")]
    public RVAandSize VTableFixups;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ExportAddressTableJumps;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ManagedNativeHeader;

    public static CLIHeader Instance; // TODO good idea?
}

[Flags]
enum CliHeaderFlags : uint
{
    ILOnly = 0x01,
    Required32Bit = 0x02,
    StrongNameSigned = 0x08,
    NativeEntryPoint = 0x10,
    TrackDebugData = 0x10000,
}

// II.25.4
sealed class Method : ICanRead, IHaveAName, IHaveValueNode
{
    public byte Header; // TODO enum
    public FatFormat FatFormat;
    public byte[] CilOps;

    static int count = 0;
    public string Name { get; } = $"{nameof(Method)}[{count++}]";

    public object Value => ""; //TODO clean up all "" Value. Should this just implment IHaveValue? How does that work with CodeNode.DelayedValueNode?

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        Node = new CodeNode
        {
            stream.ReadStruct(out Header, nameof(Header)),
        };

        int length;
        MethodHeaderType type = (MethodHeaderType)(Header & 0x03);
        switch (type)
        {
            case MethodHeaderType.Tiny:
                length = Header >> 2;
                break;
            case MethodHeaderType.Fat:
                if (((MethodHeaderType)Header).HasFlag(MethodHeaderType.MoreSects))
                {
                    throw new NotImplementedException("Exception Handlers");
                }

                Node.Add(stream.ReadStruct(out FatFormat, nameof(FatFormat)));

                if ((FatFormat.FlagsAndSize & 0xF0) != 0x30)
                {
                    Node.AddError("Expected upper bits of FlagsAndSize to be 3");
                }

                length = (int)FatFormat.CodeSize;
                break;
            default:
                throw new InvalidOperationException("Invalid MethodHeaderType " + type);
        }

        Node.Add(stream.ReadAnything(out CilOps, StreamExtensions.ReadByteArray(length), "CilOps"));

        return Node;
    }

    public static Dictionary<uint, Method> MethodsByRVA { get; } = new Dictionary<uint, Method>();
}

// II.25.4.1 and .4
enum MethodHeaderType : byte
{
    Tiny = 0x02,
    Fat = 0x03,
    MoreSects = 0x08,
    InitLocals = 0x10,
}

// II.25.4.3
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct FatFormat
{
    [Description("Lower four bits is rest of Flags, Upper four bits is size of this header expressed as the count of 4-byte integers occupied (currently 3)")]
    public byte FlagsAndSize;
    [Description("Maximum number of items on the operand stack")]
    public ushort MaxStack;
    [Description("Size in bytes of the actual method body")]
    public uint CodeSize;
    [Description("Meta Data token for a signature describing the layout of the local variables for the method")]
    public uint LocalVarSigTok;
}
