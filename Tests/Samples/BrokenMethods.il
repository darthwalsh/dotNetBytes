.assembly extern mscorlib {}

.class BrokenMethods {
.method static void Main() {
.entrypoint
ret
}

.method static int32 WorkingControlFlow(int32 n)
{
  ldarg.0
  switch (L4, L2)
  br.s DEFAULT_

  L2: ldc.i4.0
  ret
  L4: ldc.i4.1
  ret
  DEFAULT_: ldc.i4.m1
  ret
}

.method static void BrokenBranch()
{
  br.s 10
}

.method static void WithinBranch()
{
  br.s -1
}

.method static void BrokenSwitch()
{
  ldc.i4.m1
  switch (10)
}

.method static void StackUnderflow()
{
  beq 0
  ret
}

.method static void StackOverMax()
{
  .maxstack 1
  ldc.i4.m1
  ldc.i4.m1
  pop
  pop
  ret
}

.method static int32 RetEmpty()
{
  ret
}

.method static void RetExtra()
{
  ldc.i4.m1
  ret
}
}
