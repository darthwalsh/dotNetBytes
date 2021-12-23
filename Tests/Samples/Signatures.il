.assembly extern mscorlib {}

.class Hello {
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

// .method static void Array1() {
//   ldnull 
//   castclass int32[0...2]
//   pop 
//   ret 
// }

// .method static void Array2() {
//   ldnull 
//   castclass int32[,,,,,,]
//   pop 
//   ret 
// }

// .method static void ArrayNested() {
//   ldnull 
//   castclass int32[0...2][,]
//   pop 
//   ret 
// }

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

}

