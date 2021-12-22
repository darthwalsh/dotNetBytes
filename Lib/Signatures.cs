using System;
using System.IO;
using System.Linq;

// CodeNode is written though reflection
#pragma warning disable 0649 // CS0649: Field '...' is never assigned to

// II.23.2 Blobs and signatures
// MAYBE move this into Lib/MetadataStructs

sealed class UnsignedCompressed : CodeNode
{
  public uint Value { get; private set; }

  public override string NodeValue => Value.GetString();

  // Number is compressed into one of these byte formats, where x is a bit of the number
  //  0xxxxxxx
  //  10xxxxxx xxxxxxxx
  //  110xxxxx xxxxxxxx xxxxxxxx xxxxxxxx
  protected override void InnerRead() {
    var first = Bytes.Read<byte>();
    switch (GetWidth(first)) {
      case 1:
        Value = first;
        break;
      case 2:
        var second = Bytes.Read<byte>();
        Value = (uint)(((first & ~0xC0) << 8) + second);
        break;
      case 4:
        second = Bytes.Read<byte>();
        var third = Bytes.Read<byte>();
        var fourth = Bytes.Read<byte>();
        Value = (uint)(((first & ~0xC0) << 24) + (second << 16) + (third << 8) + fourth);
        break;
      default:
        throw new InvalidDataException();
    }
  }

  static int GetWidth(byte b) {
    switch (b & 0xE0) {
      case 0x00:
      case 0x20:
      case 0x40:
      case 0x60:
        return 1;
      case 0x80:
      case 0xA0:
        return 2;
      case 0xC0:
        return 4;
      case 0xE0:
        throw new InvalidOperationException("Not expecting null string!");
      default:
        throw new InvalidOperationException();
    }
  }
}

sealed class SignedCompressed : CodeNode
{
  public int Value { get; private set; }

  public override string NodeValue => Value.GetString();

  // Number is compressed into one of these byte formats, where x is a bit of the number
  //  0xxxxxxx
  //  10xxxxxx xxxxxxxx
  //  110xxxxx xxxxxxxx xxxxxxxx xxxxxxxx
  protected override void InnerRead() {
    var unsigned = new UnsignedCompressed { Bytes = Bytes };
    unsigned.Read();

    var sum = (int)unsigned.Value;

    if (sum % 2 == 0) {
      Value = sum >> 1;
      return;
    }

    int negativeMask = (unsigned.End - unsigned.Start) switch {
      1 => unchecked((int)0xFFFFFFC0),
      2 => unchecked((int)0xFFFFE000),
      4 => unchecked((int)0xF0000000),
      _ => throw new InvalidOperationException()
    };

    Value = negativeMask | (sum >> 1);
  }
}

sealed class Signature<T> : CodeNode where T : CodeNode, new()
{
  // Same shape as BlobHeapIndex but allws for custom types in the blob heap
  public short Index;

  // TODO calling-convention byte for most signatures?

  protected override void InnerRead() {
    Index = Bytes.Read<short>();

    Bytes.BlobHeap.GetCustom<T>(Index);
    Link = Bytes.BlobHeap.GetNode(Index).Children.Last();
    NodeValue = Link.NodeValue;
  }
}

// II.23.2.1
// sealed class MethodDefSig : CodeNode { }
// II.23.2.2
// sealed class MethodRefSig : CodeNode { }
// II.23.2.3
// sealed class StandAloneMethodSig : CodeNode { }
// II.23.2.4
// sealed class FieldSig : CodeNode { }
// II.23.2.5
// sealed class PropertySig : CodeNode { }
// II.23.2.6
// sealed class LocalVarSig : CodeNode { }

// II.23.2.7
sealed class CustomMod : CodeNode {
  public ElementType OptOrReq;
  public TypeDefOrRefOrSpecEncoded Token;

  protected override void InnerRead() {
    AddChild(nameof(OptOrReq));
    if (OptOrReq != ElementType.CModOpt && OptOrReq != ElementType.CModReqd) {
      Errors.Add("OptOrReq must be CModOpt or CModReqd");
    }
    AddChild(nameof(Token));

    var name = OptOrReq switch { 
      ElementType.CModOpt => "modopt",
      ElementType.CModReqd => "modreq",
      _ => throw new InvalidOperationException()
    };
    NodeValue = $"{name} ({Token.NodeValue})";
  }
}

sealed class CustomMods : CodeNode {
  protected override void InnerRead() {
    while (true) {
      var b = Bytes.Read<ElementType>();
      --Bytes.Stream.Position;
      if (b != ElementType.CModOpt && b != ElementType.CModReqd) break;
      var mod = new CustomMod { Bytes = Bytes };
      mod.Read();
      mod.NodeName = $"{nameof(CustomMods)}[{Children.Count}]";
      Children.Add(mod);
    }

    NodeValue = string.Join(" ", Enumerable.Reverse(Children).Select(c => c.NodeValue));
  }
}

// II.23.2.8
sealed class TypeDefOrRefOrSpecEncoded : CodeNode
{
  protected override void InnerRead() {
    var encoded = new UnsignedCompressed { Bytes = Bytes };
    encoded.Read();

    var tag = encoded.Value & 0b11;
    var index = (encoded.Value >> 2) - 1;
    Link = tag switch {
      0b00 => Bytes.TildeStream.TypeDefs[index],
      0b01 => Bytes.TildeStream.TypeRefs[index],
      0b10 => Bytes.TildeStream.TypeSpecs[index],
      _ => throw new InvalidOperationException(),
    };

    NodeValue = Link.NodeValue;
    Description = $"Compressed {encoded.Value} has lower 2 bits {tag} specify {Link.GetType().Name}, with row {index}";
  }
}

// II.23.2.9
// sealed class Constraint : CodeNode { }
// II.23.2.10
// sealed class ParamSig : CodeNode { }
// II.23.2.11
// sealed class RetType : CodeNode { }

// II.23.2.12
sealed class TypeSig : CodeNode
{
  public ElementType Type;

  public UnsignedCompressed VarNumber;

  protected override void InnerRead() {
    AddChild(nameof(Type));

    switch (Type) {
      case ElementType.Boolean:
      case ElementType.Char:
      case ElementType.Int1:
      case ElementType.UInt1:
      case ElementType.Int2:
      case ElementType.UInt2:
      case ElementType.Int4:
      case ElementType.UInt4:
      case ElementType.Int8:
      case ElementType.UInt8:
      case ElementType.Real4:
      case ElementType.Real8:
      case ElementType.IntPtr:
      case ElementType.UIntPtr:
      case ElementType.Object:
      case ElementType.String:
        NodeValue = Type.ToString();
        return;
      // case ElementType.ARRAY: Type ArrayShape(general array, see §II.23.2.13)
      // case ElementType.CLASS: TypeDefOrRefOrSpecEncoded
      // case ElementType.FNPTR: MethodDefSig
      // case ElementType.FNPTR: MethodRefSig
      // case ElementType.GENERICINST: (CLASS | VALUETYPE:) TypeDefOrRefOrSpecEncoded GenArgCount Type*
      case ElementType.MVar:
        AddChild(nameof(VarNumber));
        NodeValue = $"!{VarNumber.Value}";
        return;
      // case ElementType.PTR: CustomMod* Type
      // case ElementType.PTR: CustomMod* VOID
      // case ElementType.SZARRAY: CustomMod* Type(single dimensional, zero-based array i.e., vector)
      // case ElementType.VALUETYPE: TypeDefOrRefOrSpecEncoded
      case ElementType.Var:
        AddChild(nameof(VarNumber));
        NodeValue = $"!!{VarNumber.Value}";
        return;
      default:
        throw new NotImplementedException(Type.GetString());
    }
  }
}

// II.23.2.13
// sealed class ArrayShape : CodeNode { }

// II.23.2.14
sealed class TypeSpecSig : CodeNode
{
  public ElementType Type;

  protected override void InnerRead() {
    AddChild(nameof(Type));

    switch (Type) {
      case ElementType.Ptr:
        throw new NotImplementedException("Ptr");
      case ElementType.Fnptr:
        throw new NotImplementedException("Fnptr");
      case ElementType.Array:
        throw new NotImplementedException("Array");
      case ElementType.SzArray:
        ReadSzArray();
        return;
      case ElementType.GenericInst:
        ReadGeneric();
        break;
      case ElementType.Boolean:
      case ElementType.Char:
      case ElementType.Int1:
      case ElementType.UInt1:
      case ElementType.Int2:
      case ElementType.UInt2:
      case ElementType.Int4:
      case ElementType.UInt4:
      case ElementType.Int8:
      case ElementType.UInt8:
      case ElementType.Real4:
      case ElementType.Real8:
      case ElementType.IntPtr:
      case ElementType.UIntPtr:
      case ElementType.Object:
      case ElementType.String:
        // According to the spec these values aren't allowed in TypeSpec, but assembling i.e. `modreq (object)` creates a TypeSpec for Object
        NodeValue = Type.ToString();
        return; 
      default:
        throw new InvalidOperationException(Type.ToString());
    }
  }

  public CustomMods CustomMods;
  public ElementType ElementType;
  void ReadSzArray() {
    AddChild(nameof(CustomMods));
    if (!CustomMods.Children.Any()) { Children.Remove(CustomMods); } // TODO pattern for this?
    AddChild(nameof(ElementType));

    NodeValue =  $"{CustomMods.NodeValue} {ElementType}[]".Trim();
  }

  public ElementType GenKind;
  public TypeDefOrRefOrSpecEncoded GenType;
  public UnsignedCompressed GenArgCount;
  public TypeSig[] GenArgTypes;
  void ReadGeneric() {
    AddChild(nameof(GenKind));
    if (GenKind != ElementType.ValueType && GenKind != ElementType.Class) {
      Errors.Add("GenKind must be ValueType or Class");
    }
    AddChild(nameof(GenType));
    AddChild(nameof(GenArgCount));
    AddChildren(nameof(GenArgTypes), (int)GenArgCount.Value);

    NodeValue = $"Generic {GenKind} {GenType.NodeValue}<{string.Join(", ", GenArgTypes.Select(t => t.NodeValue))}>";
  }
}

// II.23.2.15
// sealed class MethodSpecSig : CodeNode { }

// II.23.2.16
// MAYBE forced Short form signatures:
//   No ElementType.Class then TypeRef of System.String or System.Object
//   No ElementType.ValueType then TypeRef to any primitibe already in ElementType or TypedByRef
