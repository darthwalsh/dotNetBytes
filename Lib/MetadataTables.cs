using System;
using System.IO;
using System.Linq;

// II.22 Metadata logical format: tables 

// MyCodeNode is written though reflection
#pragma warning disable 0649 // CS0649: Field '...' is never assigned to

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
sealed class Assembly : MyCodeNode
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

  public override string NodeValue => Name.StringValue + " " + new Version(MajorVersion, MinorVersion, BuildNumber, RevisionNumber).ToString();
}

// II.22.3
sealed class AssemblyOS : MyCodeNode
{
  [OrderedField] public uint OSPlatformID;
  [OrderedField] public uint OSMajorVersion;
  [OrderedField] public uint OSMinorVersion;
}

// II.22.4
sealed class AssemblyProcessor : MyCodeNode
{
  [OrderedField] public uint Processor;
}

// II.22.5
sealed class AssemblyRef : MyCodeNode
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

  public override string NodeValue => Name.StringValue + " " + new Version(MajorVersion, MinorVersion, BuildNumber, RevisionNumber).ToString();
}

// II.22.6
sealed class AssemblyRefOS : MyCodeNode
{
  [OrderedField] public uint OSPlatformID;
  [OrderedField] public uint OSMajorVersion;
  [OrderedField] public uint OSMinorVersion;
  [OrderedField] public UnknownCodedIndex AssemblyRef;
}

// II.22.7
sealed class AssemblyRefProcessor : MyCodeNode
{
  [OrderedField] public uint Processor;
  [OrderedField] public UnknownCodedIndex AssemblyRef;
}

// II.22.8
sealed class ClassLayout : MyCodeNode
{
  [OrderedField] public ushort PackingSize;
  [OrderedField] public uint ClassSize;
  [OrderedField] public UnknownCodedIndex Parent;
}

// II.22.9
sealed class Constant : MyCodeNode
{
  [OrderedField] public UnknownCodedIndex Type;
  [OrderedField] public CodedIndex.HasConstant Parent;
  [OrderedField] public BlobHeapIndex Value;
}

// II.22.10
sealed class CustomAttribute : MyCodeNode
{
  [OrderedField] public CodedIndex.HasCustomAttribute Parent;
  [OrderedField] public CodedIndex.CustomAttributeType Type;
  [OrderedField] public BlobHeapIndex Value; //TODO(pedant) II.23.3 Custom attributes 
}

// II.22.11
sealed class DeclSecurity : MyCodeNode
{
  [OrderedField] public ushort Action; // Not implementing these flags as details are lacking in 22.11
  [OrderedField] public CodedIndex.HasDeclSecurity Parent;
  [OrderedField] public BlobHeapIndex PermissionSet; // Not implementing parsing this for now
}

// II.22.12
sealed class EventMap : MyCodeNode
{
  [OrderedField] public UnknownCodedIndex Parent;
  [OrderedField] public UnknownCodedIndex EventList;
}

// II.22.13
sealed class Event : MyCodeNode
{
  [OrderedField] public EventAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public CodedIndex.TypeDefOrRef EventType;

  public override string NodeValue => Name.NodeValue;
}

// II.22.14
sealed class ExportedType : MyCodeNode
{
  [OrderedField] public TypeAttributes Flags;
  [OrderedField] public uint TypeDefId;
  [OrderedField] public StringHeapIndex TypeName;
  [OrderedField] public StringHeapIndex TypeNamespace;
  [OrderedField] public CodedIndex.Implementation Implementation;

  public override string NodeValue => TypeNamespace.StringValue + "." + TypeName.StringValue;
}

// II.22.15
sealed class Field : MyCodeNode
{
  [OrderedField] public FieldAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public BlobHeapIndex Signature; //TODO(FieldSig) 

  public override string NodeValue => Name.NodeValue;
}

// II.22.16
sealed class FieldLayout : MyCodeNode
{
  [OrderedField] public uint Offset;
  [OrderedField] public UnknownCodedIndex Field;
}

// II.22.17
sealed class FieldMarshal : MyCodeNode
{
  [OrderedField] public CodedIndex.HasFieldMarshall Parent;
  [OrderedField] public BlobHeapIndex NativeType; //TODO(Signature) II.23.4 Marshalling descriptors
}

// II.22.18
sealed class FieldRVA : MyCodeNode
{
  [OrderedField] public uint RVA;
  [OrderedField] public UnknownCodedIndex Field;
}

// II.22.19
sealed class FileTable : MyCodeNode
{
  [OrderedField] public FileAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public BlobHeapIndex HashValue;

  public override string NodeValue => Name.NodeValue;
}

// II.22.20
sealed class GenericParam : MyCodeNode
{
  [OrderedField] public ushort Number;
  [OrderedField] public GenericParamAttributes Flags;
  [OrderedField] public CodedIndex.TypeOrMethodDef Owner;
  [OrderedField] public StringHeapIndex Name;

  public override string NodeValue => Name.NodeValue;
}

// II.22.21
sealed class GenericParamConstraint : MyCodeNode
{
  [OrderedField] public UnknownCodedIndex Owner;
  [OrderedField] public CodedIndex.TypeDefOrRef Constraint;
}

// II.22.22
sealed class ImplMap : MyCodeNode
{
  [OrderedField] public PInvokeAttributes MappingFlags;
  [OrderedField] public CodedIndex.MemberForwarded MemberForwarded;
  [OrderedField] public StringHeapIndex ImportName;
  [OrderedField] public UnknownCodedIndex ImportScope;
}

// II.22.23
sealed class InterfaceImpl : MyCodeNode
{
  [OrderedField] public UnknownCodedIndex Class;
  [OrderedField] public CodedIndex.TypeDefOrRef Interface;
}

// II.22.24
sealed class ManifestResource : MyCodeNode
{
  [OrderedField] public uint Offset; //TODO(link)
  [OrderedField] public ManifestResourceAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public CodedIndex.Implementation Implementation;

  public override string NodeValue => Name.NodeValue;

  public override void Read() {
    base.Read();

    var origPos = Bytes.Stream.Position;
    try {
      var section = Bytes.CLIHeaderSection;
      section.Reposition(section.CLIHeader.Resources.RVA + Offset);

    } finally {
      Bytes.Stream.Position = origPos;
    }
  }
}

sealed class ResourceEntry : MyCodeNode
{
  int i;
  public ResourceEntry(int i) {
    this.i = i;
  }

  public override void Read() {
    var offset = Bytes.TildeStream.ManifestResources[i].Offset;
    Bytes.CLIHeaderSection.Reposition(offset + Bytes.CLIHeaderSection.CLIHeader.Resources.RVA);
    base.Read();
  }

  [OrderedField] public uint Length;
  [OrderedField] public byte[] Data;

  protected override int GetCount(string field) => field switch {
    nameof(Data) => (int)Length,
    _ => base.GetCount(field),
  };
}

// II.22.25
sealed class MemberRef : MyCodeNode
{
  [OrderedField] public CodedIndex.MemberRefParent Class;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public BlobHeapIndex Signature; //TODO(MethodRefSig)

  public override string NodeValue => Name.NodeValue;
}

// II.22.26
sealed class MethodDef : MyCodeNode
{
  [OrderedField] public uint RVA;
  [OrderedField] public MethodImplAttributes ImplFlags;
  [OrderedField] public MethodAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public BlobHeapIndex Signature; //TODO(MethodDefSig)
  [OrderedField] public UnknownCodedIndex ParamList;

  public override string NodeValue => Name.NodeValue;

  public void SetLink(Method method) {
    Children.First().Link = method;
  }
}

// II.22.27
sealed class MethodImpl : MyCodeNode
{
  [OrderedField] public UnknownCodedIndex Class;
  [OrderedField] public CodedIndex.MethodDefOrRef MethodBody;
  [OrderedField] public CodedIndex.MethodDefOrRef MethodDeclaration;
}

// II.22.28
sealed class MethodSemantics : MyCodeNode
{
  [OrderedField] public MethodSemanticsAttributes Semantics;
  [OrderedField] public UnknownCodedIndex Method;
  [OrderedField] public CodedIndex.HasSemantics Association;
}

// II.22.29
sealed class MethodSpec : MyCodeNode
{
  [OrderedField] public CodedIndex.MethodDefOrRef Method;
  [OrderedField] public BlobHeapIndex Instantiation; //TODO(MethodSpec Sig)
}

// II.22.30
sealed class Module : MyCodeNode
{
  [OrderedField] public ushort Generation;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public GuidHeapIndex Mvid;
  [OrderedField] public GuidHeapIndex EncId;
  [OrderedField] public GuidHeapIndex EncBaseId;

  public override string NodeValue => Name.NodeValue;
}

// II.22.31
sealed class ModuleRef : MyCodeNode
{
  [OrderedField] public StringHeapIndex Name;

  public override string NodeValue => Name.NodeValue;
}

// II.22.32 (Should be NestedClass but renaming type so field is allowed to be NestedClass)
sealed class Nestedclass : MyCodeNode
{
  [OrderedField] public UnknownCodedIndex NestedClass;
  [OrderedField] public UnknownCodedIndex EnclosingClass;
}

// II.22.33
sealed class Param : MyCodeNode
{
  [OrderedField] public ParamAttributes Flags;
  [OrderedField] public ushort Sequence;
  [OrderedField] public StringHeapIndex Name;

  public override string NodeValue => Name.NodeValue;
}

// II.22.34
sealed class Property : MyCodeNode
{
  [OrderedField] public PropertyAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public BlobHeapIndex Signature; //TODO(PropertySig)

  public override string NodeValue => Name.NodeValue;
}

// II.22.35
sealed class PropertyMap : MyCodeNode
{
  [OrderedField] public UnknownCodedIndex Parent;
  [OrderedField] public UnknownCodedIndex PropertyList;
}

// II.22.36
sealed class StandAloneSig : MyCodeNode
{
  [OrderedField] public BlobHeapIndex Signature; //TODO(StandAloneSig)
}

// II.22.37
sealed class TypeDef : MyCodeNode
{
  [OrderedField] public TypeAttributes Flags;
  [OrderedField] public StringHeapIndex TypeName;
  [OrderedField] public StringHeapIndex TypeNamespace;
  [OrderedField] public CodedIndex.TypeDefOrRef Extends;
  [OrderedField] public UnknownCodedIndex FieldList;
  [OrderedField] public UnknownCodedIndex MethodList;

  public override string NodeValue => TypeNamespace.StringValue + "." + TypeName.StringValue;
}

// II.22.38
sealed class TypeRef : MyCodeNode
{
  [OrderedField] public CodedIndex.ResolutionScope ResolutionScope;
  [OrderedField] public StringHeapIndex TypeName;
  [OrderedField] public StringHeapIndex TypeNamespace;

  public override string NodeValue => TypeNamespace.StringValue + "." + TypeName.StringValue;
}

// II.22.39
sealed class TypeSpec : MyCodeNode
{
  [OrderedField] public BlobHeapIndex Signature; //TODO(TypeSpec Sig) TypeSpecSignature
}
