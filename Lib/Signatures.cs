﻿using System;
using System.Collections.Generic;
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
    var unsigned = Bytes.ReadClass<UnsignedCompressed>();
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

sealed class Signature<T> : SizedSignature<short, T> where T : CodeNode, new()
{
}

abstract class SizedSignature<Ti, Ts> : CodeNode where Ti : struct where Ts : CodeNode, new()
{
  // Same shape as BlobHeapIndex but allow for custom types in the blob heap
  public Ti Index;

  Lazy<Ts> link;
  public SizedSignature() {
    link = new Lazy<Ts>(() => Bytes.BlobHeap.GetCustom<Ts>(Index.GetInt32()));
  }

  public Ts Value => link.Value;
  public override CodeNode Link => Value;
  public override string NodeValue => Value.NodeValue;

  // TODO calling-convention byte for most signatures?

  protected override void InnerRead() {
    Index = Bytes.Read<Ti>();
    // Actually read the sig from the blob heap later, AFTER all tilde metadata tables are read
  }
}

// II.23.2.1
// sealed class MethodDefSig : CodeNode { }
// II.23.2.2
// sealed class MethodRefSig : CodeNode { }
// II.23.2.3
// sealed class StandAloneMethodSig : CodeNode { }

// II.23.2.4
sealed class FieldSig : CodeNode
{
  [Expected(0x6)]
  public byte FIELD;
  public CustomMods CustomMods;
  public TypeSig Type;

  public override string NodeValue => $"{Type.NodeValue} {CustomMods.NodeValue}".Trim();

  protected override void InnerRead() {
    AddChild(nameof(FIELD));

    AddChild(nameof(CustomMods));
    ResizeLastChild();

    AddChild(nameof(Type));
  }
}

// II.23.2.5
// sealed class PropertySig : CodeNode { }

// II.23.2.6
sealed class LocalVarSig : CodeNode
{
  [Expected(0x7)]
  public byte LocalSig;
  [OrderedField]
  public UnsignedCompressed Count;
  [OrderedField]
  public LocalVar[] Types;

  public override string NodeValue => string.Join(", ", Types.Select(t => t.NodeValue));

  protected override int GetCount(string field) => field switch {
    nameof(Types) => (int)Count.Value,
    _ => base.GetCount(field),
  };

  public sealed class LocalVar : CodeNode
  {
    public CustomMods CustomMods;
    public ElementType Constraint; // II.23.2.9
    public ElementType ByRef;
    public TypeSig Type;

    string value;

    public override string NodeValue {
      get {
        if (value != null) return value;
        var parts = new[] {
          Type.NodeValue,
          ByRef != default ? "&" : "",
          Constraint != default ? Constraint.S() : "",
          CustomMods.NodeValue,
        };
        return string.Join(" ", parts.Where(s => !string.IsNullOrEmpty(s))).Replace(" &", "&");
      }
    }

    protected override void InnerRead() {
      if (Bytes.Peek<ElementType>() == ElementType.TypedByRef) {
        var typedByRef = Bytes.Read<ElementType>();
        value = typedByRef.S();
        return;
      }

      AddChild(nameof(CustomMods));
      ResizeLastChild();

      if (Bytes.Peek<ElementType>() == ElementType.Pinned) {
        AddChild(nameof(Constraint));
      }

      if (Bytes.Peek<ElementType>() == ElementType.ByRef) {
        AddChild(nameof(ByRef));
      }
      AddChild(nameof(Type));

      if (Children.Count == 1) {
        Children = Type.Children;
      }
    }
  }
}

// II.23.2.7
sealed class CustomMod : CodeNode
{
  public ElementType OptOrReq;
  public TypeDefOrRefOrSpecEncoded Token;

  protected override void InnerRead() {
    AddChild(nameof(OptOrReq));
    AddChild(nameof(Token));

    string name;
    switch (OptOrReq) {
      case ElementType.CModOpt:
        name = "modopt";
        break;
      case ElementType.CModReqd:
        name = "modreq";
        break;
      default:
        name = "!!missing";
        Errors.Add("OptOrReq must be CModOpt or CModReqd");
        break;
    };
    NodeValue = $"{name} ({Token.NodeValue})";
  }
}

sealed class CustomMods : CodeNode
{
  protected override void InnerRead() {
    while (true) {
      var b = Bytes.Peek<ElementType>();
      if (b != ElementType.CModOpt && b != ElementType.CModReqd) break;
      var mod = Bytes.ReadClass<CustomMod>();
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
    var encoded = Bytes.ReadClass<UnsignedCompressed>();

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

// II.23.2.10
// sealed class ParamSig : CodeNode { }
// II.23.2.11
// sealed class RetType : CodeNode { }

// II.23.2.12
sealed class TypeSig : CodeNode
{
  public CustomMods PrefixCustomMods;
  // TODO(SpecViolation) CModOpt shouldn't be allowed at start of TypeSpec, but found from i.e. `object modopt ([mscorlib]System.Text.StringBuilder)`
  public ElementType Type;

  public TypeDefOrRefOrSpecEncoded TypeEncoded;
  public UnsignedCompressed VarNumber;
  public TypeSig PtrType;
  public ElementType PtrVoid;

  protected override void InnerRead() {
    AddChild(nameof(PrefixCustomMods));
    ResizeLastChild();
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
        if (Children.Count == 1) Children.Clear();
        SetNodeValue(Type.S());
        return;
      case ElementType.Array:
        ReadArray();
        return;
      case ElementType.Class:
      case ElementType.ValueType:
        AddChild(nameof(TypeEncoded));
        SetNodeValue(Type.S(), TypeEncoded.NodeValue);
        return;
      case ElementType.Fnptr:
        throw new NotImplementedException("Fnptr"); // MethodDefSig | MethodRefSig
      case ElementType.GenericInst:
        ReadGeneric();
        return;
      case ElementType.MVar:
        AddChild(nameof(VarNumber));
        SetNodeValue($"!!{VarNumber.Value}");
        return;
      case ElementType.Ptr:
        var ptrVoidPeek = Bytes.Peek<ElementType>();
        if (ptrVoidPeek == ElementType.Void) {
          AddChild(nameof(PtrVoid));
          SetNodeValue("void*");
        } else {
          AddChild(nameof(PtrType));
          SetNodeValue($"{PtrType.NodeValue}*");
        }
        return;
      case ElementType.SzArray:
        ReadSzArray();
        return;
      case ElementType.Var:
        AddChild(nameof(VarNumber));
        SetNodeValue($"!{VarNumber.Value}");
        return;
      default:
        throw new InvalidOperationException(Type.GetString());
    }
  }

  void SetNodeValue(params string[] parts) {
    parts = parts.Concat(new[] { PrefixCustomMods.NodeValue }).ToArray();
    NodeValue = string.Join(" ", parts.Where(s => !string.IsNullOrEmpty(s)));
  }

  public TypeSig ArrayElementType;
  public ArrayShape ArrayShape;
  void ReadArray() {
    AddChild(nameof(ArrayElementType));
    AddChild(nameof(ArrayShape));

    SetNodeValue($"{ArrayElementType.NodeValue}[{ArrayShape.NodeValue}]");
  }

  public CustomMods SzArrayCustomMods;
  public ElementType SzArrayElementType;
  void ReadSzArray() {
    AddChild(nameof(SzArrayCustomMods));
    ResizeLastChild();
    AddChild(nameof(SzArrayElementType));

    if (!string.IsNullOrEmpty(SzArrayCustomMods.NodeValue)) {
      SetNodeValue(SzArrayElementType.S(), SzArrayCustomMods.NodeValue + "[]");
    } else {
      SetNodeValue(SzArrayElementType.S() + "[]");
    }
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

    SetNodeValue("Generic", GenKind.S(), $"{GenType.NodeValue}<{string.Join(", ", GenArgTypes.Select(t => t.NodeValue))}>");
  }
}

// II.23.2.13
sealed class ArrayShape : CodeNode
{
  public UnsignedCompressed Rank;
  public UnsignedCompressed NumSizes;
  public UnsignedCompressed[] Size;
  public UnsignedCompressed NumLoBounds;
  public SignedCompressed[] LoBound;

  protected override void InnerRead() {
    AddChild(nameof(Rank));
    AddChild(nameof(NumSizes));
    AddChildren(nameof(Size), (int)NumSizes.Value);
    AddChild(nameof(NumLoBounds));
    AddChildren(nameof(LoBound), (int)NumLoBounds.Value);

    var text = new List<string>();
    for (int i = 0; i < Rank.Value; ++i) {
      int lower = i < NumLoBounds.Value ? LoBound[i].Value : 0;
      uint size = i < NumSizes.Value ? Size[i].Value : 0;

      if (lower == 0) {
        if (size == 0) {
          text.Add("");
        } else {
          text.Add($"{size}");
        }
      } else {
        if (size == 0) {
          text.Add($"{lower}...");
        } else {
          text.Add($"{lower}...{lower + size - 1}");
        }
      }
    }

    NodeValue = string.Join(",", text);
  }
}

// II.23.2.14
sealed class TypeSpecSig : CodeNode
{
  public TypeSig TypeSig;

  protected override void InnerRead() {
    // Since TypeSpec is a subset of Type, don't reimplement it!
    AddChild(nameof(TypeSig));
    Children.Clear();
    Children.AddRange(TypeSig.Children);

    NodeValue = TypeSig.NodeValue;
    Description = TypeSig.Description;

    switch (TypeSig.Type) {
      case ElementType.GenericInst:
        if (TypeSig.GenArgCount.Value == 0) {
          Errors.Add("TypeSpec GenArgCount should be non-zero");
        }
        break;
        // case ElementType.Object:
        // case ElementType.Int8:
        // case ElementType.Class:
        // case ElementType.MVar:
        //TODO(SpecViolation) i.e. Object, Int8, Class, MVar, etc. aren't allowed in TypeSpec, but assembling i.e. `modreq (object)` creates a TypeSpec for Object
        //   throw new InvalidOperationException(TypeSig.Type.ToString());
    }
  }
}

// II.23.2.15
sealed class MethodSpecSig : CodeNode
{
  [Expected(0x0A)]
  public byte GenericInst;
  [OrderedField]
  public UnsignedCompressed GenArgCount;
  [OrderedField]
  public TypeSig[] Types;

  public override string NodeValue => $"<{string.Join(", ", Types.Select(t => t.NodeValue))}>";

  protected override int GetCount(string field) => field switch {
    nameof(Types) => (int)GenArgCount.Value,
    _ => base.GetCount(field),
  };
}

// II.23.2.16
// MAYBE forced Short form signatures:
//   No ElementType.Class then TypeRef of System.String or System.Object
//   No ElementType.ValueType then TypeRef to any primitibe already in ElementType or TypedByRef
