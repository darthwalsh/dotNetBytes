using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    public static class Verify
    {
        public static void Run(Stream s)
        {
            //TODO not valid? s = new ForwardOnlyStream(s);

            var assm = new AssemblyBytes(s);

            assm.Node.CallBack(AssertChildrenDontOverlap);

            assm.Node.CallBack(AssertNoErrors);

            assm.Node.CallBack(AssertUniqueNames);

            assm.Node.CallBack(AssertLinkOrChildren);

            assm.Node.CallBack(AssertParentDifferentSizeThanChild);

            byte[] data;
            using (var memory = new MemoryStream())
            {
                s.Position = 0;
                s.CopyTo(memory);
                data = memory.ToArray();
            }
            assm.Node.CallBack(node => AssertInterestingBytesNotIgnored(node, data));
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
            Assert.IsNull(name, $"multiple {name} under {node.Name}");
        }

        static void AssertLinkOrChildren(CodeNode node)
        {
            if (node.LinkPath != null && node.Children.Any())
            {
                Assert.Fail(node.Name);
            }
        }

        static IEnumerable<string> exceptions = new [] { "TypeSpecs", "Methods" };
        static void AssertParentDifferentSizeThanChild(CodeNode node)
        {
            if (node.Children.Count == 1 && node.Start == node.Children.Single().Start && node.End == node.Children.Single().End)
            {
                if (exceptions.Any(sub => node.Name.Contains(sub)))
                {
                    return;
                }
                Assert.Fail(string.Join("\r\n", node));
            }
        }

        static void AssertInterestingBytesNotIgnored(CodeNode node, byte[] data)
        {
            if (!node.Children.Any())
            {
                return;
            }

            HashSet<int> childIncludes = new HashSet<int>();
            foreach (var child in node.Children)
            {
                childIncludes.UnionWith(new HashSet<int>(Enumerable.Range(child.Start, child.End - child.Start)));
            }

            foreach (var i in Enumerable.Range(node.Start, node.End - node.Start))
            {
                if (data[i] == 0 || childIncludes.Contains(i))
                    continue;

                Assert.Fail($"Interested byte 0x{data[i]:X} at 0x{i:X} was non-zero in node {node.Name}");
            }
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
