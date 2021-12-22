.assembly extern mscorlib {}

.class Hello {
.method static void Main() {
.entrypoint

ret
}

}

.class private auto ansi beforefieldinit ExtendsDict
  extends class [mscorlib]System.Collections.Generic.Dictionary`2<int32,string>
{
  .method public hidebysig specialname rtspecialname 
          instance default void '.ctor' ()  cil managed 
  {
.maxstack 8
IL_0000:  ldarg.0 
IL_0001:  call instance void class [mscorlib]System.Collections.Generic.Dictionary`2<int32, string>::'.ctor'()
IL_0006:  ret 
  } 

}
