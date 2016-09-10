.assembly extern mscorlib {}

.data theInt = int32(123) 
.field public static int32 count at theInt 

.class Hello {
.method static void Main() {
.entrypoint                                                   // Main isn't automatically the entry method
ret
}
}