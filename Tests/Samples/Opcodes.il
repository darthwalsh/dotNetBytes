.assembly extern mscorlib {}

.class Hello {
.method static void Main() {
.entrypoint

ldc.i4.s 4
pop
ldc.i8 8
pop

ldc.r4 0xF10A7
pop
ldc.r8 0xD00B1E
pop

ret
}
}
