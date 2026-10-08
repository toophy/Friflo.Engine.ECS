using Friflo.Engine.ECS.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable IntVariableOverflowInUncheckedContext
// ReSharper disable InconsistentNaming
namespace Internal.ECS {

public static class Test_BitSet
{
    [Test]
    public static void Test_BitSet_Overflow_SetOperations()
    {
        var left = new BitSet(new [] { 1, 256, 512, 1024 });
        var right = new BitSet(new [] { 2, 256, 768, 1024 });

        var union = BitSet.Add(left, right);
        var removed = BitSet.Remove(left, right);
        var added = BitSet.Added(left, right);
        var changed = BitSet.Changed(left, right);
        var intersect = BitSet.Intersect(left, right);

        CollectionAssert.AreEqual(new [] { 1, 2, 256, 512, 768, 1024 }, ToArray(union));
        CollectionAssert.AreEqual(new [] { 1, 512 }, ToArray(removed));
        CollectionAssert.AreEqual(new [] { 2, 768 }, ToArray(added));
        CollectionAssert.AreEqual(new [] { 1, 2, 512, 768 }, ToArray(changed));
        CollectionAssert.AreEqual(new [] { 256, 1024 }, ToArray(intersect));
        Assert.IsTrue(union.HasAll(left));
        Assert.IsTrue(union.HasAll(right));
        Assert.IsTrue(left.HasAny(right));
        Assert.AreNotEqual(new BitSet(new [] { 1, 256 }).HashCode(), new BitSet(new [] { 1, 512 }).HashCode());
    }

    [Test]
    public static void Test_BitSet_Overflow_NoOpOperationsReuseBlocks()
    {
        var left = new BitSet(new [] { 256, 512, 1024 });
        var subset = new BitSet(new [] { 256, 1024 });
        var disjoint = new BitSet(new [] { 768 });
        var originalBlocks = left.GetOverflowBlocks();

        left.Add(subset);
        Assert.AreSame(originalBlocks, left.GetOverflowBlocks());

        left.Remove(disjoint);
        Assert.AreSame(originalBlocks, left.GetOverflowBlocks());

        var intersect = BitSet.Intersect(left, subset);
        Assert.AreSame(subset.GetOverflowBlocks(), intersect.GetOverflowBlocks());

        var added = BitSet.Added(left, subset);
        Assert.IsNull(added.GetOverflowBlocks());
    }

    [Test]
    public static void Test_BitSet_Overflow_SetOperationsMatchHashSet()
    {
        var random = new Random(0x5a17);
        for (var iteration = 0; iteration < 100; iteration++) {
            var leftValues = CreateValues(random);
            var rightValues = CreateValues(random);
            var left = new BitSet(leftValues.ToArray());
            var right = new BitSet(rightValues.ToArray());

            var union = new HashSet<int>(leftValues);
            union.UnionWith(rightValues);
            AssertBits(union, BitSet.Add(left, right));

            var removed = new HashSet<int>(leftValues);
            removed.ExceptWith(rightValues);
            AssertBits(removed, BitSet.Remove(left, right));

            var added = new HashSet<int>(rightValues);
            added.ExceptWith(leftValues);
            AssertBits(added, BitSet.Added(left, right));

            var changed = new HashSet<int>(leftValues);
            changed.SymmetricExceptWith(rightValues);
            AssertBits(changed, BitSet.Changed(left, right));

            var intersect = new HashSet<int>(leftValues);
            intersect.IntersectWith(rightValues);
            AssertBits(intersect, BitSet.Intersect(left, right));

            Assert.AreEqual(leftValues.IsSupersetOf(rightValues), left.HasAll(right));
            Assert.AreEqual(leftValues.Overlaps(rightValues), left.HasAny(right));
        }
    }

    [Test]
    public static void Test_BitSet_shims()
    {
        ulong allBits = 0xffffffff_ffffffff;
        Assert.AreEqual(64, BitSet.NumberOfSetBits((long)allBits));
        
        Assert.AreEqual(0,  BitSet.NumberOfSetBits(0));
        
        Assert.AreEqual(32, BitSet.NumberOfSetBits(0x0f0f0f0f_0f0f0f0f));
        
        
        Assert.AreEqual(64,  BitSet.TrailingZeroCount(0));
        Assert.AreEqual( 0,  BitSet.TrailingZeroCount((long)allBits));
        Assert.AreEqual( 0,  BitSet.TrailingZeroCount(1));
        
        Assert.AreEqual(31,  BitSet.TrailingZeroCount(1  << 31));
        Assert.AreEqual(32,  BitSet.TrailingZeroCount(1L << 32));
        Assert.AreEqual(63,  BitSet.TrailingZeroCount(1L << 63));
        
        for (int n = 1; n < 64; n++) {
            long value = 1L << n;
            Assert.AreEqual(n,  BitSet.TrailingZeroCount(value));
        }
    }

    private static int[] ToArray(BitSet bits)
    {
        var result = new System.Collections.Generic.List<int>();
        foreach (var index in bits) result.Add(index);
        return result.ToArray();
    }

    private static HashSet<int> CreateValues(Random random)
    {
        var result = new HashSet<int>();
        for (var index = 0; index < 128; index++) {
            result.Add(random.Next(BitSet.MaxIndex + 1));
        }
        return result;
    }

    private static void AssertBits(HashSet<int> expected, BitSet actual)
    {
        CollectionAssert.AreEqual(expected.OrderBy(static value => value), ToArray(actual));
    }
}

}
