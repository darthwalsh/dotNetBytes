using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class AssemblyBytesTests
    {
        [TestMethod]
        public void TestExample()
        {
            Run(File.OpenRead(@"C:\code\dotNetBytes\view\Program.dat"));
        }

        static void Run(Stream s)
        {
            //TODO not valid? s = new ForwardOnlyStream(s);

            var assm = new AssemblyBytes(s);

            assm.Node.CallBack(AssertChildrenDontOverlap);

            assm.Node.CallBack(AssertNoErrors);

            assm.Node.CallBack(AssertUniqueNames);
        }

        static void AssertChildrenDontOverlap(CodeNode node)
        {
            foreach (var o in node.Children.Zip(node.Children.Skip(1), (last, next) => new { last, next }))
            {
                Asserts.IsLessThanOrEqual(o.last.End, o.next.Start);
            }
        }

        static void AssertNoErrors(CodeNode node)
        {
            string error = node.Errors.FirstOrDefault();
            Assert.IsNull(error, error);
        }

        static void AssertUniqueNames(CodeNode node)
        {
            string name = node.Children.GroupBy(c => c.Name).Where(g => g.Count() > 1).FirstOrDefault()?.Key;
            Assert.IsNull(name, name);
        }
    }

    class ForwardOnlyStream : DelegatingStream
    {
        public ForwardOnlyStream(Stream stream)
            : base(stream)
        { }

        public override long Position
        {
            get
            {
                return base.Position;
            }

            set
            {
                if (value < Position)
                    Assert.Fail($"Not allowed: set from {value} to {Position}");

                base.Position = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long relative;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    relative = 0;
                    break;
                case SeekOrigin.Current:
                    relative = Position;
                    break;
                case SeekOrigin.End:
                    relative = Length;
                    break;
                default:
                    throw new InvalidOperationException(origin.ToString());
            }

            long newPos = offset + relative;

            if (offset < Position)
                Assert.Fail($"Not allowed: ${offset} < ${Position}");

            return base.Seek(offset, origin);
        }
    }
}
