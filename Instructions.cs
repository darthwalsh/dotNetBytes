// Copyright Microsoft, 2017

using System;
using System.Collections.Generic;
using System.IO;

//TODO(link) Link branch targets
//TODO(HACK)? §III.1.7.2 validate branch targets are valid offsets
//TODO(method) §III.1.3 validate stack depth doesn't go negative or violate maxstack §III.1.7.4
//TODO(method) §III.1.5 validate operand type like in
//TODO(method) §III.1.7.4 validate branching stack depth is consistant
//TODO(method) §III.1.7.5 after unconditional branch the stack is assumed to have depth zero
//TODO(method) §III.1.8 validate all sorts of stack type conversions, null type, etc.

// III
sealed class InstructionStream : ICanRead
{
  List<object> instructions = new List<object>();
  int length;
  int opCount = 0;

  public InstructionStream(int length) {
    this.length = length;
  }

  public CodeNode Read(Stream stream) {
    var readUntil = stream.Position + length;

    var node = new CodeNode();

    while (stream.Position < readUntil) {
      var op = GetOp(stream);
      instructions.Add(op);
      var opNode = op.Read(stream);
      opNode.Name = $"Op[{opCount++}]";
      node.Add(opNode);
    }

    return node;
  }

  ICanRead GetOp(Stream stream) {
    byte firstByte;
    var opNode = stream.ReadStruct(out firstByte, "OpCode");

    switch (firstByte) {
      case 0x00:
        return new Op(opNode, "nop");
      case 0x01:
        return new Op(opNode, "break");
      case 0x02:
        return new Op(opNode, "ldarg.0");
      case 0x03:
        return new Op(opNode, "ldarg.1");
      case 0x04:
        return new Op(opNode, "ldarg.2");
      case 0x05:
        return new Op(opNode, "ldarg.3");
      case 0x06:
        return new Op(opNode, "ldloc.0");
      case 0x07:
        return new Op(opNode, "ldloc.1");
      case 0x08:
        return new Op(opNode, "ldloc.2");
      case 0x09:
        return new Op(opNode, "ldloc.3");
      case 0x0A:
        return new Op(opNode, "stloc.0");
      case 0x0B:
        return new Op(opNode, "stloc.1");
      case 0x0C:
        return new Op(opNode, "stloc.2");
      case 0x0D:
        return new Op(opNode, "stloc.3");
      case 0x0E:
        return new OpWith<byte>(opNode, "ldarg.s");
      case 0x0F:
        return new OpWith<byte>(opNode, "ldarga.s");
      case 0x10:
        return new OpWith<byte>(opNode, "starg.s");
      case 0x11:
        return new OpWith<byte>(opNode, "ldloc.s");
      case 0x12:
        return new OpWith<byte>(opNode, "ldloca.s");
      case 0x13:
        return new OpWith<byte>(opNode, "stloc.s");
      case 0x14:
        return new Op(opNode, "ldnull");
      case 0x15:
        return new Op(opNode, "ldc.i4.m1");
      case 0x16:
        return new Op(opNode, "ldc.i4.0");
      case 0x17:
        return new Op(opNode, "ldc.i4.1");
      case 0x18:
        return new Op(opNode, "ldc.i4.2");
      case 0x19:
        return new Op(opNode, "ldc.i4.3");
      case 0x1A:
        return new Op(opNode, "ldc.i4.4");
      case 0x1B:
        return new Op(opNode, "ldc.i4.5");
      case 0x1C:
        return new Op(opNode, "ldc.i4.6");
      case 0x1D:
        return new Op(opNode, "ldc.i4.7");
      case 0x1E:
        return new Op(opNode, "ldc.i4.8");
      case 0x1F:
        return new OpWith<sbyte>(opNode, "ldc.i4.s");
      case 0x20:
        return new OpWith<int>(opNode, "ldc.i4");
      case 0x21:
        return new OpWith<long>(opNode, "ldc.i8");
      case 0x22:
        return new OpWith<float>(opNode, "ldc.r4");
      case 0x23:
        return new OpWith<double>(opNode, "ldc.r8");
      case 0x25:
        return new Op(opNode, "dup");
      case 0x26:
        return new Op(opNode, "pop");
      case 0x27:
        return new OpWithToken(opNode, "jmp");
      case 0x28:
        return new OpWithToken(opNode, "call");
      case 0x29:
        return new OpWithToken(opNode, "calli");
      case 0x2A:
        return new Op(opNode, "ret");
      case 0x2B:
        return new OpWith<sbyte>(opNode, "br.s");
      case 0x2C:
        return new OpWith<sbyte>(opNode, "brfalse.s");
      case 0x2D:
        return new OpWith<sbyte>(opNode, "brtrue.s");
      case 0x2E:
        return new OpWith<sbyte>(opNode, "beq.s");
      case 0x2F:
        return new OpWith<sbyte>(opNode, "bge.s");
      case 0x30:
        return new OpWith<sbyte>(opNode, "bgt.s");
      case 0x31:
        return new OpWith<sbyte>(opNode, "ble.s");
      case 0x32:
        return new OpWith<sbyte>(opNode, "blt.s");
      case 0x33:
        return new OpWith<sbyte>(opNode, "bne.un.s");
      case 0x34:
        return new OpWith<sbyte>(opNode, "bge.un.s");
      case 0x35:
        return new OpWith<sbyte>(opNode, "bgt.un.s");
      case 0x36:
        return new OpWith<sbyte>(opNode, "ble.un.s");
      case 0x37:
        return new OpWith<sbyte>(opNode, "blt.un.s");
      case 0x38:
        return new OpWith<int>(opNode, "br");
      case 0x39:
        return new OpWith<int>(opNode, "brfalse");
      case 0x3A:
        return new OpWith<int>(opNode, "brtrue");
      case 0x3B:
        return new OpWith<int>(opNode, "beq");
      case 0x3C:
        return new OpWith<int>(opNode, "bge");
      case 0x3D:
        return new OpWith<int>(opNode, "bgt");
      case 0x3E:
        return new OpWith<int>(opNode, "ble");
      case 0x3F:
        return new OpWith<int>(opNode, "blt");
      case 0x40:
        return new OpWith<int>(opNode, "bne.un");
      case 0x41:
        return new OpWith<int>(opNode, "bge.un");
      case 0x42:
        return new OpWith<int>(opNode, "bgt.un");
      case 0x43:
        return new OpWith<int>(opNode, "ble.un");
      case 0x44:
        return new OpWith<int>(opNode, "blt.un");
      case 0x45:
        return new SwitchOp(opNode);
      case 0x46:
        return new Op(opNode, "ldind.i1");
      case 0x47:
        return new Op(opNode, "ldind.u1");
      case 0x48:
        return new Op(opNode, "ldind.i2");
      case 0x49:
        return new Op(opNode, "ldind.u2");
      case 0x4A:
        return new Op(opNode, "ldind.i4");
      case 0x4B:
        return new Op(opNode, "ldind.u4");
      case 0x4C:
        return new Op(opNode, "ldind.i8");
      case 0x4D:
        return new Op(opNode, "ldind.i");
      case 0x4E:
        return new Op(opNode, "ldind.r4");
      case 0x4F:
        return new Op(opNode, "ldind.r8");
      case 0x50:
        return new Op(opNode, "ldind.ref");
      case 0x51:
        return new Op(opNode, "stind.ref");
      case 0x52:
        return new Op(opNode, "stind.i1");
      case 0x53:
        return new Op(opNode, "stind.i2");
      case 0x54:
        return new Op(opNode, "stind.i4");
      case 0x55:
        return new Op(opNode, "stind.i8");
      case 0x56:
        return new Op(opNode, "stind.r4");
      case 0x57:
        return new Op(opNode, "stind.r8");
      case 0x58:
        return new Op(opNode, "add");
      case 0x59:
        return new Op(opNode, "sub");
      case 0x5A:
        return new Op(opNode, "mul");
      case 0x5B:
        return new Op(opNode, "div");
      case 0x5C:
        return new Op(opNode, "div.un");
      case 0x5D:
        return new Op(opNode, "rem");
      case 0x5E:
        return new Op(opNode, "rem.un");
      case 0x5F:
        return new Op(opNode, "and");
      case 0x60:
        return new Op(opNode, "or");
      case 0x61:
        return new Op(opNode, "xor");
      case 0x62:
        return new Op(opNode, "shl");
      case 0x63:
        return new Op(opNode, "shr");
      case 0x64:
        return new Op(opNode, "shr.un");
      case 0x65:
        return new Op(opNode, "neg");
      case 0x66:
        return new Op(opNode, "not");
      case 0x67:
        return new Op(opNode, "conv.i1");
      case 0x68:
        return new Op(opNode, "conv.i2");
      case 0x69:
        return new Op(opNode, "conv.i4");
      case 0x6A:
        return new Op(opNode, "conv.i8");
      case 0x6B:
        return new Op(opNode, "conv.r4");
      case 0x6C:
        return new Op(opNode, "conv.r8");
      case 0x6D:
        return new Op(opNode, "conv.u4");
      case 0x6E:
        return new Op(opNode, "conv.u8");
      case 0x6F:
        return new OpWithToken(opNode, "callvirt");
      case 0x70:
        return new OpWithToken(opNode, "cpobj");
      case 0x71:
        return new OpWithToken(opNode, "ldobj");
      case 0x72:
        return new OpWithToken(opNode, "ldstr");
      case 0x73:
        return new OpWithToken(opNode, "newobj");
      case 0x74:
        return new OpWithToken(opNode, "castclass");
      case 0x75:
        return new OpWithToken(opNode, "isinst");
      case 0x76:
        return new Op(opNode, "conv.r.un");
      case 0x79:
        return new OpWithToken(opNode, "unbox");
      case 0x7A:
        return new Op(opNode, "throw");
      case 0x7B:
        return new OpWithToken(opNode, "ldfld");
      case 0x7C:
        return new OpWithToken(opNode, "ldflda");
      case 0x7D:
        return new OpWithToken(opNode, "stfld");
      case 0x7E:
        return new OpWithToken(opNode, "ldsfld");
      case 0x7F:
        return new OpWithToken(opNode, "ldsflda");
      case 0x80:
        return new OpWithToken(opNode, "stsfld");
      case 0x81:
        return new OpWithToken(opNode, "stobj");
      case 0x82:
        return new Op(opNode, "conv.ovf.i1.un");
      case 0x83:
        return new Op(opNode, "conv.ovf.i2.un");
      case 0x84:
        return new Op(opNode, "conv.ovf.i4.un");
      case 0x85:
        return new Op(opNode, "conv.ovf.i8.un");
      case 0x86:
        return new Op(opNode, "conv.ovf.u1.un");
      case 0x87:
        return new Op(opNode, "conv.ovf.u2.un");
      case 0x88:
        return new Op(opNode, "conv.ovf.u4.un");
      case 0x89:
        return new Op(opNode, "conv.ovf.u8.un");
      case 0x8A:
        return new Op(opNode, "conv.ovf.i.un");
      case 0x8B:
        return new Op(opNode, "conv.ovf.u.un");
      case 0x8C:
        return new OpWithToken(opNode, "box");
      case 0x8D:
        return new OpWithToken(opNode, "newarr");
      case 0x8E:
        return new Op(opNode, "ldlen");
      case 0x8F:
        return new OpWithToken(opNode, "ldelema");
      case 0x90:
        return new Op(opNode, "ldelem.i1");
      case 0x91:
        return new Op(opNode, "ldelem.u1");
      case 0x92:
        return new Op(opNode, "ldelem.i2");
      case 0x93:
        return new Op(opNode, "ldelem.u2");
      case 0x94:
        return new Op(opNode, "ldelem.i4");
      case 0x95:
        return new Op(opNode, "ldelem.u4");
      case 0x96:
        return new Op(opNode, "ldelem.i8");
      case 0x97:
        return new Op(opNode, "ldelem.i");
      case 0x98:
        return new Op(opNode, "ldelem.r4");
      case 0x99:
        return new Op(opNode, "ldelem.r8");
      case 0x9A:
        return new Op(opNode, "ldelem.ref");
      case 0x9B:
        return new Op(opNode, "stelem.i");
      case 0x9C:
        return new Op(opNode, "stelem.i1");
      case 0x9D:
        return new Op(opNode, "stelem.i2");
      case 0x9E:
        return new Op(opNode, "stelem.i4");
      case 0x9F:
        return new Op(opNode, "stelem.i8");
      case 0xA0:
        return new Op(opNode, "stelem.r4");
      case 0xA1:
        return new Op(opNode, "stelem.r8");
      case 0xA2:
        return new Op(opNode, "stelem.ref");
      case 0xA3:
        return new OpWithToken(opNode, "ldelem");
      case 0xA4:
        return new OpWithToken(opNode, "stelem");
      case 0xA5:
        return new OpWithToken(opNode, "unbox.any");
      case 0xB3:
        return new Op(opNode, "conv.ovf.i1");
      case 0xB4:
        return new Op(opNode, "conv.ovf.u1");
      case 0xB5:
        return new Op(opNode, "conv.ovf.i2");
      case 0xB6:
        return new Op(opNode, "conv.ovf.u2");
      case 0xB7:
        return new Op(opNode, "conv.ovf.i4");
      case 0xB8:
        return new Op(opNode, "conv.ovf.u4");
      case 0xB9:
        return new Op(opNode, "conv.ovf.i8");
      case 0xBA:
        return new Op(opNode, "conv.ovf.u8");
      case 0xC2:
        return new OpWithToken(opNode, "refanyval");
      case 0xC3:
        return new OpWithToken(opNode, "ckfinite");
      case 0xC6:
        return new OpWithToken(opNode, "mkrefany");
      case 0xD0:
        return new OpWithToken(opNode, "ldtoken");
      case 0xD1:
        return new Op(opNode, "conv.u2");
      case 0xD2:
        return new Op(opNode, "conv.u1");
      case 0xD3:
        return new Op(opNode, "conv.i");
      case 0xD4:
        return new Op(opNode, "conv.ovf.i");
      case 0xD5:
        return new Op(opNode, "conv.ovf.u");
      case 0xD6:
        return new Op(opNode, "add.ovf");
      case 0xD7:
        return new Op(opNode, "add.ovf.un");
      case 0xD8:
        return new Op(opNode, "mul.ovf");
      case 0xD9:
        return new Op(opNode, "mul.ovf.un");
      case 0xDA:
        return new Op(opNode, "sub.ovf");
      case 0xDB:
        return new Op(opNode, "sub.ovf.un");
      case 0xDC:
        return new Op(opNode, "endfault"); //TODO(pedant) endfinally?
      case 0xDD:
        return new OpWith<int>(opNode, "leave");
      case 0xDE:
        return new OpWith<sbyte>(opNode, "leave_s");
      case 0xDF:
        return new Op(opNode, "stind.i");
      case 0xE0:
        return new Op(opNode, "conv.u");
      case 0xFE:
        byte secondByte;
        var secondOpNode = stream.ReadStruct(out secondByte, "OpCode");
        var combinedOp = new CodeNode("OpCode");
        combinedOp.Start = opNode.Start;
        combinedOp.End = secondOpNode.End;
        combinedOp.Value = opNode.Value + " " + secondOpNode.Value;
        switch (secondByte) {
          case 0x00:
            return new Op(combinedOp, "arglist");
          case 0x01:
            return new Op(combinedOp, "ceq");
          case 0x02:
            return new Op(combinedOp, "cgt");
          case 0x03:
            return new Op(combinedOp, "cgt.un");
          case 0x04:
            return new Op(combinedOp, "clt");
          case 0x05:
            return new Op(combinedOp, "clt.un");
          case 0x06:
            return new OpWithToken(combinedOp, "ldftn");
          case 0x07:
            return new OpWithToken(combinedOp, "ldvirtftn");
          case 0x09:
            return new OpWith<ushort>(combinedOp, "ldarg");
          case 0x0A:
            return new OpWith<ushort>(combinedOp, "ldarga");
          case 0x0B:
            return new OpWith<ushort>(combinedOp, "starg");
          case 0x0C:
            return new OpWith<ushort>(combinedOp, "ldloc");
          case 0x0D:
            return new OpWith<ushort>(combinedOp, "ldloca");
          case 0x0E:
            return new OpWith<ushort>(combinedOp, "stloc");
          case 0x0F:
            return new Op(combinedOp, "localloc");
          case 0x11:
            return new Op(combinedOp, "endfilter");
          case 0x12:
            throw new NotImplementedException($"unaligned.");
          case 0x13:
            throw new NotImplementedException($"volatile.");
          case 0x14:
            throw new NotImplementedException($"tail.");
          case 0x15:
            return new OpWithToken(combinedOp, "initobj");
          case 0x16:
            return new OpWithToken(combinedOp, "constrained.");
          case 0x17:
            return new Op(combinedOp, "cpblk");
          case 0x18:
            return new Op(combinedOp, "initblk");
          case 0x19:
            throw new NotImplementedException($"no.");
          case 0x1A:
            return new Op(combinedOp, "rethrow");
          case 0x1C:
            return new OpWithToken(combinedOp, "sizeof");
          case 0x1D:
            return new Op(combinedOp, "refanytype");
          case 0x1E:
            throw new NotImplementedException($"readonly.");
          default:
            throw new InvalidOperationException($"unkonwn op 0xFE 0x{secondByte:X}");
        }
      default:
        throw new InvalidOperationException($"unknown op 0x{firstByte:X}");
    }
  }
}

// III.1.9
sealed class MetadataToken : ICanRead, IHaveLiteralValue
{
  public UInt24 offset;
  public byte table;

  public UserStringHeapIndex index;

  public object Value { get; set; }


  public CodeNode Read(Stream stream) {
    var offsetNode = stream.ReadClass(ref offset, nameof(offset));
    var tableNode = stream.ReadStruct(out table, nameof(table));

    if (table == 0x70) {
      // Reposition stream, and read "XX XX 00 70"
      stream.Position -= 4;

      var indexNode = stream.ReadClass(ref index, nameof(index));

      if (stream.ReallyReadByte() != 0)
        throw new NotImplementedException("Too big UserStringHeapIndex");
      indexNode.End++;
      stream.Position += 1;

      var node = new CodeNode
      {
                indexNode,
                tableNode,
            };
      tableNode.Description = "UserStringHeapIndex";
      Value = UserStringHeap.Get(index).GetString();
      return node;
    } else {
      var node = new CodeNode
      {
                offsetNode,
                tableNode,
            };
      var flag = (MetadataTableFlags)(1L << table);
      tableNode.Description = flag.ToString();
      var link = Singletons.Instance.TildeStream.GetCodeNode(flag, offset.IntValue - 1); // indexed by 1
      offsetNode.Link = link;
      Value = link.Value;
      return node;
    }
  }
}

sealed class Op : ICanRead
{
  CodeNode op;
  public Op(CodeNode op, string opName) {
    this.op = op;
    op.Description = opName;
  }

  public CodeNode Read(Stream stream) => op;
}

sealed class OpWith<T> : ICanRead
    where T : struct
{
  CodeNode op;
  public T value;
  string opName;

  public OpWith(CodeNode op, string opName) {
    this.op = op;
    this.opName = opName;
  }

  public CodeNode Read(Stream stream) {
    var node = new CodeNode
    {
            op,
            stream.ReadStruct(out value, nameof(value)),
        };
    node.Description = opName + " " + value.GetString();
    return node;
  }
}

sealed class OpWithToken : ICanRead
{
  CodeNode op;
  public MetadataToken token;

  public OpWithToken(CodeNode op, string opName) {
    this.op = op;
    op.Description = opName;
  }

  public CodeNode Read(Stream stream) {
    var tokenNode = stream.ReadClass(ref token, nameof(token));
    var node = new CodeNode
    {
            op,
            tokenNode,
        };
    node.Description = op.Description + " " + tokenNode.Value;

    return node;
  }
}

sealed class SwitchOp : ICanRead
{
  CodeNode op;
  public uint count;
  public int[] targets;

  public SwitchOp(CodeNode op) {
    this.op = op;
    op.Description = "switch";
  }

  public CodeNode Read(Stream stream) {
    var node = new CodeNode
    {
            op,
            stream.ReadStruct(out count, nameof(count)),
            stream.ReadStructs(out targets, (int)count, nameof(targets)), //TODO(links) switch offset
        };
    node.Description = $"switch ({string.Join(", ", targets)})";
    return node;
  }
}
