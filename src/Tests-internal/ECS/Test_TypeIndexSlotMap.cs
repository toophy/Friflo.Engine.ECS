using System;
using Friflo.Engine.ECS;
using NUnit.Framework;

namespace Internal.ECS {

public static class Test_TypeIndexSlotMap
{
    private sealed class Slot
    {
        internal readonly int Value;

        internal Slot(int value)
        {
            Value = value;
        }
    }

    [Test]
    public static void EmptyMap_DoesNotAllocateSlotsAndRejectsInvalidIndexes()
    {
        var map = new TypeIndexSlotMap<Slot>(512);

        Assert.AreEqual(0, map.InlineLength);
        Assert.AreEqual(0, map.OverflowCount);
        Assert.IsNull(map[0]);
        Assert.IsNull(map[255]);
        Assert.IsNull(map[256]);
        Assert.IsNull(map[511]);
        Assert.Throws<IndexOutOfRangeException>(() => _ = map[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => _ = map[512]);
        Assert.Throws<ArgumentNullException>(() => map[1] = null);
    }

    [Test]
    public static void InlineSlots_GrowGeometricallyAndPreserveValues()
    {
        var map = new TypeIndexSlotMap<Slot>(600);
        var slot3 = new Slot(3);
        var slot17 = new Slot(17);
        var slot255 = new Slot(255);

        map[3] = slot3;
        Assert.AreEqual(4, map.InlineLength);
        map[17] = slot17;
        Assert.AreEqual(32, map.InlineLength);
        map[255] = slot255;
        Assert.AreEqual(256, map.InlineLength);

        Assert.AreSame(slot3, map[3]);
        Assert.AreSame(slot17, map[17]);
        Assert.AreSame(slot255, map[255]);
        Assert.IsNull(map[16]);
    }

    [Test]
    public static void InlineSlots_RespectSmallSchemaLength()
    {
        var map = new TypeIndexSlotMap<Slot>(3);
        map[2] = new Slot(2);

        Assert.AreEqual(3, map.InlineLength);
        Assert.AreEqual(2, map[2].Value);
    }

    [Test]
    public static void OverflowSlots_AreInsertedSortedAndCanBeReplaced()
    {
        var map = new TypeIndexSlotMap<Slot>(ushort.MaxValue + 1);
        var slot256 = new Slot(256);
        var slot4096 = new Slot(4096);
        var slot65535 = new Slot(ushort.MaxValue);

        map[4096] = slot4096;
        map[ushort.MaxValue] = slot65535;
        map[256] = slot256;

        Assert.AreEqual(0, map.InlineLength);
        Assert.AreEqual(3, map.OverflowCount);
        Assert.AreSame(slot256, map[256]);
        Assert.AreSame(slot4096, map[4096]);
        Assert.AreSame(slot65535, map[ushort.MaxValue]);
        Assert.IsNull(map[257]);

        var replacement = new Slot(42);
        map[4096] = replacement;
        Assert.AreEqual(3, map.OverflowCount);
        Assert.AreSame(replacement, map[4096]);
    }
}

}
