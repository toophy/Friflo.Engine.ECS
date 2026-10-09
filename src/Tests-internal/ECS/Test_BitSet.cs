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
        var left = new WideBitSet(new [] { 1, 256, 512, 1024 });
        var right = new WideBitSet(new [] { 2, 256, 768, 1024 });

        var union = WideBitSet.Add(left, right);
        var removed = WideBitSet.Remove(left, right);
        var added = WideBitSet.Added(left, right);
        var changed = WideBitSet.Changed(left, right);
        var intersect = WideBitSet.Intersect(left, right);

        CollectionAssert.AreEqual(new [] { 1, 2, 256, 512, 768, 1024 }, ToArray(union));
        CollectionAssert.AreEqual(new [] { 1, 512 }, ToArray(removed));
        CollectionAssert.AreEqual(new [] { 2, 768 }, ToArray(added));
        CollectionAssert.AreEqual(new [] { 1, 2, 512, 768 }, ToArray(changed));
        CollectionAssert.AreEqual(new [] { 256, 1024 }, ToArray(intersect));
        Assert.IsTrue(union.HasAll(left));
        Assert.IsTrue(union.HasAll(right));
        Assert.IsTrue(left.HasAny(right));
        Assert.AreNotEqual(new WideBitSet(new [] { 1, 256 }).HashCode(), new WideBitSet(new [] { 1, 512 }).HashCode());
    }

    [Test]
    public static void Test_BitSet_Overflow_NoOpOperationsReuseBlocks()
    {
        var left = new WideBitSet(new [] { 256, 512, 1024 });
        var subset = new WideBitSet(new [] { 256, 1024 });
        var disjoint = new WideBitSet(new [] { 768 });
        var originalBlocks = left.GetOverflowBlocks();

        left.Add(subset);
        Assert.AreSame(originalBlocks, left.GetOverflowBlocks());

        left.Remove(disjoint);
        Assert.AreSame(originalBlocks, left.GetOverflowBlocks());

        var intersect = WideBitSet.Intersect(left, subset);
        Assert.AreSame(subset.GetOverflowBlocks(), intersect.GetOverflowBlocks());

        var added = WideBitSet.Added(left, subset);
        Assert.IsNull(added.GetOverflowBlocks());
    }

    [Test]
    public static void Test_BitSet_Overflow_SetOperationsMatchHashSet()
    {
        var random = new Random(0x5a17);
        for (var iteration = 0; iteration < 100; iteration++) {
            var leftValues = CreateValues(random);
            var rightValues = CreateValues(random);
            var left = new WideBitSet(leftValues.ToArray());
            var right = new WideBitSet(rightValues.ToArray());

            var union = new HashSet<int>(leftValues);
            union.UnionWith(rightValues);
            AssertBits(union, WideBitSet.Add(left, right));

            var removed = new HashSet<int>(leftValues);
            removed.ExceptWith(rightValues);
            AssertBits(removed, WideBitSet.Remove(left, right));

            var added = new HashSet<int>(rightValues);
            added.ExceptWith(leftValues);
            AssertBits(added, WideBitSet.Added(left, right));

            var changed = new HashSet<int>(leftValues);
            changed.SymmetricExceptWith(rightValues);
            AssertBits(changed, WideBitSet.Changed(left, right));

            var intersect = new HashSet<int>(leftValues);
            intersect.IntersectWith(rightValues);
            AssertBits(intersect, WideBitSet.Intersect(left, right));

            Assert.AreEqual(leftValues.IsSupersetOf(rightValues), left.HasAll(right));
            Assert.AreEqual(leftValues.Overlaps(rightValues), left.HasAny(right));
        }
    }

    [Test]
    public static void Test_BitSet_shims()
    {
        ulong allBits = 0xffffffff_ffffffff;
        Assert.AreEqual(64, WideBitSet.NumberOfSetBits((long)allBits));
        
        Assert.AreEqual(0,  WideBitSet.NumberOfSetBits(0));
        
        Assert.AreEqual(32, WideBitSet.NumberOfSetBits(0x0f0f0f0f_0f0f0f0f));
        
        
        Assert.AreEqual(64,  WideBitSet.TrailingZeroCount(0));
        Assert.AreEqual( 0,  WideBitSet.TrailingZeroCount((long)allBits));
        Assert.AreEqual( 0,  WideBitSet.TrailingZeroCount(1));
        
        Assert.AreEqual(31,  WideBitSet.TrailingZeroCount(1  << 31));
        Assert.AreEqual(32,  WideBitSet.TrailingZeroCount(1L << 32));
        Assert.AreEqual(63,  WideBitSet.TrailingZeroCount(1L << 63));
        
        for (int n = 1; n < 64; n++) {
            long value = 1L << n;
            Assert.AreEqual(n,  WideBitSet.TrailingZeroCount(value));
        }
    }

    private static int[] ToArray(WideBitSet bits)
    {
        var result = new System.Collections.Generic.List<int>();
        foreach (var index in bits) result.Add(index);
        return result.ToArray();
    }

    private static HashSet<int> CreateValues(Random random)
    {
        var result = new HashSet<int>();
        for (var index = 0; index < 128; index++) {
            result.Add(random.Next(WideBitSet.MaxIndex + 1));
        }
        return result;
    }

    private static void AssertBits(HashSet<int> expected, WideBitSet actual)
    {
        CollectionAssert.AreEqual(expected.OrderBy(static value => value), ToArray(actual));
    }
}

}
