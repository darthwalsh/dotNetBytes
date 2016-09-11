using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

// ICanBeReadInOrder is written though reflection
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
sealed class Assembly : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public AssemblyHashAlgorithm HashAlgId;
    [OrderedField] public ushort MajorVersion;
    [OrderedField] public ushort MinorVersion;
    [OrderedField] public ushort BuildNumber;
    [OrderedField] public ushort RevisionNumber;
    [OrderedField] public AssemblyFlags Flags;
    [OrderedField] public BlobHeapIndex PublicKey;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public StringHeapIndex Culture;

    public object Value => Name.StringValue + " " + new Version(MajorVersion, MinorVersion, BuildNumber, RevisionNumber).ToString();

    public CodeNode Node { get; set; }
}

// II.22.3
sealed class AssemblyOS : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public uint OSPlatformID;
    [OrderedField] public uint OSMajorVersion;
    [OrderedField] public uint OSMinorVersion;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.4
sealed class AssemblyProcessor : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public uint Processor;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.5
sealed class AssemblyRef : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public ushort MajorVersion;
    [OrderedField] public ushort MinorVersion;
    [OrderedField] public ushort BuildNumber;
    [OrderedField] public ushort RevisionNumber;
    [OrderedField] public AssemblyFlags Flags;
    [OrderedField] public BlobHeapIndex PublicKeyOrToken;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public StringHeapIndex Culture;
    [OrderedField] public BlobHeapIndex HashValue;

    public object Value => Name.StringValue + " " + new Version(MajorVersion, MinorVersion, BuildNumber, RevisionNumber).ToString();

    public CodeNode Node { get; set; }
}

// II.22.6
sealed class AssemblyRefOS : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public uint OSPlatformID;
    [OrderedField] public uint OSMajorVersion;
    [OrderedField] public uint OSMinorVersion;
    [OrderedField] public UnknownCodedIndex AssemblyRef;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.7
sealed class AssemblyRefProcessor : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public uint Processor;
    [OrderedField] public UnknownCodedIndex AssemblyRef;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.8
sealed class ClassLayout : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public ushort PackingSize;
    [OrderedField] public uint ClassSize;
    [OrderedField] public UnknownCodedIndex Parent;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.9
sealed class Constant : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public UnknownCodedIndex Type;
    [OrderedField] public CodedIndex.HasConstant Parent;
    [OrderedField] public BlobHeapIndex Value;

    object IHaveValue.Value => "";

    public CodeNode Node { get; set; }
}

// II.22.10
sealed class CustomAttribute : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public CodedIndex.HasCustomAttribute Parent;
    [OrderedField] public CodedIndex.CustomAttributeType Type;
    [OrderedField] public BlobHeapIndex Value;

    object IHaveValue.Value => "";

    public CodeNode Node { get; set; }
}

// II.22.11
sealed class DeclSecurity : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public ushort Action; // TODO (flags) 
    [OrderedField] public CodedIndex.HasDeclSecurity Parent;
    [OrderedField] public BlobHeapIndex PermissionSet; // TODO (parse?) 

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.12
sealed class EventMap : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public UnknownCodedIndex Parent;
    [OrderedField] public UnknownCodedIndex EventList;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.13
sealed class Event : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public EventAttributes Flags;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public CodedIndex.TypeDefOrRef EventType;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.14
sealed class ExportedType : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public TypeAttributes Flags;
    [OrderedField] public uint TypeDefId;
    [OrderedField] public StringHeapIndex TypeName;
    [OrderedField] public StringHeapIndex TypeNamespace;
    [OrderedField] public CodedIndex.Implementation Implementation;

    public object Value => TypeNamespace.StringValue + "." + TypeName.StringValue;

    public CodeNode Node { get; set; }
}

// II.22.15
sealed class Field : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public FieldAttributes Flags;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public BlobHeapIndex Signature;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.16
sealed class FieldLayout : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public uint Offset;
    [OrderedField] public UnknownCodedIndex Field;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.17
sealed class FieldMarshal : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public CodedIndex.HasFieldMarshall Parent;
    [OrderedField] public BlobHeapIndex NativeType; // TODO (Signature)

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.18
sealed class FieldRVA : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public uint RVA;
    [OrderedField] public UnknownCodedIndex Field;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.19
sealed class FileTable : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public FileAttributes Flags;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public BlobHeapIndex HashValue;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.20
sealed class GenericParam : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public ushort Number;
    [OrderedField] public GenericParamAttributes Flags;
    [OrderedField] public CodedIndex.TypeOrMethodDef Owner;
    [OrderedField] public StringHeapIndex Name;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.21
sealed class GenericParamConstraint : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public UnknownCodedIndex Owner;
    [OrderedField] public CodedIndex.TypeDefOrRef Constraint;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.22
sealed class ImplMap : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public PInvokeAttributes MappingFlags;
    [OrderedField] public CodedIndex.MemberForwarded MemberForwarded;
    [OrderedField] public StringHeapIndex ImportName;
    [OrderedField] public UnknownCodedIndex ImportScope;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.23
sealed class InterfaceImpl : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public UnknownCodedIndex Class;
    [OrderedField] public CodedIndex.TypeDefOrRef Interface;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.24
sealed class ManifestResource : ICanRead, IHaveValueNode
{
    public uint Offset; //TODO (link)
    public ManifestResourceAttributes Flags;
    public StringHeapIndex Name;
    public CodedIndex.Implementation Implementation;

    public object Value => Name.Value;

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        Node = new CodeNode
        {
            stream.ReadStruct(out Offset, nameof(Offset)),
            stream.ReadStruct(out Flags, nameof(Flags)),
            stream.ReadClass(ref Name, nameof(Name)),
            stream.ReadClass(ref Implementation, nameof(Implementation)),
        };

        Section section = TildeStream.Instance.Section;
        section.ReadNode(strm =>
        {
            section.Reposition(strm, section.CLIHeader.Resources.RVA + Offset);

            ResourceEntry entry = null;
            return stream.ReadClass(ref entry);
        });

        return Node;
    }
}

sealed class ResourceEntry : ICanRead, IHaveAName
{
    public uint Length;
    public byte[] Data;

    [ThreadStatic] //TODO (ThreadStatic) instead use local instances?
    static int count;
    public string Name { get; } = $"{nameof(ResourceEntry)}[{count++}]";

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out Length, nameof(Length)),
            stream.ReadAnything(out Data, StreamExtensions.ReadByteArray((int)Length), nameof(Data)),
        };
    }
}

// II.22.25
sealed class MemberRef : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public CodedIndex.MemberRefParent Class;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public BlobHeapIndex Signature;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.26
sealed class MethodDef : ICanRead, IHaveValueNode
{
    public uint RVA;
    public MethodImplAttributes ImplFlags;
    public MethodAttributes Flags;
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
            stream.ReadClass(ref ImplFlags, nameof(ImplFlags)),
            stream.ReadClass(ref Flags, nameof(Flags)),
            stream.ReadClass(ref Name, nameof(Name)),
            stream.ReadClass(ref Signature, nameof(Signature)),
            stream.ReadClass(ref ParamList, nameof(ParamList)),
        };

        rva.DelayedValueNode = () => new DefaultValueNode(
            rva.Value,
            RVA > 0 ? Method.MethodsByRVA[RVA].Node : null);

        return Node;
    }
}

// II.22.27
sealed class MethodImpl : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public UnknownCodedIndex Class;
    [OrderedField] public CodedIndex.MethodDefOrRef MethodBody;
    [OrderedField] public CodedIndex.MethodDefOrRef MethodDeclaration;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.28
sealed class MethodSemantics : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public MethodSemanticsAttributes Semantics;
    [OrderedField] public UnknownCodedIndex Method;
    [OrderedField] public CodedIndex.HasSemantics Association;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.29
sealed class MethodSpec : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public CodedIndex.MethodDefOrRef Method;
    [OrderedField] public BlobHeapIndex Instantiation;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.30
sealed class Module : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public ushort Generation;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public GuidHeapIndex Mvid;
    [OrderedField] public GuidHeapIndex EncId;
    [OrderedField] public GuidHeapIndex EncBaseId;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.31
sealed class ModuleRef : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public StringHeapIndex Name;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.32
sealed class NestedClass : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public UnknownCodedIndex _NestedClass;
    [OrderedField] public UnknownCodedIndex EnclosingClass;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.33
sealed class Param : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public ParamAttributes Flags;
    [OrderedField] public ushort Sequence;
    [OrderedField] public StringHeapIndex Name;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.34
sealed class Property : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public PropertyAttributes Flags;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public BlobHeapIndex Signature;

    public object Value => Name.Value;
    public CodeNode Node { get; set; }
}

// II.22.35
sealed class PropertyMap : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public UnknownCodedIndex Parent;
    [OrderedField] public UnknownCodedIndex PropertyList;

    public object Value => "";
    
    public CodeNode Node { get; set; }
}

// II.22.36
sealed class StandAloneSig : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public BlobHeapIndex Signature;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.37
sealed class TypeDef : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public TypeAttributes Flags;
    [OrderedField] public StringHeapIndex TypeName;
    [OrderedField] public StringHeapIndex TypeNamespace;
    [OrderedField] public CodedIndex.TypeDefOrRef Extends;
    [OrderedField] public UnknownCodedIndex FieldList;
    [OrderedField] public UnknownCodedIndex MethodList;

    public object Value => TypeNamespace.StringValue + "." + TypeName.StringValue;

    public CodeNode Node { get; set; }
}

// II.22.38
sealed class TypeRef : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public CodedIndex.ResolutionScope ResolutionScope;
    [OrderedField] public StringHeapIndex TypeName;
    [OrderedField] public StringHeapIndex TypeNamespace;

    public object Value => TypeNamespace.StringValue + "." + TypeName.StringValue;

    public CodeNode Node { get; set; }
}

// II.22.39
sealed class TypeSpec : ICanBeReadInOrder, IHaveValueNode
{
    [OrderedField] public BlobHeapIndex Signature;

    public object Value => "";
    public CodeNode Node { get; set; }
}



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

    public CodeNode Read(Stream stream)
    {
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

    public CodeNode Read(Stream stream)
    {
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

    public CodeNode Read(Stream stream)
    {
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

    public CodeNode Read(Stream stream)
    {
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

    public CodeNode Read(Stream stream)
    {
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

    public CodeNode Read(Stream stream)
    {
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

abstract class Heap<T> : ICanRead, IHaveAName
{
    readonly byte[] data;
    CodeNode parent;
    int offset;
    SortedList<int, Tuple<T, CodeNode>> children = new SortedList<int, Tuple<T, CodeNode>>();

    public Heap(int size)
    {
        data = new byte[size];
    }

    public CodeNode Read(Stream stream)
    {
        offset = (int)stream.Position;

        // Parsing the whole array now isn't sensible
        stream.ReadWholeArray(data);

        return parent = new CodeNode();
    }

    public string Name => GetType().Name;
    protected abstract CodeNode ReadChild(Stream stream, int index, out T t);

    protected Tuple<T, CodeNode> AddChild(IHaveIndex i)
    {
        var stream = new MemoryStream(data);
        int index = i.Index;
        stream.Position = index;

        Tuple<T, CodeNode> childpair;
        if (!children.TryGetValue(index, out childpair))
        {
            T t;
            CodeNode child = ReadChild(stream, i.Index, out t);
            if (child == null)
            {
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

    // Children shouldn't overlap, but that can happen inside these binary heaps
    void AdjustChildRanges(int index, CodeNode child)
    {
        int chI = children.IndexOfKey(index);
        if (chI != 0)
        {
            AdjustChildren(children.Values[chI - 1].Item2, child);
        }

        if (chI + 1 != children.Count)
        {
            AdjustChildren(child, children.Values[chI + 1].Item2);
        }
    }

    static void AdjustChildren(CodeNode before, CodeNode after)
    {
        if (before.End > after.Start)
        {
            before.Description = @"(Sharing bytes with the next element...)";
            before.End = after.Start;
        }
    }
}

// II.24.2.3
sealed class StringHeap : Heap<string>
{
    public StringHeap(int size)
        : base(size)
    {
        instance = this;
    }

    protected override CodeNode ReadChild(Stream stream, int index, out string s)
    {
        return stream.ReadAnything(out s, StreamExtensions.ReadNullTerminated(Encoding.UTF8, 1), $"StringHeap[{index}]");
    }

    [ThreadStatic]
    static StringHeap instance;
    public static string Get(StringHeapIndex i)
    {
        return instance.AddChild(i).Item1;
    }
    public static CodeNode GetNode(StringHeapIndex i)
    {
        return instance.AddChild(i).Item2;
    }
}

// II.24.2.4
sealed class UserStringHeap : Heap<string>
{
    public UserStringHeap(int size)
        : base(size)
    {
        instance = this;
    }

    protected override CodeNode ReadChild(Stream stream, int index, out string s)
    {
        throw new NotImplementedException("UserStringHeap");
    }

    [ThreadStatic]
    static UserStringHeap instance;
    public static string Get(UserStringHeapIndex i)
    {
        return instance.AddChild(i).Item1;
    }
    public static CodeNode GetNode(UserStringHeapIndex i)
    {
        return instance.AddChild(i).Item2;
    }
}

sealed class BlobHeap : Heap<byte[]>
{
    public BlobHeap(int size)
        : base(size)
    {
        instance = this;
    }

    protected override CodeNode ReadChild(Stream stream, int index, out byte[] b)
    {
        int length;
        int offset;
        byte first = stream.ReallyReadByte();
        if ((first & 0x80) == 0)
        {
            length = first & 0x7F;
            offset = 1;
        }
        else if ((first & 0xC0) == 0x80)
        {
            byte second = stream.ReallyReadByte();
            length = ((first & 0x3F) << 8) + second;
            offset = 2;
        }
        else if ((first & 0xE0) == 0xC0)
        {
            byte second = stream.ReallyReadByte();
            byte third = stream.ReallyReadByte();
            byte fourth = stream.ReallyReadByte();
            length = ((first & 0x1F) << 24) + (second << 16) + (third << 8) + fourth;
            offset = 4;
        }
        else
        {
            throw new InvalidOperationException($"Blob heap byte {stream.Position} can't start with 1111...");
        }

        var node = stream.ReadAnything(out b, StreamExtensions.ReadByteArray(length), $"BlobHeap[{index}]");
        node.Description = $"{offset} leading bits";
        node.Start -= offset;
        return node;
    }

    [ThreadStatic]
    static BlobHeap instance;
    public static byte[] Get(BlobHeapIndex i)
    {
        return instance.AddChild(i).Item1;
    }
    public static CodeNode GetNode(BlobHeapIndex i)
    {
        return instance.AddChild(i).Item2;
    }
}

// II.24.2.5
sealed class GuidHeap : Heap<Guid>
{
    public GuidHeap(int size)
        : base(size)
    {
        instance = this;
    }

    protected override CodeNode ReadChild(Stream stream, int index, out Guid g)
    {
        if (index == 0)
        {
            g = Guid.Empty;
            return null;
        }

        const int size = 16;

        stream.Position = (index - 1) * size; // GuidHeap is indexed from 1

        return stream.ReadAnything(out g, s => new Guid(StreamExtensions.ReadByteArray(16)(s)), $"GuidHeap[{index}]");
    }

    [ThreadStatic]
    static GuidHeap instance;
    public static Guid Get(GuidHeapIndex i)
    {
        return instance.AddChild(i).Item1;
    }
    public static CodeNode GetNode(GuidHeapIndex i)
    {
        return instance.AddChild(i).Item2;
    }
}

// II.24.2.6
sealed class TildeStream : ICanRead
{
    public Section Section { get; private set; }
    public TildeStream(Section section)
    {
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

    public CodeNode Read(Stream stream)
    {
        var node = new CodeNode
        {
            stream.ReadStruct(out TildeData),
            new CodeNode("Rows") {
                stream.ReadStructs(out Rows, ((ulong)TildeData.Valid).CountSetBits(), "Rows"),
            },
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
                return stream.ReadClasses(ref MethodSemantics, count);
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
                return stream.ReadClasses(ref Files, count);
            case MetadataTableFlags.ExportedType:
                return stream.ReadClasses(ref ExportedTypes, count);
            case MetadataTableFlags.ManifestResource:
                return stream.ReadClasses(ref ManifestResources, count);
            case MetadataTableFlags.NestedClass:
                return stream.ReadClasses(ref NestedClasses, count);
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

    [ThreadStatic]
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

sealed class StringHeapIndex : ICanRead, IHaveValue, IHaveIndex
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

    public CodeNode Read(Stream stream)
    {
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

    public CodeNode Read(Stream stream)
    {
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

    public CodeNode Read(Stream stream)
    {
        ushort index;
        var node = stream.ReadStruct(out index, nameof(index));
        shortIndex = index;

        node.Link = GuidHeap.GetNode(this);
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
    }

    public class HasConstant : CodedIndex
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
                case Tag.Field: return TildeStream.Instance.Fields[Index];
                case Tag.Param: return TildeStream.Instance.Params[Index];
                case Tag.Property: return TildeStream.Instance.Properties[Index];
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
                case Tag.InterfaceImpl: return TildeStream.Instance.InterfaceImpls[Index];
                case Tag.MemberRef: return TildeStream.Instance.MemberRefs[Index];
                case Tag.Module: return TildeStream.Instance.Modules[Index];
                //case Tag.Permission: return TildeStream.Instance.Permissions[Index]; // TODO DeclSecuritys?
                case Tag.Property: return TildeStream.Instance.Properties[Index];
                case Tag.Event: return TildeStream.Instance.Events[Index];
                case Tag.StandAloneSig: return TildeStream.Instance.StandAloneSigs[Index];
                case Tag.ModuleRef: return TildeStream.Instance.ModuleRefs[Index];
                case Tag.TypeSpec: return TildeStream.Instance.TypeSpecs[Index];
                case Tag.Assembly: return TildeStream.Instance.Assemblies[Index];
                case Tag.AssemblyRef: return TildeStream.Instance.AssemblyRefs[Index];
                case Tag.File: return TildeStream.Instance.Files[Index];
                case Tag.ExportedType: return TildeStream.Instance.ExportedTypes[Index];
                case Tag.ManifestResource: return TildeStream.Instance.ManifestResources[Index];
                case Tag.GenericParam: return TildeStream.Instance.GenericParams[Index];
                case Tag.GenericParamConstraint: return TildeStream.Instance.GenericParamConstraints[Index];
                case Tag.MethodSpec: return TildeStream.Instance.MethodSpecs[Index];
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

        protected override int GetIndex(int readData)
        {
            tag = (Tag)(readData & 0x1);
            return (readData >> 1) - 1;
        }

        protected override IHaveValueNode GetLink()
        {
            switch (tag)
            {
                case Tag.Field: return TildeStream.Instance.Fields[Index];
                case Tag.Param: return TildeStream.Instance.Params[Index];
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

        protected override int GetIndex(int readData)
        {
            tag = (Tag)(readData & 0x3);
            return (readData >> 2) - 1;
        }

        protected override IHaveValueNode GetLink()
        {
            switch (tag)
            {
                case Tag.TypeDef: return TildeStream.Instance.TypeDefs[Index];
                case Tag.MethodDef: return TildeStream.Instance.MethodDefs[Index];
                case Tag.Assembly: return TildeStream.Instance.Assemblies[Index];
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
                case Tag.ModuleRef: return TildeStream.Instance.ModuleRefs[Index];
                case Tag.MethodDef: return TildeStream.Instance.MethodDefs[Index];
                case Tag.TypeSpec: return TildeStream.Instance.TypeSpecs[Index];
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

        protected override int GetIndex(int readData)
        {
            tag = (Tag)(readData & 0x1);
            return (readData >> 1) - 1;
        }

        protected override IHaveValueNode GetLink()
        {
            switch (tag)
            {
                case Tag.Event: return TildeStream.Instance.Events[Index];
                case Tag.Property: return TildeStream.Instance.Properties[Index];
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

        protected override int GetIndex(int readData)
        {
            tag = (Tag)(readData & 0x1);
            return (readData >> 1) - 1;
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
            MethodDef = 0,
            MemberRef = 1,
        }
    }

    public class MemberForwarded : CodedIndex
    {
        Tag tag;

        protected override int GetIndex(int readData)
        {
            tag = (Tag)(readData & 0x1);
            return (readData >> 1) - 1;
        }

        protected override IHaveValueNode GetLink()
        {
            switch (tag)
            {
                case Tag.Field: return TildeStream.Instance.Fields[Index];
                case Tag.MethodDef: return TildeStream.Instance.MethodDefs[Index];
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
                case Tag.File: return TildeStream.Instance.Files[Index];
                case Tag.AssemblyRef: return TildeStream.Instance.AssemblyRefs[Index];
                case Tag.ExportedType: return TildeStream.Instance.ExportedTypes[Index];
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
                case Tag.ModuleRef: return TildeStream.Instance.ModuleRefs[Index];
                case Tag.AssemblyRef: return TildeStream.Instance.AssemblyRefs[Index];
                case Tag.TypeRef: return TildeStream.Instance.TypeRefs[Index];
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

        protected override int GetIndex(int readData)
        {
            tag = (Tag)(readData & 0x1);
            return (readData >> 1) - 1;
        }

        protected override IHaveValueNode GetLink()
        {
            switch (tag)
            {
                case Tag.TypeDef: return TildeStream.Instance.TypeDefs[Index];
                case Tag.MethodDef: return TildeStream.Instance.MethodDefs[Index];
            }
            throw new InvalidOperationException(tag.ToString());
        }

        enum Tag
        {
            TypeDef = 0,
            MethodDef = 1,
        }
    }

    class ExtendsNothing : IHaveValueNode
    {
        public object Value => "(Nothing)";
        public CodeNode Node => null;
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

        node.Add(stream.ReadClasses(ref Sections));
        for (int i = 0; i < Sections.Length; ++i)
        {
            Sections[i].CallBack();
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
    public MachineType Machine;
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

enum MachineType : ushort
{
    Unknown = 0x0,
    Am33 = 0x1d3,
    Amd64 = 0x8664,
    Arm = 0x1c0,
    Armnt = 0x1c4,
    Arm64 = 0xaa64,
    Ebc = 0xebc,
    I386 = 0x14c,
    Ia64 = 0x200,
    M32r = 0x9041,
    Mips16 = 0x266,
    Mipsfpu = 0x366,
    Mipsfpu16 = 0x466,
    Powerpc = 0x1f0,
    Powerpcfp = 0x1f1,
    R4000 = 0x166,
    Sh3 = 0x1a2,
    Sh3dsp = 0x1a3,
    Sh4 = 0x1a6,
    Sh5 = 0x1a8,
    Thumb = 0x1c2,
    Wcemipsv2 = 0x169
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
            case PE32Magic.PE32:
                node.Add(stream.ReadStruct(out BaseOfData, nameof(BaseOfData)));
                node.Add(stream.ReadStruct(out PEHeaderWindowsNtSpecificFields32).Children.Single());
                break;
            case PE32Magic.PE32plus:
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
    [Description("Identifies version.")]
    public PE32Magic Magic;
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

enum PE32Magic : ushort
{
    PE32 = 0x10b,
    PE32plus = 0x20b,
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

    public CLIHeader CLIHeader;
    public MetadataRoot MetadataRoot;
    public ImportTable ImportTable;
    public ImportLookupTable ImportLookupTable;
    public ImportAddressHintNameTable ImportAddressHintNameTable;
    public NativeEntryPoint NativeEntryPoint;
    public ImportAddressTable ImportAddressTable;
    public Relocations Relocations;

    CodeNode node;

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
        node = new CodeNode
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
                    node.Add(stream.ReadStruct(out CLIHeader));

                    Reposition(stream, CLIHeader.MetaData.RVA);
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
                                TildeStream TildeStream = new TildeStream(this);
                                TildeStream.Instance = TildeStream;
                                node.Add(stream.ReadClass(ref TildeStream));
                                
                                CodeNode methods = new CodeNode
                                {
                                    Name = "Methods",
                                };

                                foreach (var rva in (TildeStream.MethodDefs ?? new MethodDef[0])
                                    .Select(def => def.RVA)
                                    .Where(rva => rva > 0)
                                    .Distinct()
                                    .OrderBy(rva => rva))
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
                    node.Add(stream.ReadStruct(out ImportTable));

                    Reposition(stream, ImportTable.ImportLookupTable);
                    node.Add(stream.ReadStruct(out ImportLookupTable));

                    Reposition(stream, ImportLookupTable.HintNameTableRVA);
                    node.Add(stream.ReadStruct(out ImportAddressHintNameTable));

                    Reposition(stream, ImportTable.Name);
                    string RuntimeEngineName;
                    node.Add(stream.ReadAnything(out RuntimeEngineName, StreamExtensions.ReadNullTerminated(Encoding.ASCII, 1), "RuntimeEngineName"));

                    Reposition(stream, entryPointRVA);
                    node.Add(stream.ReadStruct(out NativeEntryPoint));
                    break;
                case "ImportAddressTable":
                    node.Add(stream.ReadStruct(out ImportAddressTable));
                    break;
                case "BaseRelocationTable":
                    node.Add(stream.ReadClass(ref Relocations));
                    break;
                default:
                    node.AddError("Unexpected data directoriy name: " + nr.name);
                    break;
            }
        }

        foreach (var toRead in toReads)
        {
            node.Add(toRead(stream));
        }

        return node;
    }

    List<Func<Stream, CodeNode>> toReads = new List<Func<Stream, CodeNode>>();
    public void ReadNode(Func<Stream, CodeNode> read)
    {
        toReads.Add(read);
    }

    public void Reposition(Stream stream, long dataRVA)
    {
        stream.Position = start + dataRVA - rva;
    }

    public void CallBack()
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
    public MethodDataSection[] DataSections;
    public byte[] CilOps;

    [ThreadStatic]
    static int count;
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
        bool moreSects = false;
        MethodHeaderType type = (MethodHeaderType)(Header & 0x03);
        switch (type)
        {
            case MethodHeaderType.Tiny:
                length = Header >> 2;
                break;
            case MethodHeaderType.Fat:
                Node.Add(stream.ReadStruct(out FatFormat, nameof(FatFormat)));

                if ((FatFormat.FlagsAndSize & 0xF0) != 0x30)
                {
                    Node.AddError("Expected upper bits of FlagsAndSize to be 3");
                }

                length = (int)FatFormat.CodeSize;
                moreSects = ((MethodHeaderType)Header).HasFlag(MethodHeaderType.MoreSects);
                break;
            default:
                throw new InvalidOperationException("Invalid MethodHeaderType " + type);
        }

        Node.Add(stream.ReadAnything(out CilOps, StreamExtensions.ReadByteArray(length), "CilOps"));

        if (moreSects)
        {
            while (stream.Position % 4 != 0)
            {
                var b = stream.ReadByte();
            }

            var dataSections = new List<MethodDataSection>();
            var dataSectionNode = new CodeNode { Name = "MethodDataSection" };

            MethodDataSection dataSection = null;
            do
            {
                dataSectionNode.Add(stream.ReadClass(ref dataSection, $"MethodDataSections[{dataSections.Count}]"));
                dataSections.Add(dataSection);
            }
            while (dataSection.Header.HasFlag(MethodHeaderSection.MoreSects));

            DataSections = dataSections.ToArray();
            Node.Add(dataSectionNode.Children.Count == 1 ? dataSectionNode.Children.Single() : dataSectionNode);
        }

        return Node;
    }

    [ThreadStatic]
    static Dictionary<uint, Method> methodsByRVA;
    public static Dictionary<uint, Method> MethodsByRVA
    {
        get
        {
            if (methodsByRVA == null)
            {
                methodsByRVA = new Dictionary<uint, Method>();
            }
            return methodsByRVA;
        }
    }
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

// II.25.4.5
sealed class MethodDataSection : ICanRead
{
    public MethodHeaderSection Header;
    public LargeMethodHeader LargeMethodHeader;
    public SmallMethodHeader SmallMethodHeader;

    public CodeNode Read(Stream stream)
    {
        var node = new CodeNode
        {
            stream.ReadStruct(out Header, nameof(MethodHeaderSection))
        };

        if (!Header.HasFlag(MethodHeaderSection.EHTable))
        {
            throw new InvalidOperationException("Only kind of section data is exception header");
        }
        
        if (Header.HasFlag(MethodHeaderSection.FatFormat))
        {
            node.Add(stream.ReadClass(ref LargeMethodHeader, nameof(LargeMethodHeader)));
        }
        else
        {
            node.Add(stream.ReadClass(ref SmallMethodHeader, nameof(SmallMethodHeader)));
        }

        return node;
    }
}

[Flags]
enum MethodHeaderSection : byte
{
    [Description("Exception handling data.")]
    EHTable = 0x1,
    [Description("Reserved, shall be 0.")]
    OptILTable = 0x2,
    [Description("Data format is of the fat variety, meaning there is a 3-byte length least-significant byte first format. If not set, the header is small with a 1-byte length")]
    FatFormat = 0x40,
    [Description("Another data section occurs after this current section")]
    MoreSects = 0x80,
}

sealed class SmallMethodHeader : ICanRead
{
    [Description("Size of the data for the block, including the header, say n * 12 + 4.")]
    public byte DataSize;
    [Description("Padding, always 0.")]
    [Expected(0)]
    public ushort Reserved;
    public SmallExceptionHandlingClause[] Clauses;

    public CodeNode Read(Stream stream)
    {
        var node = new CodeNode
        {
            stream.ReadStruct(out DataSize, nameof(DataSize)),
            stream.ReadStruct(out Reserved, nameof(Reserved)),
        };

        var n = (DataSize - 4) / 12;
        if (n * 12 + 4 != DataSize)
        {
            node.AddError("DataSize was not of the form n * 12 + 4");
        }

        node.Add(stream.ReadStructs(out Clauses, n, nameof(Clauses)));

        return node;
    }
}

sealed class LargeMethodHeader : ICanRead
{
    [Description("Size of the data for the block, including the header, say n * 24 + 4.")]
    public UInt24 DataSize;
    public SmallExceptionHandlingClause[] Clauses;

    public CodeNode Read(Stream stream)
    {
        var node = new CodeNode
        {
            stream.ReadClass(ref DataSize, nameof(DataSize)),
        };

        var n = (DataSize.IntValue - 4) / 12;
        if (n * 24 + 4 != DataSize.IntValue)
        {
            node.AddError("DataSize was not of the form n * 24 + 4");
        }

        node.Add(stream.ReadStructs(out Clauses, n, nameof(Clauses)));

        return node;
    }
}

// II.25.4.6
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct SmallExceptionHandlingClause
{
    [Description("Flags, see below.")]
    public ushort Flags; //TODO (flags)
    [Description("Offset in bytes of try block from start of method body.")]
    public ushort TryOffset; //TODO (links)
    [Description("Length in bytes of the try block")]
    public byte TryLength;
    [Description("Location of the handler for this try block")]
    public ushort HandlerOffset;
    [Description("Size of the handler code in bytes")]
    public byte HandlerLength;
    [Description("Meta data token for a type-based exception handler OR Offset in method body for filter-based exception handler")]
    public uint ClassTokenOrFilterOffset; //TODO (links)
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct LargeExceptionHandlingClause
{
    [Description("Flags, see below.")]
    public uint Flags; //TODO (flags)
    [Description("Offset in bytes of try block from start of method body.")]
    public uint TryOffset;
    [Description("Length in bytes of the try block")]
    public uint TryLength;
    [Description("Location of the handler for this try block")]
    public uint HandlerOffset;
    [Description("Size of the handler code in bytes")]
    public uint HandlerLength;
    [Description("Meta data token for a type-based exception handler OR offset in method body for filter-based exception handler")]
    public uint ClassTokenOrFilterOffset;
}

sealed class UInt24 : ICanRead, IHaveValue
{
    public int IntValue { get; private set; }
    public object Value => IntValue;

    public CodeNode Read(Stream stream)
    {
        byte[] b;
        stream.ReadStructs(out b, 3);

        IntValue = (b[2] << 16) + (b[1] << 8) + b[0];

        return new CodeNode();
    }
}
