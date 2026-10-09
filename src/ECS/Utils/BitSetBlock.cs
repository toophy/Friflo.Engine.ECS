// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Engine.ECS.Utils;

internal readonly struct BitSetBlock
{
    internal readonly byte Index;
    internal readonly long L0;
    internal readonly long L1;
    internal readonly long L2;
    internal readonly long L3;

    internal bool IsDefault => (L0 | L1 | L2 | L3) == 0;

    internal BitSetBlock(byte index, long l0, long l1, long l2, long l3)
    {
        Index = index;
        L0 = l0;
        L1 = l1;
        L2 = l2;
        L3 = l3;
    }

    internal bool Has(int index)
    {
        return index switch {
            < 64  => (L0 & (1L << index)) != 0,
            < 128 => (L1 & (1L << (index - 64))) != 0,
            < 192 => (L2 & (1L << (index - 128))) != 0,
            _     => (L3 & (1L << (index - 192))) != 0,
        };
    }

    internal BitSetBlock SetBit(int index)
    {
        return index switch {
            < 64  => new BitSetBlock(Index, L0 | (1L << index), L1, L2, L3),
            < 128 => new BitSetBlock(Index, L0, L1 | (1L << (index - 64)), L2, L3),
            < 192 => new BitSetBlock(Index, L0, L1, L2 | (1L << (index - 128)), L3),
            _     => new BitSetBlock(Index, L0, L1, L2, L3 | (1L << (index - 192))),
        };
    }

    internal BitSetBlock ClearBit(int index)
    {
        return index switch {
            < 64  => new BitSetBlock(Index, L0 & ~(1L << index), L1, L2, L3),
            < 128 => new BitSetBlock(Index, L0, L1 & ~(1L << (index - 64)), L2, L3),
            < 192 => new BitSetBlock(Index, L0, L1, L2 & ~(1L << (index - 128)), L3),
            _     => new BitSetBlock(Index, L0, L1, L2, L3 & ~(1L << (index - 192))),
        };
    }

    internal bool HasAll(in BitSetBlock other)
    {
        return (L0 & other.L0) == other.L0 &&
               (L1 & other.L1) == other.L1 &&
               (L2 & other.L2) == other.L2 &&
               (L3 & other.L3) == other.L3;
    }

    internal bool HasAny(in BitSetBlock other)
    {
        return ((L0 & other.L0) | (L1 & other.L1) | (L2 & other.L2) | (L3 & other.L3)) != 0;
    }

    internal bool Equals(in BitSetBlock other)
    {
        return Index == other.Index &&
               L0 == other.L0 &&
               L1 == other.L1 &&
               L2 == other.L2 &&
               L3 == other.L3;
    }

    internal long GetWord(int index)
    {
        return index switch {
            0 => L0,
            1 => L1,
            2 => L2,
            _ => L3,
        };
    }

    internal static BitSetBlock Add(in BitSetBlock left, in BitSetBlock right)
    {
        return new BitSetBlock(left.Index, left.L0 | right.L0, left.L1 | right.L1, left.L2 | right.L2, left.L3 | right.L3);
    }

    internal static BitSetBlock Remove(in BitSetBlock left, in BitSetBlock right)
    {
        return new BitSetBlock(left.Index, left.L0 & ~right.L0, left.L1 & ~right.L1, left.L2 & ~right.L2, left.L3 & ~right.L3);
    }

    internal static BitSetBlock Added(in BitSetBlock left, in BitSetBlock right)
    {
        return new BitSetBlock(left.Index, ~left.L0 & right.L0, ~left.L1 & right.L1, ~left.L2 & right.L2, ~left.L3 & right.L3);
    }

    internal static BitSetBlock Changed(in BitSetBlock left, in BitSetBlock right)
    {
        return new BitSetBlock(left.Index, left.L0 ^ right.L0, left.L1 ^ right.L1, left.L2 ^ right.L2, left.L3 ^ right.L3);
    }

    internal static BitSetBlock Intersect(in BitSetBlock left, in BitSetBlock right)
    {
        return new BitSetBlock(left.Index, left.L0 & right.L0, left.L1 & right.L1, left.L2 & right.L2, left.L3 & right.L3);
    }
}
