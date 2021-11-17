// Copyright Microsoft, 2017
// Copyright Carl Walsh, 2021

using System;
using System.Linq;

//TODO(link) Link branch targets
//TODO(HACK)? §III.1.7.2 validate branch targets are valid offsets
//TODO(method) §III.1.3 validate stack depth doesn't go negative or violate maxstack §III.1.7.4
//TODO(method) §III.1.5 validate operand type like in
//TODO(method) §III.1.7.4 validate branching stack depth is consistant
//TODO(method) §III.1.7.5 after unconditional branch the stack is assumed to have depth zero
//TODO(method) §III.1.8 validate all sorts of stack type conversions, null type, etc.

// III
sealed class InstructionStream : MyCodeNode
{
  int length;

  public InstructionStream(int length) {
    this.length = length;
  }

  public override void Read() {
    MarkStarting();

    while (Bytes.Stream.Position - Start < length) {
      var op = new Op { Bytes = Bytes };
      op.Read();
      op.NodeName = $"Op[{Children.Count}]";
      Children.Add(op);
    }

    Description = string.Join("\n", Children.Select(n => n.Description));

    MarkEnding();
  }
}

// III.1.9
sealed class MetadataToken : MyCodeNode
{
  public UInt24 Offset;
  public byte Table;

  public UserStringHeapIndex Index;

  public override void Read() {
    AddChild(nameof(Offset));
    AddChild(nameof(Table));

    if (Table == 0x70) {
      Children.Clear();
      // Reposition stream, and read "XX XX 00 70"
      var stream = Bytes.Stream;
      stream.Position -= 4;

      AddChild(nameof(Index));

      if (stream.ReallyReadByte() != 0)
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

sealed class Op : MyCodeNode
{
  public byte OpCode;

  public override void Read() {
    MarkStarting();
    AddChild(nameof(OpCode));

    Description = OpCode switch {
      0x00 => Solo("nop"),
      0x01 => Solo("break"),
      0x02 => Solo("ldarg.0"),
      0x03 => Solo("ldarg.1"),
      0x04 => Solo("ldarg.2"),
      0x05 => Solo("ldarg.3"),
      0x06 => Solo("ldloc.0"),
      0x07 => Solo("ldloc.1"),
      0x08 => Solo("ldloc.2"),
      0x09 => Solo("ldloc.3"),
      0x0A => Solo("stloc.0"),
      0x0B => Solo("stloc.1"),
      0x0C => Solo("stloc.2"),
      0x0D => Solo("stloc.3"),
      0x0E => With<byte>("ldarg.s"),
      0x0F => With<byte>("ldarga.s"),
      0x10 => With<byte>("starg.s"),
      0x11 => With<byte>("ldloc.s"),
      0x12 => With<byte>("ldloca.s"),
      0x13 => With<byte>("stloc.s"),
      0x14 => Solo("ldnull"),
      0x15 => Solo("ldc.i4.m1"),
      0x16 => Solo("ldc.i4.0"),
      0x17 => Solo("ldc.i4.1"),
      0x18 => Solo("ldc.i4.2"),
      0x19 => Solo("ldc.i4.3"),
      0x1A => Solo("ldc.i4.4"),
      0x1B => Solo("ldc.i4.5"),
      0x1C => Solo("ldc.i4.6"),
      0x1D => Solo("ldc.i4.7"),
      0x1E => Solo("ldc.i4.8"),
      0x1F => With<sbyte>("ldc.i4.s"),
      0x20 => With<int>("ldc.i4"),
      0x21 => With<long>("ldc.i8"),
      0x22 => With<float>("ldc.r4"),
      0x23 => With<double>("ldc.r8"),
      0x25 => Solo("dup"),
      0x26 => Solo("pop"),
      0x27 => WithToken("jmp"),
      0x28 => WithToken("call"),
      0x29 => WithToken("calli"),
      0x2A => Solo("ret"),
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
      0x46 => Solo("ldind.i1"),
      0x47 => Solo("ldind.u1"),
      0x48 => Solo("ldind.i2"),
      0x49 => Solo("ldind.u2"),
      0x4A => Solo("ldind.i4"),
      0x4B => Solo("ldind.u4"),
      0x4C => Solo("ldind.i8"),
      0x4D => Solo("ldind.i"),
      0x4E => Solo("ldind.r4"),
      0x4F => Solo("ldind.r8"),
      0x50 => Solo("ldind.ref"),
      0x51 => Solo("stind.ref"),
      0x52 => Solo("stind.i1"),
      0x53 => Solo("stind.i2"),
      0x54 => Solo("stind.i4"),
      0x55 => Solo("stind.i8"),
      0x56 => Solo("stind.r4"),
      0x57 => Solo("stind.r8"),
      0x58 => Solo("add"),
      0x59 => Solo("sub"),
      0x5A => Solo("mul"),
      0x5B => Solo("div"),
      0x5C => Solo("div.un"),
      0x5D => Solo("rem"),
      0x5E => Solo("rem.un"),
      0x5F => Solo("and"),
      0x60 => Solo("or"),
      0x61 => Solo("xor"),
      0x62 => Solo("shl"),
      0x63 => Solo("shr"),
      0x64 => Solo("shr.un"),
      0x65 => Solo("neg"),
      0x66 => Solo("not"),
      0x67 => Solo("conv.i1"),
      0x68 => Solo("conv.i2"),
      0x69 => Solo("conv.i4"),
      0x6A => Solo("conv.i8"),
      0x6B => Solo("conv.r4"),
      0x6C => Solo("conv.r8"),
      0x6D => Solo("conv.u4"),
      0x6E => Solo("conv.u8"),
      0x6F => WithToken("callvirt"),
      0x70 => WithToken("cpobj"),
      0x71 => WithToken("ldobj"),
      0x72 => WithToken("ldstr"),
      0x73 => WithToken("newobj"),
      0x74 => WithToken("castclass"),
      0x75 => WithToken("isinst"),
      0x76 => Solo("conv.r.un"),
      0x79 => WithToken("unbox"),
      0x7A => Solo("throw"),
      0x7B => WithToken("ldfld"),
      0x7C => WithToken("ldflda"),
      0x7D => WithToken("stfld"),
      0x7E => WithToken("ldsfld"),
      0x7F => WithToken("ldsflda"),
      0x80 => WithToken("stsfld"),
      0x81 => WithToken("stobj"),
      0x82 => Solo("conv.ovf.i1.un"),
      0x83 => Solo("conv.ovf.i2.un"),
      0x84 => Solo("conv.ovf.i4.un"),
      0x85 => Solo("conv.ovf.i8.un"),
      0x86 => Solo("conv.ovf.u1.un"),
      0x87 => Solo("conv.ovf.u2.un"),
      0x88 => Solo("conv.ovf.u4.un"),
      0x89 => Solo("conv.ovf.u8.un"),
      0x8A => Solo("conv.ovf.i.un"),
      0x8B => Solo("conv.ovf.u.un"),
      0x8C => WithToken("box"),
      0x8D => WithToken("newarr"),
      0x8E => Solo("ldlen"),
      0x8F => WithToken("ldelema"),
      0x90 => Solo("ldelem.i1"),
      0x91 => Solo("ldelem.u1"),
      0x92 => Solo("ldelem.i2"),
      0x93 => Solo("ldelem.u2"),
      0x94 => Solo("ldelem.i4"),
      0x95 => Solo("ldelem.u4"),
      0x96 => Solo("ldelem.i8"),
      0x97 => Solo("ldelem.i"),
      0x98 => Solo("ldelem.r4"),
      0x99 => Solo("ldelem.r8"),
      0x9A => Solo("ldelem.ref"),
      0x9B => Solo("stelem.i"),
      0x9C => Solo("stelem.i1"),
      0x9D => Solo("stelem.i2"),
      0x9E => Solo("stelem.i4"),
      0x9F => Solo("stelem.i8"),
      0xA0 => Solo("stelem.r4"),
      0xA1 => Solo("stelem.r8"),
      0xA2 => Solo("stelem.ref"),
      0xA3 => WithToken("ldelem"),
      0xA4 => WithToken("stelem"),
      0xA5 => WithToken("unbox.any"),
      0xB3 => Solo("conv.ovf.i1"),
      0xB4 => Solo("conv.ovf.u1"),
      0xB5 => Solo("conv.ovf.i2"),
      0xB6 => Solo("conv.ovf.u2"),
      0xB7 => Solo("conv.ovf.i4"),
      0xB8 => Solo("conv.ovf.u4"),
      0xB9 => Solo("conv.ovf.i8"),
      0xBA => Solo("conv.ovf.u8"),
      0xC2 => WithToken("refanyval"),
      0xC3 => WithToken("ckfinite"),
      0xC6 => WithToken("mkrefany"),
      0xD0 => WithToken("ldtoken"),
      0xD1 => Solo("conv.u2"),
      0xD2 => Solo("conv.u1"),
      0xD3 => Solo("conv.i"),
      0xD4 => Solo("conv.ovf.i"),
      0xD5 => Solo("conv.ovf.u"),
      0xD6 => Solo("add.ovf"),
      0xD7 => Solo("add.ovf.un"),
      0xD8 => Solo("mul.ovf"),
      0xD9 => Solo("mul.ovf.un"),
      0xDA => Solo("sub.ovf"),
      0xDB => Solo("sub.ovf.un"),
      0xDC => Solo("endfault"), //TODO(pedant) endfinally?
      0xDD => With<int>("leave"),
      0xDE => With<sbyte>("leave_s"),
      0xDF => Solo("stind.i"),
      0xE0 => Solo("conv.u"),
      0xFE => Extended(),
      _ => throw new InvalidOperationException($"unknown op 0x{OpCode:X}"),
    };
    MarkEnding();
  }

  string Extended() {
    var secondByte = new MyStructNode<byte> { Bytes = Bytes };
    secondByte.Read();

    var firstNode = Children.Single();
    firstNode.End = secondByte.End;

    firstNode.NodeValue = firstNode.NodeValue + " " + secondByte.NodeValue;

    return secondByte.t switch {
      0x00 => Solo("arglist"),
      0x01 => Solo("ceq"),
      0x02 => Solo("cgt"),
      0x03 => Solo("cgt.un"),
      0x04 => Solo("clt"),
      0x05 => Solo("clt.un"),
      0x06 => WithToken("ldftn"),
      0x07 => WithToken("ldvirtftn"),
      0x09 => With<ushort>("ldarg"),
      0x0A => With<ushort>("ldarga"),
      0x0B => With<ushort>("starg"),
      0x0C => With<ushort>("ldloc"),
      0x0D => With<ushort>("ldloca"),
      0x0E => With<ushort>("stloc"),
      0x0F => Solo("localloc"),
      0x11 => Solo("endfilter"),
      0x12 => throw new NotImplementedException($"unaligned."),
      0x13 => throw new NotImplementedException($"volatile."),
      0x14 => throw new NotImplementedException($"tail."),
      0x15 => WithToken("initobj"),
      0x16 => WithToken("constrained."),
      0x17 => Solo("cpblk"),
      0x18 => Solo("initblk"),
      0x19 => throw new NotImplementedException($"no."),
      0x1A => Solo("rethrow"),
      0x1C => WithToken("sizeof"),
      0x1D => Solo("refanytype"),
      0x1E => throw new NotImplementedException($"readonly."),
      _ => throw new InvalidOperationException($"unkonwn op 0xFE 0x{secondByte:X}"),
    };
  }

  string Solo(string description) {
    Children.Single().Description = description;
    return description;
  }

  string With<T>(string description) where T : struct {
    // Children.Single().Description = description; // TODO(solonode) better diff

    var value = new MyStructNode<T> { Bytes = Bytes };
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
  public int[] Targets;
  string SwitchOp() {
    Children.Single().Description = "switch";
    AddChild(nameof(Count));
    AddChildren(nameof(Targets), (int)Count); //TODO(links) switch offset
    return $"switch ({string.Join(", ", Targets)})";
  }
}
