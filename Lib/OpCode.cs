// Copyright (c) .NET Foundation and Contributors; Licensed MIT
// Copied from https://github.com/dotnet/runtime/blob/v6.0.0/src/coreclr/inc/opcode.def

// Would be simpler to loop through System.Reflection.Emit.OpCodes static fields,
// but prefer to instead use the definitions here.

using System;
using System.Collections.Generic;

sealed class OpCode
{
  const string BRANCH = "BRANCH";
  const string BREAK = "BREAK";
  const string CALL = "CALL";
  const string COND_BRANCH = "COND_BRANCH";
  const string IMacro = "IMacro";
  const string InlineBrTarget = "InlineBrTarget";
  const string InlineField = "InlineField";
  const string InlineI = "InlineI";
  const string InlineI8 = "InlineI8";
  const string InlineMethod = "InlineMethod";
  const string InlineNone = "InlineNone";
  const string InlineR = "InlineR";
  const string InlineSig = "InlineSig";
  const string InlineString = "InlineString";
  const string InlineSwitch = "InlineSwitch";
  const string InlineTok = "InlineTok";
  const string InlineType = "InlineType";
  const string InlineVar = "InlineVar";
  const string IObjModel = "IObjModel";
  const string IPrefix = "IPrefix";
  const string IPrimitive = "IPrimitive";
  const string META = "META";
  const string NEXT = "NEXT";
  const string Pop0 = "Pop0";
  const string Pop1 = "Pop1";
  const string PopI = "PopI";
  const string PopI8 = "PopI8";
  const string PopR4 = "PopR4";
  const string PopR8 = "PopR8";
  const string PopRef = "PopRef";
  const string Push0 = "Push0";
  const string Push1 = "Push1";
  const string PushI = "PushI";
  const string PushI8 = "PushI8";
  const string PushR4 = "PushR4";
  const string PushR8 = "PushR8";
  const string PushRef = "PushRef";
  const string RETURN = "RETURN";
  const string ShortInlineBrTarget = "ShortInlineBrTarget";
  const string ShortInlineI = "ShortInlineI";
  const string ShortInlineR = "ShortInlineR";
  const string ShortInlineVar = "ShortInlineVar";
  const string THROW = "THROW";
  const string VarPop = "VarPop";
  const string VarPush = "VarPush";

  static OpCode() {
    // Edits made to OPDEF lines: 
    // - Remove the enum def names: s/CEE_\w+, //g
    // - Remove unused op codes: s/.*unused.*\n//g
    // - Remove prefix meta codes: s/.*"prefix.*\n//g
    OPDEF("nop", Pop0, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x00, NEXT);
    OPDEF("break", Pop0, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x01, BREAK);
    OPDEF("ldarg.0", Pop0, Push1, InlineNone, IMacro, 1, 0xFF, 0x02, NEXT);
    OPDEF("ldarg.1", Pop0, Push1, InlineNone, IMacro, 1, 0xFF, 0x03, NEXT);
    OPDEF("ldarg.2", Pop0, Push1, InlineNone, IMacro, 1, 0xFF, 0x04, NEXT);
    OPDEF("ldarg.3", Pop0, Push1, InlineNone, IMacro, 1, 0xFF, 0x05, NEXT);
    OPDEF("ldloc.0", Pop0, Push1, InlineNone, IMacro, 1, 0xFF, 0x06, NEXT);
    OPDEF("ldloc.1", Pop0, Push1, InlineNone, IMacro, 1, 0xFF, 0x07, NEXT);
    OPDEF("ldloc.2", Pop0, Push1, InlineNone, IMacro, 1, 0xFF, 0x08, NEXT);
    OPDEF("ldloc.3", Pop0, Push1, InlineNone, IMacro, 1, 0xFF, 0x09, NEXT);
    OPDEF("stloc.0", Pop1, Push0, InlineNone, IMacro, 1, 0xFF, 0x0A, NEXT);
    OPDEF("stloc.1", Pop1, Push0, InlineNone, IMacro, 1, 0xFF, 0x0B, NEXT);
    OPDEF("stloc.2", Pop1, Push0, InlineNone, IMacro, 1, 0xFF, 0x0C, NEXT);
    OPDEF("stloc.3", Pop1, Push0, InlineNone, IMacro, 1, 0xFF, 0x0D, NEXT);
    OPDEF("ldarg.s", Pop0, Push1, ShortInlineVar, IMacro, 1, 0xFF, 0x0E, NEXT);
    OPDEF("ldarga.s", Pop0, PushI, ShortInlineVar, IMacro, 1, 0xFF, 0x0F, NEXT);
    OPDEF("starg.s", Pop1, Push0, ShortInlineVar, IMacro, 1, 0xFF, 0x10, NEXT);
    OPDEF("ldloc.s", Pop0, Push1, ShortInlineVar, IMacro, 1, 0xFF, 0x11, NEXT);
    OPDEF("ldloca.s", Pop0, PushI, ShortInlineVar, IMacro, 1, 0xFF, 0x12, NEXT);
    OPDEF("stloc.s", Pop1, Push0, ShortInlineVar, IMacro, 1, 0xFF, 0x13, NEXT);
    OPDEF("ldnull", Pop0, PushRef, InlineNone, IPrimitive, 1, 0xFF, 0x14, NEXT);
    OPDEF("ldc.i4.m1", Pop0, PushI, InlineNone, IMacro, 1, 0xFF, 0x15, NEXT);
    OPDEF("ldc.i4.0", Pop0, PushI, InlineNone, IMacro, 1, 0xFF, 0x16, NEXT);
    OPDEF("ldc.i4.1", Pop0, PushI, InlineNone, IMacro, 1, 0xFF, 0x17, NEXT);
    OPDEF("ldc.i4.2", Pop0, PushI, InlineNone, IMacro, 1, 0xFF, 0x18, NEXT);
    OPDEF("ldc.i4.3", Pop0, PushI, InlineNone, IMacro, 1, 0xFF, 0x19, NEXT);
    OPDEF("ldc.i4.4", Pop0, PushI, InlineNone, IMacro, 1, 0xFF, 0x1A, NEXT);
    OPDEF("ldc.i4.5", Pop0, PushI, InlineNone, IMacro, 1, 0xFF, 0x1B, NEXT);
    OPDEF("ldc.i4.6", Pop0, PushI, InlineNone, IMacro, 1, 0xFF, 0x1C, NEXT);
    OPDEF("ldc.i4.7", Pop0, PushI, InlineNone, IMacro, 1, 0xFF, 0x1D, NEXT);
    OPDEF("ldc.i4.8", Pop0, PushI, InlineNone, IMacro, 1, 0xFF, 0x1E, NEXT);
    OPDEF("ldc.i4.s", Pop0, PushI, ShortInlineI, IMacro, 1, 0xFF, 0x1F, NEXT);
    OPDEF("ldc.i4", Pop0, PushI, InlineI, IPrimitive, 1, 0xFF, 0x20, NEXT);
    OPDEF("ldc.i8", Pop0, PushI8, InlineI8, IPrimitive, 1, 0xFF, 0x21, NEXT);
    OPDEF("ldc.r4", Pop0, PushR4, ShortInlineR, IPrimitive, 1, 0xFF, 0x22, NEXT);
    OPDEF("ldc.r8", Pop0, PushR8, InlineR, IPrimitive, 1, 0xFF, 0x23, NEXT);
    OPDEF("dup", Pop1, Push1 + Push1, InlineNone, IPrimitive, 1, 0xFF, 0x25, NEXT);
    OPDEF("pop", Pop1, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x26, NEXT);
    OPDEF("jmp", Pop0, Push0, InlineMethod, IPrimitive, 1, 0xFF, 0x27, CALL);
    OPDEF("call", VarPop, VarPush, InlineMethod, IPrimitive, 1, 0xFF, 0x28, CALL);
    OPDEF("calli", VarPop, VarPush, InlineSig, IPrimitive, 1, 0xFF, 0x29, CALL);
    OPDEF("ret", VarPop, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x2A, RETURN);
    OPDEF("br.s", Pop0, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x2B, BRANCH);
    OPDEF("brfalse.s", PopI, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x2C, COND_BRANCH);
    OPDEF("brtrue.s", PopI, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x2D, COND_BRANCH);
    OPDEF("beq.s", Pop1 + Pop1, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x2E, COND_BRANCH);
    OPDEF("bge.s", Pop1 + Pop1, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x2F, COND_BRANCH);
    OPDEF("bgt.s", Pop1 + Pop1, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x30, COND_BRANCH);
    OPDEF("ble.s", Pop1 + Pop1, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x31, COND_BRANCH);
    OPDEF("blt.s", Pop1 + Pop1, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x32, COND_BRANCH);
    OPDEF("bne.un.s", Pop1 + Pop1, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x33, COND_BRANCH);
    OPDEF("bge.un.s", Pop1 + Pop1, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x34, COND_BRANCH);
    OPDEF("bgt.un.s", Pop1 + Pop1, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x35, COND_BRANCH);
    OPDEF("ble.un.s", Pop1 + Pop1, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x36, COND_BRANCH);
    OPDEF("blt.un.s", Pop1 + Pop1, Push0, ShortInlineBrTarget, IMacro, 1, 0xFF, 0x37, COND_BRANCH);
    OPDEF("br", Pop0, Push0, InlineBrTarget, IPrimitive, 1, 0xFF, 0x38, BRANCH);
    OPDEF("brfalse", PopI, Push0, InlineBrTarget, IPrimitive, 1, 0xFF, 0x39, COND_BRANCH);
    OPDEF("brtrue", PopI, Push0, InlineBrTarget, IPrimitive, 1, 0xFF, 0x3A, COND_BRANCH);
    OPDEF("beq", Pop1 + Pop1, Push0, InlineBrTarget, IMacro, 1, 0xFF, 0x3B, COND_BRANCH);
    OPDEF("bge", Pop1 + Pop1, Push0, InlineBrTarget, IMacro, 1, 0xFF, 0x3C, COND_BRANCH);
    OPDEF("bgt", Pop1 + Pop1, Push0, InlineBrTarget, IMacro, 1, 0xFF, 0x3D, COND_BRANCH);
    OPDEF("ble", Pop1 + Pop1, Push0, InlineBrTarget, IMacro, 1, 0xFF, 0x3E, COND_BRANCH);
    OPDEF("blt", Pop1 + Pop1, Push0, InlineBrTarget, IMacro, 1, 0xFF, 0x3F, COND_BRANCH);
    OPDEF("bne.un", Pop1 + Pop1, Push0, InlineBrTarget, IMacro, 1, 0xFF, 0x40, COND_BRANCH);
    OPDEF("bge.un", Pop1 + Pop1, Push0, InlineBrTarget, IMacro, 1, 0xFF, 0x41, COND_BRANCH);
    OPDEF("bgt.un", Pop1 + Pop1, Push0, InlineBrTarget, IMacro, 1, 0xFF, 0x42, COND_BRANCH);
    OPDEF("ble.un", Pop1 + Pop1, Push0, InlineBrTarget, IMacro, 1, 0xFF, 0x43, COND_BRANCH);
    OPDEF("blt.un", Pop1 + Pop1, Push0, InlineBrTarget, IMacro, 1, 0xFF, 0x44, COND_BRANCH);
    OPDEF("switch", PopI, Push0, InlineSwitch, IPrimitive, 1, 0xFF, 0x45, COND_BRANCH);
    OPDEF("ldind.i1", PopI, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x46, NEXT);
    OPDEF("ldind.u1", PopI, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x47, NEXT);
    OPDEF("ldind.i2", PopI, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x48, NEXT);
    OPDEF("ldind.u2", PopI, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x49, NEXT);
    OPDEF("ldind.i4", PopI, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x4A, NEXT);
    OPDEF("ldind.u4", PopI, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x4B, NEXT);
    OPDEF("ldind.i8", PopI, PushI8, InlineNone, IPrimitive, 1, 0xFF, 0x4C, NEXT);
    OPDEF("ldind.i", PopI, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x4D, NEXT);
    OPDEF("ldind.r4", PopI, PushR4, InlineNone, IPrimitive, 1, 0xFF, 0x4E, NEXT);
    OPDEF("ldind.r8", PopI, PushR8, InlineNone, IPrimitive, 1, 0xFF, 0x4F, NEXT);
    OPDEF("ldind.ref", PopI, PushRef, InlineNone, IPrimitive, 1, 0xFF, 0x50, NEXT);
    OPDEF("stind.ref", PopI + PopI, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x51, NEXT);
    OPDEF("stind.i1", PopI + PopI, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x52, NEXT);
    OPDEF("stind.i2", PopI + PopI, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x53, NEXT);
    OPDEF("stind.i4", PopI + PopI, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x54, NEXT);
    OPDEF("stind.i8", PopI + PopI8, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x55, NEXT);
    OPDEF("stind.r4", PopI + PopR4, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x56, NEXT);
    OPDEF("stind.r8", PopI + PopR8, Push0, InlineNone, IPrimitive, 1, 0xFF, 0x57, NEXT);
    OPDEF("add", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x58, NEXT);
    OPDEF("sub", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x59, NEXT);
    OPDEF("mul", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x5A, NEXT);
    OPDEF("div", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x5B, NEXT);
    OPDEF("div.un", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x5C, NEXT);
    OPDEF("rem", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x5D, NEXT);
    OPDEF("rem.un", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x5E, NEXT);
    OPDEF("and", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x5F, NEXT);
    OPDEF("or", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x60, NEXT);
    OPDEF("xor", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x61, NEXT);
    OPDEF("shl", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x62, NEXT);
    OPDEF("shr", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x63, NEXT);
    OPDEF("shr.un", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x64, NEXT);
    OPDEF("neg", Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x65, NEXT);
    OPDEF("not", Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0x66, NEXT);
    OPDEF("conv.i1", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x67, NEXT);
    OPDEF("conv.i2", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x68, NEXT);
    OPDEF("conv.i4", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x69, NEXT);
    OPDEF("conv.i8", Pop1, PushI8, InlineNone, IPrimitive, 1, 0xFF, 0x6A, NEXT);
    OPDEF("conv.r4", Pop1, PushR4, InlineNone, IPrimitive, 1, 0xFF, 0x6B, NEXT);
    OPDEF("conv.r8", Pop1, PushR8, InlineNone, IPrimitive, 1, 0xFF, 0x6C, NEXT);
    OPDEF("conv.u4", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x6D, NEXT);
    OPDEF("conv.u8", Pop1, PushI8, InlineNone, IPrimitive, 1, 0xFF, 0x6E, NEXT);
    OPDEF("callvirt", VarPop, VarPush, InlineMethod, IObjModel, 1, 0xFF, 0x6F, CALL);
    OPDEF("cpobj", PopI + PopI, Push0, InlineType, IObjModel, 1, 0xFF, 0x70, NEXT);
    OPDEF("ldobj", PopI, Push1, InlineType, IObjModel, 1, 0xFF, 0x71, NEXT);
    OPDEF("ldstr", Pop0, PushRef, InlineString, IObjModel, 1, 0xFF, 0x72, NEXT);
    OPDEF("newobj", VarPop, PushRef, InlineMethod, IObjModel, 1, 0xFF, 0x73, CALL);
    OPDEF("castclass", PopRef, PushRef, InlineType, IObjModel, 1, 0xFF, 0x74, NEXT);
    OPDEF("isinst", PopRef, PushI, InlineType, IObjModel, 1, 0xFF, 0x75, NEXT);
    OPDEF("conv.r.un", Pop1, PushR8, InlineNone, IPrimitive, 1, 0xFF, 0x76, NEXT);
    OPDEF("unbox", PopRef, PushI, InlineType, IPrimitive, 1, 0xFF, 0x79, NEXT);
    OPDEF("throw", PopRef, Push0, InlineNone, IObjModel, 1, 0xFF, 0x7A, THROW);
    OPDEF("ldfld", PopRef, Push1, InlineField, IObjModel, 1, 0xFF, 0x7B, NEXT);
    OPDEF("ldflda", PopRef, PushI, InlineField, IObjModel, 1, 0xFF, 0x7C, NEXT);
    OPDEF("stfld", PopRef + Pop1, Push0, InlineField, IObjModel, 1, 0xFF, 0x7D, NEXT);
    OPDEF("ldsfld", Pop0, Push1, InlineField, IObjModel, 1, 0xFF, 0x7E, NEXT);
    OPDEF("ldsflda", Pop0, PushI, InlineField, IObjModel, 1, 0xFF, 0x7F, NEXT);
    OPDEF("stsfld", Pop1, Push0, InlineField, IObjModel, 1, 0xFF, 0x80, NEXT);
    OPDEF("stobj", PopI + Pop1, Push0, InlineType, IPrimitive, 1, 0xFF, 0x81, NEXT);
    OPDEF("conv.ovf.i1.un", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x82, NEXT);
    OPDEF("conv.ovf.i2.un", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x83, NEXT);
    OPDEF("conv.ovf.i4.un", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x84, NEXT);
    OPDEF("conv.ovf.i8.un", Pop1, PushI8, InlineNone, IPrimitive, 1, 0xFF, 0x85, NEXT);
    OPDEF("conv.ovf.u1.un", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x86, NEXT);
    OPDEF("conv.ovf.u2.un", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x87, NEXT);
    OPDEF("conv.ovf.u4.un", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x88, NEXT);
    OPDEF("conv.ovf.u8.un", Pop1, PushI8, InlineNone, IPrimitive, 1, 0xFF, 0x89, NEXT);
    OPDEF("conv.ovf.i.un", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x8A, NEXT);
    OPDEF("conv.ovf.u.un", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0x8B, NEXT);
    OPDEF("box", Pop1, PushRef, InlineType, IPrimitive, 1, 0xFF, 0x8C, NEXT);
    OPDEF("newarr", PopI, PushRef, InlineType, IObjModel, 1, 0xFF, 0x8D, NEXT);
    OPDEF("ldlen", PopRef, PushI, InlineNone, IObjModel, 1, 0xFF, 0x8E, NEXT);
    OPDEF("ldelema", PopRef + PopI, PushI, InlineType, IObjModel, 1, 0xFF, 0x8F, NEXT);
    OPDEF("ldelem.i1", PopRef + PopI, PushI, InlineNone, IObjModel, 1, 0xFF, 0x90, NEXT);
    OPDEF("ldelem.u1", PopRef + PopI, PushI, InlineNone, IObjModel, 1, 0xFF, 0x91, NEXT);
    OPDEF("ldelem.i2", PopRef + PopI, PushI, InlineNone, IObjModel, 1, 0xFF, 0x92, NEXT);
    OPDEF("ldelem.u2", PopRef + PopI, PushI, InlineNone, IObjModel, 1, 0xFF, 0x93, NEXT);
    OPDEF("ldelem.i4", PopRef + PopI, PushI, InlineNone, IObjModel, 1, 0xFF, 0x94, NEXT);
    OPDEF("ldelem.u4", PopRef + PopI, PushI, InlineNone, IObjModel, 1, 0xFF, 0x95, NEXT);
    OPDEF("ldelem.i8", PopRef + PopI, PushI8, InlineNone, IObjModel, 1, 0xFF, 0x96, NEXT);
    OPDEF("ldelem.i", PopRef + PopI, PushI, InlineNone, IObjModel, 1, 0xFF, 0x97, NEXT);
    OPDEF("ldelem.r4", PopRef + PopI, PushR4, InlineNone, IObjModel, 1, 0xFF, 0x98, NEXT);
    OPDEF("ldelem.r8", PopRef + PopI, PushR8, InlineNone, IObjModel, 1, 0xFF, 0x99, NEXT);
    OPDEF("ldelem.ref", PopRef + PopI, PushRef, InlineNone, IObjModel, 1, 0xFF, 0x9A, NEXT);
    OPDEF("stelem.i", PopRef + PopI + PopI, Push0, InlineNone, IObjModel, 1, 0xFF, 0x9B, NEXT);
    OPDEF("stelem.i1", PopRef + PopI + PopI, Push0, InlineNone, IObjModel, 1, 0xFF, 0x9C, NEXT);
    OPDEF("stelem.i2", PopRef + PopI + PopI, Push0, InlineNone, IObjModel, 1, 0xFF, 0x9D, NEXT);
    OPDEF("stelem.i4", PopRef + PopI + PopI, Push0, InlineNone, IObjModel, 1, 0xFF, 0x9E, NEXT);
    OPDEF("stelem.i8", PopRef + PopI + PopI8, Push0, InlineNone, IObjModel, 1, 0xFF, 0x9F, NEXT);
    OPDEF("stelem.r4", PopRef + PopI + PopR4, Push0, InlineNone, IObjModel, 1, 0xFF, 0xA0, NEXT);
    OPDEF("stelem.r8", PopRef + PopI + PopR8, Push0, InlineNone, IObjModel, 1, 0xFF, 0xA1, NEXT);
    OPDEF("stelem.ref", PopRef + PopI + PopRef, Push0, InlineNone, IObjModel, 1, 0xFF, 0xA2, NEXT);
    OPDEF("ldelem", PopRef + PopI, Push1, InlineType, IObjModel, 1, 0xFF, 0xA3, NEXT);
    OPDEF("stelem", PopRef + PopI + Pop1, Push0, InlineType, IObjModel, 1, 0xFF, 0xA4, NEXT);
    OPDEF("unbox.any", PopRef, Push1, InlineType, IObjModel, 1, 0xFF, 0xA5, NEXT);
    OPDEF("conv.ovf.i1", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xB3, NEXT);
    OPDEF("conv.ovf.u1", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xB4, NEXT);
    OPDEF("conv.ovf.i2", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xB5, NEXT);
    OPDEF("conv.ovf.u2", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xB6, NEXT);
    OPDEF("conv.ovf.i4", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xB7, NEXT);
    OPDEF("conv.ovf.u4", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xB8, NEXT);
    OPDEF("conv.ovf.i8", Pop1, PushI8, InlineNone, IPrimitive, 1, 0xFF, 0xB9, NEXT);
    OPDEF("conv.ovf.u8", Pop1, PushI8, InlineNone, IPrimitive, 1, 0xFF, 0xBA, NEXT);
    OPDEF("refanyval", Pop1, PushI, InlineType, IPrimitive, 1, 0xFF, 0xC2, NEXT);
    OPDEF("ckfinite", Pop1, PushR8, InlineNone, IPrimitive, 1, 0xFF, 0xC3, NEXT);
    OPDEF("mkrefany", PopI, Push1, InlineType, IPrimitive, 1, 0xFF, 0xC6, NEXT);
    OPDEF("ldtoken", Pop0, PushI, InlineTok, IPrimitive, 1, 0xFF, 0xD0, NEXT);
    OPDEF("conv.u2", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xD1, NEXT);
    OPDEF("conv.u1", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xD2, NEXT);
    OPDEF("conv.i", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xD3, NEXT);
    OPDEF("conv.ovf.i", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xD4, NEXT);
    OPDEF("conv.ovf.u", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xD5, NEXT);
    OPDEF("add.ovf", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0xD6, NEXT);
    OPDEF("add.ovf.un", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0xD7, NEXT);
    OPDEF("mul.ovf", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0xD8, NEXT);
    OPDEF("mul.ovf.un", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0xD9, NEXT);
    OPDEF("sub.ovf", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0xDA, NEXT);
    OPDEF("sub.ovf.un", Pop1 + Pop1, Push1, InlineNone, IPrimitive, 1, 0xFF, 0xDB, NEXT);
    OPDEF("endfinally", Pop0, Push0, InlineNone, IPrimitive, 1, 0xFF, 0xDC, RETURN);
    OPDEF("leave", Pop0, Push0, InlineBrTarget, IPrimitive, 1, 0xFF, 0xDD, BRANCH);
    OPDEF("leave.s", Pop0, Push0, ShortInlineBrTarget, IPrimitive, 1, 0xFF, 0xDE, BRANCH);
    OPDEF("stind.i", PopI + PopI, Push0, InlineNone, IPrimitive, 1, 0xFF, 0xDF, NEXT);
    OPDEF("conv.u", Pop1, PushI, InlineNone, IPrimitive, 1, 0xFF, 0xE0, NEXT);

    OPDEF("arglist", Pop0, PushI, InlineNone, IPrimitive, 2, 0xFE, 0x00, NEXT);
    OPDEF("ceq", Pop1 + Pop1, PushI, InlineNone, IPrimitive, 2, 0xFE, 0x01, NEXT);
    OPDEF("cgt", Pop1 + Pop1, PushI, InlineNone, IPrimitive, 2, 0xFE, 0x02, NEXT);
    OPDEF("cgt.un", Pop1 + Pop1, PushI, InlineNone, IPrimitive, 2, 0xFE, 0x03, NEXT);
    OPDEF("clt", Pop1 + Pop1, PushI, InlineNone, IPrimitive, 2, 0xFE, 0x04, NEXT);
    OPDEF("clt.un", Pop1 + Pop1, PushI, InlineNone, IPrimitive, 2, 0xFE, 0x05, NEXT);
    OPDEF("ldftn", Pop0, PushI, InlineMethod, IPrimitive, 2, 0xFE, 0x06, NEXT);
    OPDEF("ldvirtftn", PopRef, PushI, InlineMethod, IPrimitive, 2, 0xFE, 0x07, NEXT);
    OPDEF("ldarg", Pop0, Push1, InlineVar, IPrimitive, 2, 0xFE, 0x09, NEXT);
    OPDEF("ldarga", Pop0, PushI, InlineVar, IPrimitive, 2, 0xFE, 0x0A, NEXT);
    OPDEF("starg", Pop1, Push0, InlineVar, IPrimitive, 2, 0xFE, 0x0B, NEXT);
    OPDEF("ldloc", Pop0, Push1, InlineVar, IPrimitive, 2, 0xFE, 0x0C, NEXT);
    OPDEF("ldloca", Pop0, PushI, InlineVar, IPrimitive, 2, 0xFE, 0x0D, NEXT);
    OPDEF("stloc", Pop1, Push0, InlineVar, IPrimitive, 2, 0xFE, 0x0E, NEXT);
    OPDEF("localloc", PopI, PushI, InlineNone, IPrimitive, 2, 0xFE, 0x0F, NEXT);
    OPDEF("endfilter", PopI, Push0, InlineNone, IPrimitive, 2, 0xFE, 0x11, RETURN);
    OPDEF("unaligned.", Pop0, Push0, ShortInlineI, IPrefix, 2, 0xFE, 0x12, META);
    OPDEF("volatile.", Pop0, Push0, InlineNone, IPrefix, 2, 0xFE, 0x13, META);
    OPDEF("tail.", Pop0, Push0, InlineNone, IPrefix, 2, 0xFE, 0x14, META);
    OPDEF("initobj", PopI, Push0, InlineType, IObjModel, 2, 0xFE, 0x15, NEXT);
    OPDEF("constrained.", Pop0, Push0, InlineType, IPrefix, 2, 0xFE, 0x16, META);
    OPDEF("cpblk", PopI + PopI + PopI, Push0, InlineNone, IPrimitive, 2, 0xFE, 0x17, NEXT);
    OPDEF("initblk", PopI + PopI + PopI, Push0, InlineNone, IPrimitive, 2, 0xFE, 0x18, NEXT);
    //TODO(SPEC) III.2.2 no. is marked as unused in OPDEF, but maybe it should be:
    // OPDEF("no.", Pop0, Push0, ShortInlineI, IPrefix, 2, 0xFE, 0x19, META);
    // OPDEF("unused", Pop0, Push0, InlineNone, IPrimitive, 2, 0xFE, 0x19, NEXT)
    OPDEF("rethrow", Pop0, Push0, InlineNone, IObjModel, 2, 0xFE, 0x1A, THROW);
    OPDEF("sizeof", Pop0, PushI, InlineType, IPrimitive, 2, 0xFE, 0x1C, NEXT);
    OPDEF("refanytype", Pop1, PushI, InlineNone, IPrimitive, 2, 0xFE, 0x1D, NEXT);
    OPDEF("readonly.", Pop0, Push0, InlineNone, IPrefix, 2, 0xFE, 0x1E, META);
  }

  static Dictionary<byte, OpCode> firstOps = new Dictionary<byte, OpCode>();
  static Dictionary<byte, OpCode> secondOps = new Dictionary<byte, OpCode>();

  static void OPDEF(string name, string stackPop, string stackPush, string opParams, string _kind, int length, byte b1, byte b2, string controlFlow) {
    var op = new OpCode(name, stackPop, stackPush, opParams, controlFlow);
    if (length == 1 && b1 == 0xFF) {
      firstOps.Add(b2, op);
    } else if (length == 2 && b1 == 0xFE) {
      secondOps.Add(b2, op);
    } else {
      throw new InvalidOperationException($"{length} and {b1}");
    }
  }

  public static OpCode FirstByte(byte b) => firstOps[b];
  public static OpCode SecondByte(byte b) => secondOps[b];

  public string name;
  public string stackPop;
  public string stackPush;
  public string opParams;
  public string controlFlow;
  public string ecma;
  OpCode(string name, string stackPop, string stackPush, string opParams, string controlFlow) {
    this.name = name;
    this.stackPop = stackPop;
    this.stackPush = stackPush;
    this.opParams = opParams;
    this.controlFlow = controlFlow;

    this.ecma = ECMA(name);
  }

  static string ECMA(string name) => name switch {
    "constrained" => "III.2.1",
    "no" => "III.2.2",
    "readonly" => "III.2.3",
    "tail" => "III.2.4",
    "unaligned" => "III.2.5",
    "volatile" => "III.2.6",
    "add" => "III.3.1",
    "add.ovf" => "III.3.2",
    "and" => "III.3.3",
    "arglist" => "III.3.4",
    "beq" => "III.3.5",
    "bge" => "III.3.6",
    "bge.un" => "III.3.7",
    "bgt" => "III.3.8",
    "bgt.un" => "III.3.9",
    "ble" => "III.3.10",
    "ble.un" => "III.3.11",
    "blt" => "III.3.12",
    "blt.un" => "III.3.13",
    "bne.un" => "III.3.14",
    "br" => "III.3.15",
    "break" => "III.3.16",
    "brfalse" => "III.3.17",
    "brtrue" => "III.3.18",
    "call" => "III.3.19",
    "calli" => "III.3.20",
    "ceq" => "III.3.21",
    "cgt" => "III.3.22",
    "cgt.un" => "III.3.23",
    "ckfinite" => "III.3.24",
    "clt" => "III.3.25",
    "clt.un" => "III.3.26",
    "conv" => "III.3.27",
    "conv.ovf" => "III.3.28",
    "conv.ovf.i1.un" => "III.3.29",
    "conv.ovf.i2.un" => "III.3.29",
    "conv.ovf.i4.un" => "III.3.29",
    "conv.ovf.i8.un" => "III.3.29",
    "conv.ovf.u1.un" => "III.3.29",
    "conv.ovf.u2.un" => "III.3.29",
    "conv.ovf.u4.un" => "III.3.29",
    "conv.ovf.u8.un" => "III.3.29",
    "conv.ovf.i.un" => "III.3.29",
    "conv.ovf.u.un" => "III.3.29",
    "cpblk" => "III.3.30",
    "div" => "III.3.31",
    "div.un" => "III.3.32",
    "dup" => "III.3.33",
    "endfilter" => "III.3.34",
    "endfinally" => "III.3.35",
    "initblk" => "III.3.36",
    "jmp" => "III.3.37",
    "ldarg" => "III.3.38",
    "ldarga" => "III.3.39",
    "ldc.i4.m1" => "III.3.40",
    "ldc.i4" => "III.3.40",
    "ldc.i8" => "III.3.40",
    "ldc.r4" => "III.3.40",
    "ldc.r8" => "III.3.40",
    "ldftn" => "III.3.41",
    "ldind" => "III.3.42",
    "ldloc" => "III.3.43",
    "ldloca" => "III.3.44",
    "ldnull" => "III.3.45",
    "leave" => "III.3.46",
    "localloc" => "III.3.47",
    "mul" => "III.3.48",
    "mul.ovf" => "III.3.49",
    "neg" => "III.3.50",
    "nop" => "III.3.51",
    "not" => "III.3.52",
    "or" => "III.3.53",
    "pop" => "III.3.54",
    "rem" => "III.3.55",
    "rem.un" => "III.3.56",
    "ret" => "III.3.57",
    "shl" => "III.3.58",
    "shr" => "III.3.59",
    "shr.un" => "III.3.60",
    "starg" => "III.3.61",
    "stind" => "III.3.62",
    "stloc" => "III.3.63",
    "sub" => "III.3.64",
    "sub.ovf" => "III.3.65",
    "switch" => "III.3.66",
    "xor" => "III.3.67",
    "box" => "III.4.1",
    "callvirt" => "III.4.2",
    "castclass" => "III.4.3",
    "cpobj" => "III.4.4",
    "initobj" => "III.4.5",
    "isinst" => "III.4.6",
    "ldelem" => "III.4.7",
    "ldelem.i1" => "III.4.8",
    "ldelem.u1" => "III.4.8",
    "ldelem.i2" => "III.4.8",
    "ldelem.u2" => "III.4.8",
    "ldelem.i4" => "III.4.8",
    "ldelem.u4" => "III.4.8",
    "ldelem.i8" => "III.4.8",
    "ldelem.i" => "III.4.8",
    "ldelem.r4" => "III.4.8",
    "ldelem.r8" => "III.4.8",
    "ldelem.ref" => "III.4.8",
    "ldelema" => "III.4.9",
    "ldfld" => "III.4.10",
    "ldflda" => "III.4.11",
    "ldlen" => "III.4.12",
    "ldobj" => "III.4.13",
    "ldsfld" => "III.4.14",
    "ldsflda" => "III.4.15",
    "ldstr" => "III.4.16",
    "ldtoken" => "III.4.17",
    "ldvirtftn" => "III.4.18",
    "mkrefany" => "III.4.19",
    "newarr" => "III.4.20",
    "newobj" => "III.4.21",
    "refanytype" => "III.4.22",
    "refanyval" => "III.4.23",
    "rethrow" => "III.4.24",
    "sizeof" => "III.4.25",
    "stelem" => "III.4.26",
    "stelem.i1" => "III.4.27",
    "stelem.u1" => "III.4.27",
    "stelem.i2" => "III.4.27",
    "stelem.u2" => "III.4.27",
    "stelem.i4" => "III.4.27",
    "stelem.u4" => "III.4.27",
    "stelem.i8" => "III.4.27",
    "stelem.i" => "III.4.27",
    "stelem.r4" => "III.4.27",
    "stelem.r8" => "III.4.27",
    "stelem.ref" => "III.4.27",
    "stfld" => "III.4.28",
    "stobj" => "III.4.29",
    "stsfld" => "III.4.30",
    "throw" => "III.4.31",
    "unbox" => "III.4.32",
    "unbox.any" => "III.4.33",
    _ => name.Contains(".") ?
        // Recurse so i.e. conv.r8 lookup matches conv
        ECMA(name.Substring(0, name.LastIndexOf("."))) : 
        throw new InvalidOperationException(name)
  };
}
