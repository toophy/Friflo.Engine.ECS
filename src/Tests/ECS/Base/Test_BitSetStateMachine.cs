using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Utils;
using NUnit.Framework;

namespace Tests.ECS.Base
{
public static class Test_BitSetStateMachine
{
    [Test]
    public static void PublicBitSetRetainsUnmanagedLayout()
    {
        Assert.IsFalse(RuntimeHelpers.IsReferenceOrContainsReferences<BitSet>());
        Assert.AreEqual(32, Marshal.SizeOf<BitSet>());
    }

    [Test]
    public static void TagsAcceptPublicBitSet()
    {
        var schema = EntityStore.GetEntitySchema();
        var bits = new BitSet(new[] { schema.TagTypeByType[typeof(TestTag)].TagIndex });
        var tags = new Tags(bits);
        Assert.AreEqual(1, tags.Count);
        Assert.IsTrue(tags.Has<TestTag>());
    }

    [TestCase(-1)] [TestCase(256)] [TestCase(65535)]
    [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
    public static void InvalidIndicesDoNotAliasValidBits(int index)
    {
        var bits = new BitSet(new[] { 0, 63, 192, 255 });
        var before = bits;
        Assert.Throws<ArgumentOutOfRangeException>(() => bits.SetBit(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => bits.ClearBit(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => bits.Has(index));
        Assert.AreEqual(before, bits);
    }

    [Test]
    public static void NullIndicesAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new BitSet(null));
    }

    [TestCase(17)] [TestCase(101)] [TestCase(8191)]
    public static void MutationsAndCopiesMatchIndependentSet(int seed)
    {
        var random = new Random(seed);
        var actual = new BitSet(new[] { 0, 63, 64, 127, 128, 191, 192, 255 });
        var expected = new HashSet<int>(new[] { 0, 63, 64, 127, 128, 191, 192, 255 });
        var trace = new StringBuilder();
        for (int step = 0; step < 500; step++) {
            int bit = random.Next(256);
            bool add = random.Next(2) == 0;
            trace.AppendLine($"{step}: bit={bit} add={add}");
            try {
                var snapshot = actual;
                var expectedSnapshot = new HashSet<int>(expected);
                if (add) {
                    actual.SetBit(bit);
                    expected.Add(bit);
                } else {
                    actual.ClearBit(bit);
                    expected.Remove(bit);
                }
                AssertSet(snapshot, expectedSnapshot);
                AssertSet(actual, expected);
                Assert.AreEqual(expected.IsSupersetOf(expectedSnapshot), actual.HasAll(snapshot));
                Assert.AreEqual(expected.Overlaps(expectedSnapshot), actual.HasAny(snapshot));
                Assert.IsTrue(actual.HasAll(actual));
                Assert.AreEqual(expected.Count != 0, actual.HasAny(actual));
                var rebuilt = new BitSet(expected.OrderByDescending(index => index).ToArray());
                Assert.IsTrue(actual.Equals(rebuilt));
                Assert.AreEqual(actual.HashCode(), rebuilt.HashCode());
            } catch (Exception error) {
                Assert.Fail($"seed={seed}, step={step}\n{trace}\n{error}");
            }
        }
        foreach (int bit in expected.ToArray()) actual.ClearBit(bit);
        AssertSet(actual, new HashSet<int>());
    }

    private static void AssertSet(BitSet actual, HashSet<int> expected)
    {
        Assert.AreEqual(expected.Count, actual.GetBitCount());
        var bits = new List<int>();
        foreach (int bit in actual) bits.Add(bit);
        CollectionAssert.AreEqual(expected.OrderBy(bit => bit), bits);
        for (int bit = 0; bit < 256; bit++) Assert.AreEqual(expected.Contains(bit), actual.Has(bit));
    }
}
}
