// Copyright Microsoft, 2017
// Copyright Carl Walsh, 2021

using System;
using System.Linq;

// CodeNode is written though reflection
#pragma warning disable 0649 // CS0649: Field '...' is never assigned to

//TODO(link) Link branch targets
//MAYBE §III.1.7.2 validate branch targets are valid offsets
//TODO(method) §III.1.3 validate stack depth doesn't go negative or violate maxstack §III.1.7.4
//TODO(method) §III.1.5 validate operand type like in
//TODO(method) §III.1.7.4 validate branching stack depth is consistant
//TODO(method) §III.1.7.5 after unconditional branch the stack is assumed to have depth zero
//TODO(method) §III.1.8 validate all sorts of stack type conversions, null type, etc.

// III
sealed class InstructionStream : CodeNode
{
  int length;

  public InstructionStream(int length) {
    this.length = length;
  }

  protected override void InnerRead() {
    while (Bytes.Stream.Position - Start < length) {
      var op = new Op { Bytes = Bytes };
      op.Read();
      op.NodeName = $"Op[{Children.Count}]";
      Children.Add(op);
    }

    Description = string.Join("\n", Children.Take(10).Select(n => n.Description));
  }
}

// III.1.9
sealed class MetadataToken : CodeNode
{
  public UInt24 Offset;
  public byte Table;

  public UserStringHeapIndex Index;

  protected override void InnerRead() {
    AddChild(nameof(Offset));
    AddChild(nameof(Table));

    if (Table == 0x70) {
      Children.Clear();
      // Reposition stream, and read "XX XX 00 70"
      Bytes.Stream.Position -= 4;

      AddChild(nameof(Index));

      if (Bytes.Read<byte>() != 0)
        throw new NotImplementedException("Too big UserStringHeapIndex");
      Index.End++;

      AddChild(nameof(Table));
      Children.Last().Description = "UserStringHeapIndex";

      NodeValue = Index.NodeValue;
    } else {
      var flag = (MetadataTableFlags)(1L << Table);
      Children.Last().Description = flag.ToString();
      var link = Bytes.TildeStream.GetCodeNode(flag, Offset.IntValue - 1); // indexed by 1
      Children.First().Link = link;
      NodeValue = link.NodeValue;
    }
  }
}

sealed class Op : CodeNode
{
  public byte OpCode;

  protected override void InnerRead() {
    AddChild(nameof(OpCode));

    Description = OpCode switch {
      0x00 => "nop",
      0x01 => "break",
      0x02 => "ldarg.0",
      0x03 => "ldarg.1",
      0x04 => "ldarg.2",
      0x05 => "ldarg.3",
      0x06 => "ldloc.0",
      0x07 => "ldloc.1",
      0x08 => "ldloc.2",
      0x09 => "ldloc.3",
      0x0A => "stloc.0",
      0x0B => "stloc.1",
      0x0C => "stloc.2",
      0x0D => "stloc.3",
      0x0E => With<byte>("ldarg.s"),
      0x0F => With<byte>("ldarga.s"),
      0x10 => With<byte>("starg.s"),
      0x11 => With<byte>("ldloc.s"),
      0x12 => With<byte>("ldloca.s"),
      0x13 => With<byte>("stloc.s"),
      0x14 => "ldnull",
      0x15 => "ldc.i4.m1",
      0x16 => "ldc.i4.0",
      0x17 => "ldc.i4.1",
      0x18 => "ldc.i4.2",
      0x19 => "ldc.i4.3",
      0x1A => "ldc.i4.4",
      0x1B => "ldc.i4.5",
      0x1C => "ldc.i4.6",
      0x1D => "ldc.i4.7",
      0x1E => "ldc.i4.8",
      0x1F => With<sbyte>("ldc.i4.s"),
      0x20 => With<int>("ldc.i4"),
      0x21 => With<long>("ldc.i8"),
      0x22 => With<float>("ldc.r4"),
      0x23 => With<double>("ldc.r8"),
      0x25 => "dup",
      0x26 => "pop",
      0x27 => WithToken("jmp"),
      0x28 => WithToken("call"),
      0x29 => WithToken("calli"),
      0x2A => "ret",
      0x2B => With<sbyte>("br.s"),
      0x2C => With<sbyte>("brfalse.s"),
      0x2D => With<sbyte>("brtrue.s"),
      0x2E => With<sbyte>("beq.s"),
      0x2F => With<sbyte>("bge.s"),
      0x30 => With<sbyte>("bgt.s"),
      0x31 => With<sbyte>("ble.s"),
      0x32 => With<sbyte>("blt.s"),
      0x33 => With<sbyte>("bne.un.s"),
      0x34 => With<sbyte>("bge.un.s"),
      0x35 => With<sbyte>("bgt.un.s"),
      0x36 => With<sbyte>("ble.un.s"),
      0x37 => With<sbyte>("blt.un.s"),
      0x38 => With<int>("br"),
      0x39 => With<int>("brfalse"),
      0x3A => With<int>("brtrue"),
      0x3B => With<int>("beq"),
      0x3C => With<int>("bge"),
      0x3D => With<int>("bgt"),
      0x3E => With<int>("ble"),
      0x3F => With<int>("blt"),
      0x40 => With<int>("bne.un"),
      0x41 => With<int>("bge.un"),
      0x42 => With<int>("bgt.un"),
      0x43 => With<int>("ble.un"),
      0x44 => With<int>("blt.un"),
      0x45 => SwitchOp(),
      0x46 => "ldind.i1",
      0x47 => "ldind.u1",
      0x48 => "ldind.i2",
      0x49 => "ldind.u2",
      0x4A => "ldind.i4",
      0x4B => "ldind.u4",
      0x4C => "ldind.i8",
      0x4D => "ldind.i",
      0x4E => "ldind.r4",
      0x4F => "ldind.r8",
      0x50 => "ldind.ref",
      0x51 => "stind.ref",
      0x52 => "stind.i1",
      0x53 => "stind.i2",
      0x54 => "stind.i4",
      0x55 => "stind.i8",
      0x56 => "stind.r4",
      0x57 => "stind.r8",
      0x58 => "add",
      0x59 => "sub",
      0x5A => "mul",
      0x5B => "div",
      0x5C => "div.un",
      0x5D => "rem",
      0x5E => "rem.un",
      0x5F => "and",
      0x60 => "or",
      0x61 => "xor",
      0x62 => "shl",
      0x63 => "shr",
      0x64 => "shr.un",
      0x65 => "neg",
      0x66 => "not",
      0x67 => "conv.i1",
      0x68 => "conv.i2",
      0x69 => "conv.i4",
      0x6A => "conv.i8",
      0x6B => "conv.r4",
      0x6C => "conv.r8",
      0x6D => "conv.u4",
      0x6E => "conv.u8",
      0x6F => WithToken("callvirt"),
      0x70 => WithToken("cpobj"),
      0x71 => WithToken("ldobj"),
      0x72 => WithToken("ldstr"),
      0x73 => WithToken("newobj"),
      0x74 => WithToken("castclass"),
      0x75 => WithToken("isinst"),
      0x76 => "conv.r.un",
      0x79 => WithToken("unbox"),
      0x7A => "throw",
      0x7B => WithToken("ldfld"),
      0x7C => WithToken("ldflda"),
      0x7D => WithToken("stfld"),
      0x7E => WithToken("ldsfld"),
      0x7F => WithToken("ldsflda"),
      0x80 => WithToken("stsfld"),
      0x81 => WithToken("stobj"),
      0x82 => "conv.ovf.i1.un",
      0x83 => "conv.ovf.i2.un",
      0x84 => "conv.ovf.i4.un",
      0x85 => "conv.ovf.i8.un",
      0x86 => "conv.ovf.u1.un",
      0x87 => "conv.ovf.u2.un",
      0x88 => "conv.ovf.u4.un",
      0x89 => "conv.ovf.u8.un",
      0x8A => "conv.ovf.i.un",
      0x8B => "conv.ovf.u.un",
      0x8C => WithToken("box"),
      0x8D => WithToken("newarr"),
      0x8E => "ldlen",
      0x8F => WithToken("ldelema"),
      0x90 => "ldelem.i1",
      0x91 => "ldelem.u1",
      0x92 => "ldelem.i2",
      0x93 => "ldelem.u2",
      0x94 => "ldelem.i4",
      0x95 => "ldelem.u4",
      0x96 => "ldelem.i8",
      0x97 => "ldelem.i",
      0x98 => "ldelem.r4",
      0x99 => "ldelem.r8",
      0x9A => "ldelem.ref",
      0x9B => "stelem.i",
      0x9C => "stelem.i1",
      0x9D => "stelem.i2",
      0x9E => "stelem.i4",
      0x9F => "stelem.i8",
      0xA0 => "stelem.r4",
      0xA1 => "stelem.r8",
      0xA2 => "stelem.ref",
      0xA3 => WithToken("ldelem"),
      0xA4 => WithToken("stelem"),
      0xA5 => WithToken("unbox.any"),
      0xB3 => "conv.ovf.i1",
      0xB4 => "conv.ovf.u1",
      0xB5 => "conv.ovf.i2",
      0xB6 => "conv.ovf.u2",
      0xB7 => "conv.ovf.i4",
      0xB8 => "conv.ovf.u4",
      0xB9 => "conv.ovf.i8",
      0xBA => "conv.ovf.u8",
      0xC2 => WithToken("refanyval"),
      0xC3 => WithToken("ckfinite"),
      0xC6 => WithToken("mkrefany"),
      0xD0 => WithToken("ldtoken"),
      0xD1 => "conv.u2",
      0xD2 => "conv.u1",
      0xD3 => "conv.i",
      0xD4 => "conv.ovf.i",
      0xD5 => "conv.ovf.u",
      0xD6 => "add.ovf",
      0xD7 => "add.ovf.un",
      0xD8 => "mul.ovf",
      0xD9 => "mul.ovf.un",
      0xDA => "sub.ovf",
      0xDB => "sub.ovf.un",
      0xDC => "endfault", //TODO(pedant) endfinally?
      0xDD => With<int>("leave"),
      0xDE => With<sbyte>("leave_s"),
      0xDF => "stind.i",
      0xE0 => "conv.u",
      0xFE => Extended(),
      _ => throw new InvalidOperationException($"unknown op 0x{OpCode:X}"),
    };

    if (Children.Count == 1) {
      NodeValue = Children.Single().NodeValue;
      Children.Clear();
    }
  }

  string Extended() {
    var secondByte = new StructNode<byte> { Bytes = Bytes };
    secondByte.Read();

    var firstNode = Children.Single();
    firstNode.End = secondByte.End;

    firstNode.NodeValue = firstNode.NodeValue + " " + secondByte.NodeValue;

    return secondByte.t switch {
      0x00 => "arglist",
      0x01 => "ceq",
      0x02 => "cgt",
      0x03 => "cgt.un",
      0x04 => "clt",
      0x05 => "clt.un",
      0x06 => WithToken("ldftn"),
      0x07 => WithToken("ldvirtftn"),
      0x09 => With<ushort>("ldarg"),
      0x0A => With<ushort>("ldarga"),
      0x0B => With<ushort>("starg"),
      0x0C => With<ushort>("ldloc"),
      0x0D => With<ushort>("ldloca"),
      0x0E => With<ushort>("stloc"),
      0x0F => "localloc",
      0x11 => "endfilter",
      0x12 => throw new NotImplementedException($"unaligned."),
      0x13 => throw new NotImplementedException($"volatile."),
      0x14 => throw new NotImplementedException($"tail."),
      0x15 => WithToken("initobj"),
      0x16 => WithToken("constrained."),
      0x17 => "cpblk",
      0x18 => "initblk",
      0x19 => throw new NotImplementedException($"no."),
      0x1A => "rethrow",
      0x1C => WithToken("sizeof"),
      0x1D => "refanytype",
      0x1E => throw new NotImplementedException($"readonly."),
      _ => throw new InvalidOperationException($"unkonwn op 0xFE 0x{secondByte:X}"),
    };
  }

  string With<T>(string description) where T : struct {
    Children.Single().Description = description;

    var value = new StructNode<T> { Bytes = Bytes };
    value.Read();
    value.NodeName = "Value";
    Children.Add(value);

    return $"{description} {value.NodeValue}";
  }

  public MetadataToken Token;
  string WithToken(string description) {
    Children.Single().Description = description;
    AddChild(nameof(Token));
    return $"{description} {Token.NodeValue}";
  }

  public uint Count;
  //TODO(link) link each target to op. Using StructNode<uint> keeps each row its own size
  public StructNode<int>[] Targets;
  string SwitchOp() {
    Children.Single().Description = "switch";
    AddChild(nameof(Count));
    AddChild(nameof(Targets)); 
    return $"switch ({string.Join(", ", Targets.Select(n => n.t))})";
  }

  protected override int GetCount(string field) => field switch {
    nameof(Targets) => (int)Count,
    _ => base.GetCount(field),
  };
}
