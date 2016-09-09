.assembly extern mscorlib {}

.class Hello {
.method static void Main() {
.entrypoint                                                   // Main isn't automatically the entry method
ret
}
}