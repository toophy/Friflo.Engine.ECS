// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Engine.ECS.Utils;

public partial struct WideBitSet
{
#if !NETCOREAPP3_0_OR_GREATER
    private readonly bool InlineIsDefault() => (l0 | l1 | l2 | l3) == 0;

    private readonly bool InlineEquals(in WideBitSet other)
    {
        return l0 == other.l0 && l1 == other.l1 && l2 == other.l2 && l3 == other.l3;
    }

    private readonly int InlineBitCount()
    {
        return NumberOfSetBits(l0) + NumberOfSetBits(l1) + NumberOfSetBits(l2) + NumberOfSetBits(l3);
    }
#endif

#if !NET7_0_OR_GREATER
    private readonly bool InlineHasAll(in WideBitSet bitSet)
    {
        return (l0 & bitSet.l0) == bitSet.l0 &&
               (l1 & bitSet.l1) == bitSet.l1 &&
               (l2 & bitSet.l2) == bitSet.l2 &&
               (l3 & bitSet.l3) == bitSet.l3;
    }

    private readonly bool InlineHasAny(in WideBitSet bitSet)
    {
        return ((l0 & bitSet.l0) | (l1 & bitSet.l1) | (l2 & bitSet.l2) | (l3 & bitSet.l3)) != 0;
    }

    private void InlineAdd(in WideBitSet right)
    {
        l0 |= right.l0;
        l1 |= right.l1;
        l2 |= right.l2;
        l3 |= right.l3;
    }

    private void InlineRemove(in WideBitSet right)
    {
        l0 &= ~right.l0;
        l1 &= ~right.l1;
        l2 &= ~right.l2;
        l3 &= ~right.l3;
    }

    private static WideBitSet InlineAdded(in WideBitSet left, in WideBitSet right)
    {
        return new WideBitSet {
            l0 = ~left.l0 & right.l0,
            l1 = ~left.l1 & right.l1,
            l2 = ~left.l2 & right.l2,
            l3 = ~left.l3 & right.l3,
        };
    }

    private static WideBitSet InlineRemoved(in WideBitSet left, in WideBitSet right)
    {
        return new WideBitSet {
            l0 = left.l0 & ~right.l0,
            l1 = left.l1 & ~right.l1,
            l2 = left.l2 & ~right.l2,
            l3 = left.l3 & ~right.l3,
        };
    }

    private static WideBitSet InlineChanged(in WideBitSet left, in WideBitSet right)
    {
        return new WideBitSet {
            l0 = left.l0 ^ right.l0,
            l1 = left.l1 ^ right.l1,
            l2 = left.l2 ^ right.l2,
            l3 = left.l3 ^ right.l3,
        };
    }

    private static WideBitSet InlineIntersect(in WideBitSet left, in WideBitSet right)
    {
        return new WideBitSet {
            l0 = left.l0 & right.l0,
            l1 = left.l1 & right.l1,
            l2 = left.l2 & right.l2,
            l3 = left.l3 & right.l3,
        };
    }
#endif

    internal static int NumberOfSetBits(long value)
    {
        value -= (value >> 1) & 0x5555555555555555;
        value = (value & 0x3333333333333333) + ((value >> 2) & 0x3333333333333333);
        return (int)((((value + (value >> 4)) & 0x0F0F0F0F0F0F0F0F) * 0x0101010101010101) >> 56);
    }

    internal static int TrailingZeroCount(long value)
    {
        if (value == 0) return 64;
        var count = 63;
        var shifted = value << 32;
        if (shifted != 0) { count -= 32; value = shifted; }
        shifted = value << 16;
        if (shifted != 0) { count -= 16; value = shifted; }
        shifted = value << 8;
        if (shifted != 0) { count -= 8; value = shifted; }
        shifted = value << 4;
        if (shifted != 0) { count -= 4; value = shifted; }
        shifted = value << 2;
        if (shifted != 0) { count -= 2; value = shifted; }
        return count - (int)((value << 1) >>> 63);
    }
}
