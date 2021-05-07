using System;
using System.IO;

// II.22 Metadata logical format: tables 

// ICanBeReadInOrder is written though reflection
#pragma warning disable 0649 // CS0649: Field '...' is never assigned to, and will always have its default value

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
sealed class Assembly : ICanBeReadInOrder, IHaveLiteralValueNode
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
sealed class AssemblyOS : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public uint OSPlatformID;
    [OrderedField] public uint OSMajorVersion;
    [OrderedField] public uint OSMinorVersion;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.4
sealed class AssemblyProcessor : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public uint Processor;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.5
sealed class AssemblyRef : ICanBeReadInOrder, IHaveLiteralValueNode
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
sealed class AssemblyRefOS : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public uint OSPlatformID;
    [OrderedField] public uint OSMajorVersion;
    [OrderedField] public uint OSMinorVersion;
    [OrderedField] public UnknownCodedIndex AssemblyRef;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.7
sealed class AssemblyRefProcessor : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public uint Processor;
    [OrderedField] public UnknownCodedIndex AssemblyRef;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.8
sealed class ClassLayout : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public ushort PackingSize;
    [OrderedField] public uint ClassSize;
    [OrderedField] public UnknownCodedIndex Parent;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.9
sealed class Constant : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public UnknownCodedIndex Type;
    [OrderedField] public CodedIndex.HasConstant Parent;
    [OrderedField] public BlobHeapIndex Value;

    object IHaveValue.Value => "";

    public CodeNode Node { get; set; }
}

// II.22.10
sealed class CustomAttribute : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public CodedIndex.HasCustomAttribute Parent;
    [OrderedField] public CodedIndex.CustomAttributeType Type;
    [OrderedField] public BlobHeapIndex Value; //TODO(pedant) II.23.3 Custom attributes 

    object IHaveValue.Value => "";

    public CodeNode Node { get; set; }
}

// II.22.11
sealed class DeclSecurity : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public ushort Action; // Not implementing these flags as details are lacking in 22.11
    [OrderedField] public CodedIndex.HasDeclSecurity Parent;
    [OrderedField] public BlobHeapIndex PermissionSet; // Not implementing parsing this for now

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.12
sealed class EventMap : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public UnknownCodedIndex Parent;
    [OrderedField] public UnknownCodedIndex EventList;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.13
sealed class Event : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public EventAttributes Flags;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public CodedIndex.TypeDefOrRef EventType;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.14
sealed class ExportedType : ICanBeReadInOrder, IHaveLiteralValueNode
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
sealed class Field : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public FieldAttributes Flags;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public BlobHeapIndex Signature; //TODO(FieldSig)

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.16
sealed class FieldLayout : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public uint Offset;
    [OrderedField] public UnknownCodedIndex Field;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.17
sealed class FieldMarshal : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public CodedIndex.HasFieldMarshall Parent;
    [OrderedField] public BlobHeapIndex NativeType; //TODO(Signature) II.23.4 Marshalling descriptors

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.18
sealed class FieldRVA : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public uint RVA;
    [OrderedField] public UnknownCodedIndex Field;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.19
sealed class FileTable : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public FileAttributes Flags;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public BlobHeapIndex HashValue;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.20
sealed class GenericParam : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public ushort Number;
    [OrderedField] public GenericParamAttributes Flags;
    [OrderedField] public CodedIndex.TypeOrMethodDef Owner;
    [OrderedField] public StringHeapIndex Name;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.21
sealed class GenericParamConstraint : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public UnknownCodedIndex Owner;
    [OrderedField] public CodedIndex.TypeDefOrRef Constraint;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.22
sealed class ImplMap : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public PInvokeAttributes MappingFlags;
    [OrderedField] public CodedIndex.MemberForwarded MemberForwarded;
    [OrderedField] public StringHeapIndex ImportName;
    [OrderedField] public UnknownCodedIndex ImportScope;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.23
sealed class InterfaceImpl : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public UnknownCodedIndex Class;
    [OrderedField] public CodedIndex.TypeDefOrRef Interface;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.24
sealed class ManifestResource : ICanRead, IHaveLiteralValueNode
{
    public uint Offset; //TODO(link)
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

        var section = Singletons.Instance.TildeStream.Section;
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

    public string Name { get; } = $"{nameof(ResourceEntry)}[{Singletons.Instance.ResourceEntryCount++}]";

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
sealed class MemberRef : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public CodedIndex.MemberRefParent Class;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public BlobHeapIndex Signature; //TODO(MethodRefSig)

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.26
sealed class MethodDef : ICanRead, IHaveLiteralValueNode // TODO(cleanup) can this be ICanBeReadInOrder?
{
    public uint RVA;
    public MethodImplAttributes ImplFlags;
    public MethodAttributes Flags;
    public StringHeapIndex Name;
    public BlobHeapIndex Signature; //TODO(MethodDefSig)
    public UnknownCodedIndex ParamList;

    public object Value => Name.Value;

    public CodeNode Node { get; private set; }

    public CodeNode RVANode { get; private set; }

    public CodeNode Read(Stream stream)
    {
        Node = new CodeNode
        {
            (RVANode = stream.ReadStruct(out RVA, nameof(RVA))),
            stream.ReadClass(ref ImplFlags, nameof(ImplFlags)),
            stream.ReadClass(ref Flags, nameof(Flags)),
            stream.ReadClass(ref Name, nameof(Name)),
            stream.ReadClass(ref Signature, nameof(Signature)),
            stream.ReadClass(ref ParamList, nameof(ParamList)),
        };

        return Node;
    }
}

// II.22.27
sealed class MethodImpl : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public UnknownCodedIndex Class;
    [OrderedField] public CodedIndex.MethodDefOrRef MethodBody;
    [OrderedField] public CodedIndex.MethodDefOrRef MethodDeclaration;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.28
sealed class MethodSemantics : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public MethodSemanticsAttributes Semantics;
    [OrderedField] public UnknownCodedIndex Method;
    [OrderedField] public CodedIndex.HasSemantics Association;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.29
sealed class MethodSpec : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public CodedIndex.MethodDefOrRef Method;
    [OrderedField] public BlobHeapIndex Instantiation; //TODO(MethodSpec Sig)

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.30
sealed class Module : ICanBeReadInOrder, IHaveLiteralValueNode
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
sealed class ModuleRef : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public StringHeapIndex Name;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.32
sealed class NestedClass : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public UnknownCodedIndex _NestedClass;
    [OrderedField] public UnknownCodedIndex EnclosingClass;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.33
sealed class Param : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public ParamAttributes Flags;
    [OrderedField] public ushort Sequence;
    [OrderedField] public StringHeapIndex Name;

    public object Value => Name.Value;

    public CodeNode Node { get; set; }
}

// II.22.34
sealed class Property : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public PropertyAttributes Flags;
    [OrderedField] public StringHeapIndex Name;
    [OrderedField] public BlobHeapIndex Signature; //TODO(PropertySig)

    public object Value => Name.Value;
    public CodeNode Node { get; set; }
}

// II.22.35
sealed class PropertyMap : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public UnknownCodedIndex Parent;
    [OrderedField] public UnknownCodedIndex PropertyList;

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.36
sealed class StandAloneSig : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public BlobHeapIndex Signature; //TODO(StandAloneSig)

    public object Value => "";

    public CodeNode Node { get; set; }
}

// II.22.37
sealed class TypeDef : ICanBeReadInOrder, IHaveLiteralValueNode
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
sealed class TypeRef : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public CodedIndex.ResolutionScope ResolutionScope;
    [OrderedField] public StringHeapIndex TypeName;
    [OrderedField] public StringHeapIndex TypeNamespace;

    public object Value => TypeNamespace.StringValue + "." + TypeName.StringValue;

    public CodeNode Node { get; set; }
}

// II.22.39
sealed class TypeSpec : ICanBeReadInOrder, IHaveLiteralValueNode
{
    [OrderedField] public BlobHeapIndex Signature; //TODO(TypeSpec Sig) TypeSpecSignature

    public object Value => "";
    public CodeNode Node { get; set; }
}
