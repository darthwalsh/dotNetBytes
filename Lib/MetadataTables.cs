using System;
using System.IO;
using System.Linq;

// II.22 Metadata logical format: tables 

// CodeNode is written though reflection
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
sealed class Assembly : CodeNode
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

  public override string NodeValue => Name.NodeValue + " " + new Version(MajorVersion, MinorVersion, BuildNumber, RevisionNumber).ToString();
}

// II.22.3
sealed class AssemblyOS : CodeNode
{
  [OrderedField] public uint OSPlatformID;
  [OrderedField] public uint OSMajorVersion;
  [OrderedField] public uint OSMinorVersion;
}

// II.22.4
sealed class AssemblyProcessor : CodeNode
{
  [OrderedField] public uint Processor;
}

// II.22.5
sealed class AssemblyRef : CodeNode
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

  public override string NodeValue => Name.NodeValue + " " + new Version(MajorVersion, MinorVersion, BuildNumber, RevisionNumber).ToString();
}

// II.22.6
sealed class AssemblyRefOS : CodeNode
{
  [OrderedField] public uint OSPlatformID;
  [OrderedField] public uint OSMajorVersion;
  [OrderedField] public uint OSMinorVersion;
  [OrderedField] public TableIndex<AssemblyRef> AssemblyRef;
}

// II.22.7
sealed class AssemblyRefProcessor : CodeNode
{
  [OrderedField] public uint Processor;
  [OrderedField] public TableIndex<AssemblyRef> AssemblyRef;
}

// II.22.8
sealed class ClassLayout : CodeNode
{
  [OrderedField] public ushort PackingSize;
  [OrderedField] public uint ClassSize;
  [OrderedField] public TableIndex<TypeDef> Parent;
}

// II.22.9
sealed class Constant : CodeNode
{
  [OrderedField] public ElementType Type;
  [OrderedField] public byte unused;
  [OrderedField] public CodedIndex.HasConstant Parent;
  [OrderedField] public BlobHeapIndex Value; // MAYBE parse value based on II.16.2
}

// II.22.10
sealed class CustomAttribute : CodeNode
{
  [OrderedField] public CodedIndex.HasCustomAttribute Parent;
  [OrderedField] public CodedIndex.CustomAttributeType Type;
  [OrderedField] public BlobHeapIndex Value; //TODO(pedant) II.23.3 Custom attributes 
}

// II.22.11
sealed class DeclSecurity : CodeNode
{
  [OrderedField] public ushort Action; // Not implementing these flags as details are lacking in 22.11
  [OrderedField] public CodedIndex.HasDeclSecurity Parent;
  [OrderedField] public BlobHeapIndex PermissionSet; // MAYBE parse this
}

// II.22.12
sealed class EventMap : CodeNode
{
  [OrderedField] public TableIndex<TypeDef> Parent;
  [OrderedField] public UnknownCodedIndex EventList; // contiguous run of Events; continues to last row or the next EventList
}

// II.22.13
sealed class Event : CodeNode
{
  [OrderedField] public EventAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public CodedIndex.TypeDefOrRef EventType;

  public override string NodeValue => Name.NodeValue;
}

// II.22.14
sealed class ExportedType : CodeNode
{
  [OrderedField] public TypeAttributes Flags;
  [OrderedField] public uint TypeDefId;
  [OrderedField] public StringHeapIndex TypeName;
  [OrderedField] public StringHeapIndex TypeNamespace;
  [OrderedField] public CodedIndex.Implementation Implementation;

  public override string NodeValue => TypeNamespace.NodeValue != "" ?
    TypeNamespace.NodeValue + "." + TypeName.NodeValue :
    TypeName.NodeValue;
}

// II.22.15
sealed class Field : CodeNode
{
  [OrderedField] public FieldAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public Signature<FieldSig> Signature;

  public override string NodeValue => Signature.Value.NamedValue(Name.NodeValue);
}

// II.22.16
sealed class FieldLayout : CodeNode
{
  [OrderedField] public uint Offset;
  [OrderedField] public TableIndex<Field> Field;
}

// II.22.17
sealed class FieldMarshal : CodeNode
{
  [OrderedField] public CodedIndex.HasFieldMarshall Parent;
  [OrderedField] public BlobHeapIndex NativeType; // MAYBE Implement parsing Marshalling Descriptor Signature but II.23.4 is missing details for enum values that ilasm creates...
  /* II.23.4 Marshalling descriptors lists some native types, but at least ilasm created a 0x1E fixed array
   see 
   https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/metadata/cornativetype-enumeration
   for a full list. And dotnet core has full implementation for fixed array, safe array, etc.:
   - https://github.com/dotnet/runtime/blob/6a9245f9a1a85773713ca8985a1bdd3d1c650aed/src/coreclr/ilasm/prebuilt/asmparse.cpp#L3995
   - https://github.com/dotnet/runtime/blob/d4f06a9c524819dfd1345745a17b3cc6e060ba8b/src/coreclr/inc/formattype.cpp#L1234
   - https://github.com/dotnet/runtime/blob/1043f003c2b6e404014845e42d25513cebe2b9d9/src/coreclr/tools/metainfo/mdinfo.cpp#L2270
   - https://github.com/dotnet/runtime/blob/6a9245f9a1a85773713ca8985a1bdd3d1c650aed/src/coreclr/md/compiler/custattr_emit.cpp#L1569
  */
}

// II.22.18
sealed class FieldRVA : CodeNode
{
  [OrderedField] public uint RVA;
  [OrderedField] public TableIndex<Field> Field;
}

// II.22.19
sealed class FileTable : CodeNode
{
  [OrderedField] public FileAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public BlobHeapIndex HashValue;

  public override string NodeValue => Name.NodeValue;
}

// II.22.20
sealed class GenericParam : CodeNode
{
  [OrderedField] public ushort Number;
  [OrderedField] public GenericParamAttributes Flags;
  [OrderedField] public CodedIndex.TypeOrMethodDef Owner;
  [OrderedField] public StringHeapIndex Name;

  public override string NodeValue => Name.NodeValue;
}

// II.22.21
sealed class GenericParamConstraint : CodeNode
{
  [OrderedField] public TableIndex<GenericParam> Owner;
  [OrderedField] public CodedIndex.TypeDefOrRef Constraint;
}

// II.22.22
sealed class ImplMap : CodeNode
{
  [OrderedField] public PInvokeAttributes MappingFlags;
  [OrderedField] public CodedIndex.MemberForwarded MemberForwarded;
  [OrderedField] public StringHeapIndex ImportName;
  [OrderedField] public TableIndex<ModuleRef> ImportScope;
}

// II.22.23
sealed class InterfaceImpl : CodeNode
{
  [OrderedField] public TableIndex<TypeDef> Class;
  [OrderedField] public CodedIndex.TypeDefOrRef Interface;
}

// II.22.24
sealed class ManifestResource : CodeNode
{
  [OrderedField] public uint Offset; //TODO(link)
  [OrderedField] public ManifestResourceAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public CodedIndex.Implementation Implementation;

  public override string NodeValue => Name.NodeValue;
}

sealed class ResourceEntry : CodeNode
{
  int i;
  public ResourceEntry(int i) {
    this.i = i;
  }

  [OrderedField] public uint Length;
  [OrderedField] public byte[] Data;

  protected override long BeforeReposition =>
    Bytes.TildeStream.ManifestResources[i].Offset +
    Bytes.CLIHeaderSection.CLIHeader.Resources.RVA;

  protected override int GetCount(string field) => field switch {
    nameof(Data) => (int)Length,
    _ => base.GetCount(field),
  };
}

// II.22.25
sealed class MemberRef : CodeNode
{
  [OrderedField] public CodedIndex.MemberRefParent Class;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public EitherSignature Signature = new EitherSignature("MethodRefSig", CallingConvention.LowerBits.FIELD);

  public override string NodeValue => Signature.NamedValue(Name.NodeValue);
}

// II.22.26
sealed class MethodDef : CodeNode
{
  [OrderedField] public uint RVA;
  [OrderedField] public MethodImplAttributes ImplFlags;
  [OrderedField] public MethodAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public EitherSignature Signature = new EitherSignature("MethodDefSig", 0); // Not an either-or signature, but ensure name isn't "MethodDefRefSig"
                                                                                            //TODO(SpecViolation) typo in II.23.2.1 MethodDefSig -- MethodDefSig is indexed by "Method.Signature" not "MethodDef.Signature"
                                                                                            // ALSO in II.22.28 MethodSemantics: 0x18 
                                                                                            // ALSO in II.22.36 StandAloneSig: 0x11

  [OrderedField] public UnknownCodedIndex ParamList; // contiguous run of Params; continues to last row or the next ParamList

  public override string NodeValue => Signature.NamedValue(Name.NodeValue);

  public void SetLink(Method method) {
    Children.First().Link = method;
  }
}

// II.22.27
sealed class MethodImpl : CodeNode
{
  [OrderedField] public TableIndex<TypeDef> Class;
  [OrderedField] public CodedIndex.MethodDefOrRef MethodBody;
  [OrderedField] public CodedIndex.MethodDefOrRef MethodDeclaration;
}

// II.22.28
sealed class MethodSemantics : CodeNode
{
  [OrderedField] public MethodSemanticsAttributes Semantics;
  [OrderedField] public TableIndex<MethodDef> Method;
  [OrderedField] public CodedIndex.HasSemantics Association;
}

// II.22.29
sealed class MethodSpec : CodeNode
{
  public override string NodeValue => Method.NodeValue.Replace("<>", Instantiation.NodeValue);
  [OrderedField] public CodedIndex.MethodDefOrRef Method;
  [OrderedField] public Signature<MethodSpecSig> Instantiation;
}

// II.22.30
sealed class Module : CodeNode
{
  [OrderedField] public ushort Generation;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public GuidHeapIndex Mvid;
  [OrderedField] public GuidHeapIndex EncId;
  [OrderedField] public GuidHeapIndex EncBaseId;

  public override string NodeValue => Name.NodeValue;
}

// II.22.31
sealed class ModuleRef : CodeNode
{
  [OrderedField] public StringHeapIndex Name;

  public override string NodeValue => Name.NodeValue;
}

// II.22.32 (Should be NestedClass but renaming type so field is allowed to be NestedClass)
sealed class Nestedclass : CodeNode
{
  [OrderedField] public TableIndex<TypeDef> NestedClass;
  [OrderedField] public TableIndex<TypeDef> EnclosingClass; // MAYBE printing typename of nestedclass should be like Outer/Inner
}

// II.22.33
sealed class Param : CodeNode
{
  [OrderedField] public ParamAttributes Flags;
  [OrderedField] public ushort Sequence;
  [OrderedField] public StringHeapIndex Name;

  public override string NodeValue => Name.NodeValue.Length > 0 ? Name.NodeValue : "<empty-string>"; // MAYBE not a spec violaiton, but ilasm does the "wrong thing," see 10)[] WARNING] by indexing to an empty string instead of null for COM return type
}

// II.22.34
sealed class Property : CodeNode
{
  [OrderedField] public PropertyAttributes Flags;
  [OrderedField] public StringHeapIndex Name;
  [OrderedField] public Signature<PropertySig> Type;

  public override string NodeValue => Type.Value.NamedValue(Name.NodeValue);
}

// II.22.35
sealed class PropertyMap : CodeNode
{
  [OrderedField] public TableIndex<TypeDef> Parent;
  [OrderedField] public UnknownCodedIndex PropertyList; // contiguous run of Properties; continues to last row or the next PropertyList
}

// II.22.36
sealed class StandAloneSig : CodeNode
{
  public override string NodeValue => Signature.NodeValue;
  [OrderedField] public EitherSignature Signature = new EitherSignature("StandAloneMethodSig", CallingConvention.LowerBits.LOCAL_SIG);
}

// II.22.37
sealed class TypeDef : CodeNode
{
  [OrderedField] public TypeAttributes Flags;
  [OrderedField] public StringHeapIndex TypeName;
  [OrderedField] public StringHeapIndex TypeNamespace;
  [OrderedField] public CodedIndex.TypeDefOrRef Extends;
  [OrderedField] public UnknownCodedIndex FieldList; //TODO(link) contiguous run of Fields; continues to last row or the next FieldList
  [OrderedField] public UnknownCodedIndex MethodList; //TODO(link) contiguous run of Methods; continues to last row or the next MethodList
  public override string NodeValue => TypeNamespace.NodeValue != "" ?
    TypeNamespace.NodeValue + "." + TypeName.NodeValue :
    TypeName.NodeValue;
}

// II.22.38
sealed class TypeRef : CodeNode
{
  [OrderedField] public CodedIndex.ResolutionScope ResolutionScope;
  [OrderedField] public StringHeapIndex TypeName;
  [OrderedField] public StringHeapIndex TypeNamespace;

  public override string NodeValue => TypeNamespace.NodeValue != "" ?
    TypeNamespace.NodeValue + "." + TypeName.NodeValue :
    TypeName.NodeValue;
}

// II.22.39
sealed class TypeSpec : CodeNode
{
  public override string NodeValue => Signature.NodeValue;
  [OrderedField] public Signature<TypeSpecSig> Signature;
}
