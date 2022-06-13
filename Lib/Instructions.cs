// Copyright Microsoft, 2017
// Copyright Carl Walsh, 2021

using System;
using System.Collections.Generic;
using System.Linq;

// CodeNode is written though reflection
#pragma warning disable 0649 // CS0649: Field '...' is never assigned to

//TODO(link) Link branch targets -- done for br and switch, do for throw
//TODO(method) §III.1.5 validate operand type with needed manual conversions
//TODO(method) §III.1.8.1.3 validate all branches have mergable types in the stack
//TODO(method) check multiple branches to ret with different type on the stack?

// III
sealed class InstructionStream : CodeNode
{
  Dictionary<int, Op> ops = new Dictionary<int, Op>();

  Method method;

  public InstructionStream(Method method) {
    this.method = method;
  }

  protected override void InnerRead() {
    while (Bytes.Stream.Position - Start < method.CodeSize) {
      var op = Bytes.ReadClass<Op>();
      op.NodeName = $"Op[{Children.Count}]";
      Children.Add(op);
      ops.Add(op.Start, op);
    }

    if (method.MethodDataSections == null) {
      ValidateStack(); // TODO(fixme) fix stack validation for catch blocks that push an exception object
    }
    Description = string.Join("\n", Children.Take(10).Select(n => n.Description.Split(" (Stack:")[0]));
  }

  void ValidateStack() {
    int? stack = null;
    foreach (Op op in Children) {
      // III.1.7.5 even after unconditional branch, we must assume the stack is empty
      if (!stack.HasValue) stack = 0;

      ValidateOp(op, stack.Value);

      if (op.Def.controlFlow == "RETURN") {
        if (this.method.ReturnsVoid) {
          if (stack > 0) {
            op.Errors.Add($"return type is void, but stack is {stack}");
          }
        } else {
          if (stack != 1) {
            op.Errors.Add($"stack should be 1, but stack is {stack}");
          }
          --stack;
        }
        op.SetEndStack(stack.Value);

        stack = null;
        continue;
      }


      stack = op.DataPopPush(method.MaxStack);

      switch (op.Def.controlFlow) {
        case "NEXT":
        case "CALL":
        case "META":
        case "BREAK": // Debugger breakpoint
          break;
        case "BRANCH":
          ValidateBranch(op, stack.Value);
          stack = null;
          break;
        case "COND_BRANCH":
          ValidateBranch(op, stack.Value);
          break;
        case "THROW":
          throw new NotImplementedException("throw catch/finally frames");
        case "RETURN":
          stack = null;
          break;
        default:
          throw new InvalidOperationException(op.Def.controlFlow);
      }
    }
  }

  static void ValidateOp(Op op, int stack) {
    if (op.StackCalculated) {
      if (op.StartStack != stack) {
        op.Errors.Add($"Stack depth {stack} != {op.StartStack}");
      }
    } else {
      op.StartStack = stack;
    }
  }

  // III.1.7.2 validate branch targets are valid offsets
  void ValidateBranch(Op op, int stack) {
    var targets = op.Def.name == "switch" ? op.Targets : op.Children.Skip(1);
    foreach (var targetArg in targets) {
      var target = targetArg.GetInt32() + op.End;

      if (!ops.TryGetValue(target, out var targetOp)) {
        op.Errors.Add($"Branch target {target:X2} not found");
        return;
      }

      targetArg.Link = targetOp;
      ValidateOp(targetOp, stack);
    }
  }
}

// III.1.9
sealed class MetadataToken : CodeNode
{
  public UInt24 Offset;
  public byte Table;
  public CodeNode LinkedTableRow { get; private set; }

  public UserStringHeapIndex Index;

  protected override void InnerRead() {
    var origPos = Bytes.Stream.Position;
    AddChild(nameof(Offset));
    AddChild(nameof(Table));

    if (Table == 0x70) {
      Children.Clear();
      // Reposition stream, and read "XX XX 00 70"
      Bytes.Stream.Position = origPos;

      AddChild(nameof(Index));

      if (Bytes.Read<byte>() != 0)
        throw new InvalidOperationException("UserStringHeap can only be two-byte");
      Index.End++;

      AddChild(nameof(Table));
      Children.Last().Description = "UserStringHeapIndex";

      NodeValue = Index.NodeValue;
    } else {
      var flag = (MetadataTableFlags)(1L << Table);
      Children.Last().Description = flag.ToString();
      var link = Bytes.TildeStream.GetCodeNode(flag, Offset.IntValue - 1); // indexed by 1
      Children.First().Link = LinkedTableRow = link;
      NodeValue = link.NodeValue;
    }
  }
}

sealed class Op : CodeNode
{
  public byte Opcode;

  public OpCode Def { get; set; }
  public int StartStack { get; set; } = -1; // would be better to use types
  public bool StackCalculated => StartStack != -1;

  string desc;
  string stackTransition;
  public override string Description => $"{desc} (Stack: {stackTransition})";

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

    desc = ReadInLineArguments();

    if (Children.Count == 1) {
      NodeValue = Children.Single().NodeValue;
      Children.Clear();
    }
  }

  // III.1.3 validate stack tranditions
  // III.1.7.4 validate stack depth doesn't violate maxstack
  public int DataPopPush(int maxstack) {
    int stack = StartStack;

    if (Def.stackPop == "VarPop") {
      // We know that RETURN was already handled, so we have a Token

      var o = Token.LinkedTableRow;
      if (o is MethodSpec spec) {
        o = spec.Method.Link;
      }
      var sig = o switch {
        MemberRef m => m.Signature.MethodDefRefSig,
        MethodDef m => m.Signature.MethodDefRefSig,
        StandAloneSig s => s.Signature.MethodDefRefSig,
        _ => throw new InvalidOperationException(o.GetType().Name),
      };

      if (Def.name == "calli") {
        stack -= 1; // ftn
      }
      stack -= sig.Params.Length;
      stack -= sig.VarArgParams.Length;

      if (Def.name != "newobj" &&
          sig.Kind.Flags.HasFlag(CallingConvention.UpperBits.HASTHIS) &&
          !sig.Kind.Flags.HasFlag(CallingConvention.UpperBits.EXPLICITTHIS)) {
        stack -= 1; // obj
      }

      if (stack < 0) {
        Errors.Add($"Stack underflow!");
        stack = 0;
      }

      if (Def.name == "newobj") {
        // .ctor return type is void but the reference is pushed on the stack
        stack += 1;
      } else if (Def.stackPush == "VarPush") {
        if (sig.RetType.Void != ElementType.Void) {
          stack += 1;
        }
      } else {
        throw new InvalidOperationException(Def.stackPush);
      }
    } else {

      foreach (var s in Def.stackPop.Split("Pop").Where(s => s != "")) {
        stack -= GetStackElem(s);
      }
      if (stack < 0) {
        Errors.Add($"Stack underflow!");
        stack = 0;
      }
      foreach (var s in Def.stackPush.Split("Push").Where(s => s != "")) {
        stack += GetStackElem(s);
      }
    }

    if (stack > maxstack) {
      Errors.Add($"Stack is {stack} > maxstack {maxstack}");
    }

    if (stackTransition == null) {
      SetEndStack(stack);
    }
    return stack;
  }

  public void SetEndStack(int stack) => stackTransition = $"{StartStack} -> {stack}";

  // VI.C.2 CIL opcode descriptions
  static int GetStackElem(string s) => s switch {
    // TODO(StackType) these should return int, float, etc
    "0" => 0,
    "1" => 1,
    "I" => 1,
    "I8" => 1,
    "R4" => 1,
    "R8" => 1,
    "Ref" => 1,
    _ => throw new InvalidOperationException(s),
  };

  string ReadInLineArguments() => Def.opParams switch {
    // MAYBE use OpCode const fields
    "InlineBrTarget" => With<uint>(),
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
    var value = Bytes.ReadClass<StructNode<T>>();
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
  // Using StructNode[] keeps each row its own size
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
