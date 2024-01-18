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

.method static void SzArrayClass() {
  ldnull 
  castclass class SignatureTests[]
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

.method static void PtrPtr() {
  ldc.i4.3
  newarr int32**
  pop 
  ret 
}


.method static void PtrMod() {
  ldc.i4.3
  newarr void modopt (char)*
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

.method static void Fnptr() {
  ldc.i4.3 newarr method void *(int32) pop
  ldc.i4.3 newarr method instance explicit char *() pop
  ldc.i4.3 newarr method instance void *() pop
  ldc.i4.3 newarr method vararg void *(method unmanaged fastcall void *(char, unsigned int32)) pop

  ret 
}

.method static void GenCast<T1>() {
  ldnull 
  castclass !!T1
  pop
  ret 
}
}

.class StandAloneMethodSigTests {
.method static int32 AddTen(int32) {
  ldarg.0
  ret
}
.method public static vararg void VarArgsMethod(int32 required) {
  ret
}

.method static void StandAloneMethodSigRunner()
{
  ldc.i4.1
  ldftn int32 StandAloneMethodSigTests::AddTen(int32)
  calli int32(int32)
  pop

  ldc.i4.1
  call vararg void StandAloneMethodSigTests::VarArgsMethod(int32, ...)

  ldc.i4.1
  ldc.i4.2
  call vararg void StandAloneMethodSigTests::VarArgsMethod(int32, ..., int32)

  ldftn vararg void StandAloneMethodSigTests::VarArgsMethod(int32, ..., uint32)
  ldc.i4.1
  ldc.i4.6
  calli vararg void(int32, ..., uint32)
  pop

  ret
}

}

.class FieldSigTests {
.field public static initonly int32 pointCount

.field private int32 fld = int32(42)
.field private int32 modopt (char) modreq (int32) customized

.data classInt = int32(123) // MAYBE parse the .sdata section in DLL where 123 is hardcoded
.field public static int32 classCount at classInt

.method static void FieldSigRunner()
{
  ldsfld int32 FieldSigTests::classCount
  pop
  ldsflda int32* FieldSigTests::classCount
  pop

  ret
}
}
.data globalInt = int32(123)
.field public static int32 globalCount at globalInt

.class MethodSpecsTests {
.method static void MethodSpecs() {
  call void class MethodSpecsTests::Gen<int32, string> (char)
  call void class MethodSpecsTests::Gen<string, class MethodSpecsTests> (char)
  ret
}

.method static void Gen<T1, T2>(char c) {
  ret 
}
}

.class PropertySigTests {
.method virtual hidebysig public specialname instance int32 get_Count() {
  ldc.i4.0
  ret
}
.method virtual hidebysig public specialname instance void set_Count(
    int32 newCount) {
  ret
} 
.method virtual hidebysig public instance void reset_Count() {
  ret
} 
.property int32 Count() {
  .get instance int32 PropertySigTests::get_Count()
  .set instance void PropertySigTests::set_Count(int32)
  .other instance void PropertySigTests::reset_Count()
}

.method private hidebysig specialname 
        instance default int32 get_Item (int32 i, char b)  cil managed {
  ldarg.1 
  ret 
}
.property instance int32 Item (int32, char) {
  .get instance default int32 PropertySigTests::get_Item (int32 i, char b) 
}

.method private hidebysig specialname 
        instance default int32 get_Item (char& i)  cil managed {
  ldc.i4.0 
  ret 
}
.method private hidebysig specialname 
        instance default void set_Item (char& i, int32 'value')  cil managed {
  ret 
}
.property instance int32 Item (char&) {
  .get instance default int32 PropertySigTests::get_Item (char& i) 
  .set instance default void PropertySigTests::set_Item (char& i, int32 'value') 
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
