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

// .method static void Array() {
//   ldnull 
//   castclass int32[0...,0...,0...]
//   pop 
//   ret 
// }

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

