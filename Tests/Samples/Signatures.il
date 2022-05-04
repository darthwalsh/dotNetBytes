.assembly extern mscorlib {}

.class SignatureTests {
.method static void Main() {
.entrypoint
ret
}

.method static void Dict() {
  ldnull 
  castclass class [mscorlib]System.Collections.Generic.Dictionary`2<int32, string>
  pop 
  ret 
}

// Examples from II.23.2.13
.method static void Array1() {
  ldnull 
  castclass int32[0...2]
  pop 
  ret 
}
.method static void Array2() {
  ldnull 
  castclass int32[,,,,,,]
  pop 
  ret 
}
.method static void Array3() {
  ldnull 
  castclass int32[0...3, 0...2,,,,]
  pop 
  ret 
}
.method static void Array4() {
  ldnull 
  castclass int32[1...2, 6...8]
  pop 
  ret 
}
.method static void Array5() {
  ldnull 
  castclass int32[5, 3...5, , ]
  pop 
  ret 
}

.method static void ArrayNested() {
  ldnull 
  castclass int32[5...8][,]
  pop 
  ret 
}

.method static void ArrayNegative() {
  ldnull 
  castclass int32[,-5...-2,-5...,-5...0]
  pop 
  ret 
}

.method static void Class() {
  ldnull 
  castclass class SignatureTests
  pop 
  ret 
}

.method static void ValueType() {
  ldc.i4.3
  newarr valuetype [mscorlib]System.ValueTuple
  pop 
  ret 
}

.method static void ModObject() {
  ldnull 
  castclass object modopt ([mscorlib]System.Text.StringBuilder)
  pop 
  ret 
}

.method static void ModObjectArray() {
  ldnull 
  castclass object modopt (string)[]
  pop 
  ret 
}

.method static void ModArrayObject() {
  ldnull 
  castclass object[] modopt (string)
  pop 
  ret 
}

.method static void SzArray() {
  ldnull 
  castclass int32[]
  pop 
  ret 
}

.method static void Ptr() {
  ldc.i4.3
  newarr int32*
  pop 
  ret 
}

.method static void VoidPtr() {
  ldc.i4.3
  newarr void*
  pop 
  ret 
}

.method static void Mod() {
  ldnull 
  castclass int32 modopt (char)[]
  pop 
  ret 
}

.method static void Mods() {
  ldnull 
  castclass int32 modopt (char) modreq (int32) modreq (int16) modopt(int16)[]
  pop 
  ret 
}

.method static void GenCast<T1>() {
  ldnull 
  castclass !!T1
  pop
  ret 
}

}

.class MethodSpecsTests {
.method static void MethodSpecs() {
  call void class MethodSpecsTests::Gen<int32, string> ()
  call void class MethodSpecsTests::Gen<string, class MethodSpecsTests> ()
  ret
}

.method static void Gen<T1, T2>() {
  ret 
}
}

.class LocalVarSigTests {
.method static void SimpleVar() {
  .locals init (char c, int64 pos)
  ret
}
.method static void ClassVar() {
  .locals init (string s, object o)
  ret
}
.method static void PinnedVar() {
  .locals init (char& pinned c, string pinned s)
  ret
}
.method static void ByRefVar() {
  .locals init (int64& p)
  ret
}
.method static void ModOptVar() {
  .locals init (int64 modopt (string) p)
  ret
}
.method static void TypeRefVar() {
  .locals init (typedref tr, native int x)
  ret
}
}
