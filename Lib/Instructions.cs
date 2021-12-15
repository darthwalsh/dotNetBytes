// Copyright Microsoft, 2017
// Copyright Carl Walsh, 2021

using System;
using System.Linq;

// CodeNode is written though reflection
#pragma warning disable 0649 // CS0649: Field '...' is never assigned to

//TODO(link) Link branch targets
//TODO(method) §III.1.7.2 validate branch targets are valid offsets
//TODO(method) §III.1.3 validate stack depth doesn't go negative or violate maxstack §III.1.7.4
//TODO(method) §III.1.5 validate operand type with needed manual conversions
//TODO(method) §III.1.7.4 validate branching stack depth is consistant
//TODO(method) §III.1.7.5 unseen op after unconditional branch the stack is assumed to have depth zero
//TODO(method) §III.1.8.1.3 validate all branches have mergable types in the stack

// TODO?????checking that methods return with 0 or 1 elements on the stack

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
  public byte Opcode;

  public OpCode Def { get; set; }

  protected override void InnerRead() {
    AddChild(nameof(Opcode));

    if (Opcode == 0xFE) {
      var secondByte = Bytes.Read<byte>();

      var opcodeNode = Children.Single();
      opcodeNode.End = (int)Bytes.Stream.Position;
      opcodeNode.NodeValue = opcodeNode.NodeValue + " " + secondByte.GetString();

      Def = OpCode.SecondByte(secondByte);
    } else {
      Def = OpCode.FirstByte(Opcode);
    }

    Children.Single().Description = Def.name;

    Description = ReadInLineArguments();

    if (Children.Count == 1) {
      NodeValue = Children.Single().NodeValue;
      Children.Clear();
    }
  }

  // §VI.C.2
  string ReadInLineArguments() => Def.opParams switch {
    "InlineBrTarget" => With<sbyte>(),
    "InlineField" => WithToken(),
    "InlineI" => With<int>(),
    "InlineI8" => With<long>(),
    "InlineMethod" => WithToken(),
    "InlineNone" => Def.name,
    "InlineR" => With<double>(),
    "InlineSig" => WithToken(),
    "InlineString" => WithToken(),
    "InlineSwitch" => SwitchOp(),
    "InlineTok" => WithToken(),
    "InlineType" => WithToken(),
    "InlineVar" => With<ushort>(),
    "ShortInlineBrTarget" => With<sbyte>(),
    "ShortInlineI" => With<sbyte>(),
    "ShortInlineR" => With<float>(),
    "ShortInlineVar" => With<byte>(),
    _ => throw new InvalidOperationException(Def.opParams),
  };

  string With<T>() where T : struct {
    var value = new StructNode<T> { Bytes = Bytes };
    value.Read();
    value.NodeName = "Value";
    Children.Add(value);

    return $"{Def.name} {value.NodeValue}";
  }

  public MetadataToken Token;
  string WithToken() {
    AddChild(nameof(Token));
    return $"{Def.name} {Token.NodeValue}";
  }

  public uint Count;
  //TODO(link) link each target to op. Using StructNode<uint> keeps each row its own size
  public StructNode<int>[] Targets;
  string SwitchOp() {
    AddChild(nameof(Count));
    AddChild(nameof(Targets));
    return $"switch ({string.Join(", ", Targets.Select(n => n.t))})";
  }

  protected override int GetCount(string field) => field switch {
    nameof(Targets) => (int)Count,
    _ => base.GetCount(field),
  };
}
