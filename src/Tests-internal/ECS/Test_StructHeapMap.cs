using System;
using Friflo.Engine.ECS;
using NUnit.Framework;

namespace Internal.ECS {

public static class Test_StructHeapMap
{
    private struct TestComponent { }

    [Test]
    public static void EmptyMap_ReturnsNullWithinSchemaAndRejectsInvalidIndexes()
    {
        var map = new StructHeapMap(512);

        Assert.AreEqual(0, map.InlineLength);
        Assert.AreEqual(0, map.OverflowCount);
        Assert.IsNull(map[0]);
        Assert.IsNull(map[255]);
        Assert.IsNull(map[256]);
        Assert.IsNull(map[511]);
        Assert.Throws<IndexOutOfRangeException>(() => _ = map[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => _ = map[512]);
    }

    [Test]
    public static void DefaultArchetypes_ShareEmptyMap()
    {
        var firstStore = new EntityStore();
        var secondStore = new EntityStore();

        Assert.AreSame(firstStore.defaultArchetype.heapMap, secondStore.defaultArchetype.heapMap);
        Assert.AreEqual(0, firstStore.defaultArchetype.heapMap.InlineLength);
        Assert.AreEqual(0, firstStore.defaultArchetype.heapMap.OverflowCount);
    }

    [Test]
    public static void MixedMap_ResolvesInlineAndSortedOverflowHeaps()
    {
        var heap3 = new StructHeapGen<TestComponent>(3);
        var heap255 = new StructHeapGen<TestComponent>(255);
        var heap256 = new StructHeapGen<TestComponent>(256);
        var heap257 = new StructHeapGen<TestComponent>(257);
        var heap65535 = new StructHeapGen<TestComponent>(ushort.MaxValue);
        var source = new StructHeap[] { heap65535, heap255, heap257, heap3, heap256 };

        var map = new StructHeapMap(source, ushort.MaxValue + 1);

        Assert.AreEqual(256, map.InlineLength);
        Assert.AreEqual(3, map.OverflowCount);
        Assert.AreSame(heap3, map[3]);
        Assert.AreSame(heap255, map[255]);
        Assert.AreSame(heap256, map[256]);
        Assert.AreSame(heap257, map[257]);
        Assert.AreSame(heap65535, map[ushort.MaxValue]);
        Assert.IsNull(map[0]);
        Assert.IsNull(map[254]);
        Assert.IsNull(map[258]);
        Assert.IsNull(map[ushort.MaxValue - 1]);
    }

    [Test]
    public static void HighOnlyMap_DoesNotAllocateInlineLookup()
    {
        var heap256 = new StructHeapGen<TestComponent>(256);
        var heap4096 = new StructHeapGen<TestComponent>(4096);

        var map = new StructHeapMap(new StructHeap[] { heap4096, heap256 }, 4097);

        Assert.AreEqual(0, map.InlineLength);
        Assert.AreEqual(2, map.OverflowCount);
        Assert.AreSame(heap256, map[256]);
        Assert.AreSame(heap4096, map[4096]);
    }

    [Test]
    public static void Map_DoesNotRetainCallerArrayStructure()
    {
        var heap1 = new StructHeapGen<TestComponent>(1);
        var heap300 = new StructHeapGen<TestComponent>(300);
        var source = new StructHeap[] { heap1, heap300 };
        var map = new StructHeapMap(source, 301);

        source[0] = new StructHeapGen<TestComponent>(2);
        source[1] = null;

        Assert.AreSame(heap1, map[1]);
        Assert.AreSame(heap300, map[300]);
        Assert.IsNull(map[2]);
    }

    [Test]
    public static void SingleHeapMap_UsesOnlyRequiredStorageTier()
    {
        var lowHeap = new StructHeapGen<TestComponent>(17);
        var lowMap = new StructHeapMap(lowHeap, 400);
        Assert.AreEqual(18, lowMap.InlineLength);
        Assert.AreEqual(0, lowMap.OverflowCount);
        Assert.AreSame(lowHeap, lowMap[17]);

        var highHeap = new StructHeapGen<TestComponent>(399);
        var highMap = new StructHeapMap(highHeap, 400);
        Assert.AreEqual(0, highMap.InlineLength);
        Assert.AreEqual(1, highMap.OverflowCount);
        Assert.AreSame(highHeap, highMap[399]);
    }
}

}
