// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if NETCOREAPP3_0_OR_GREATER
    using System.Numerics;
    using System.Runtime.Intrinsics;
#endif

namespace Friflo.Engine.ECS.Utils;

public partial struct BitSet
{
#if NETCOREAPP3_0_OR_GREATER
    private readonly Vector256<long> InlineVector => Vector256.Create(l0, l1, l2, l3);

    private readonly bool InlineIsDefault() => InlineVector.Equals(default);

    private readonly bool InlineEquals(in BitSet other) => InlineVector.Equals(other.InlineVector);

    private readonly int InlineBitCount()
    {
        return BitOperations.PopCount((ulong)l0) +
               BitOperations.PopCount((ulong)l1) +
               BitOperations.PopCount((ulong)l2) +
               BitOperations.PopCount((ulong)l3);
    }
#endif

#if NET7_0_OR_GREATER
    private readonly bool InlineHasAll(in BitSet bitSet)
    {
        var right = bitSet.InlineVector;
        return (InlineVector & right) == right;
    }

    private readonly bool InlineHasAny(in BitSet bitSet) => !(InlineVector & bitSet.InlineVector).Equals(default);

    private void InlineAdd(in BitSet right) => SetInlineVector(InlineVector | right.InlineVector);

    private void InlineRemove(in BitSet right) => SetInlineVector(InlineVector & ~right.InlineVector);

    private static BitSet InlineAdded(in BitSet left, in BitSet right)
    {
        return FromInlineVector(~left.InlineVector & right.InlineVector);
    }

    private static BitSet InlineRemoved(in BitSet left, in BitSet right)
    {
        return FromInlineVector(left.InlineVector & ~right.InlineVector);
    }

    private static BitSet InlineChanged(in BitSet left, in BitSet right)
    {
        return FromInlineVector(left.InlineVector ^ right.InlineVector);
    }

    private static BitSet InlineIntersect(in BitSet left, in BitSet right)
    {
        return FromInlineVector(left.InlineVector & right.InlineVector);
    }

    private void SetInlineVector(Vector256<long> value)
    {
        l0 = value.GetElement(0);
        l1 = value.GetElement(1);
        l2 = value.GetElement(2);
        l3 = value.GetElement(3);
    }

    private static BitSet FromInlineVector(Vector256<long> value)
    {
        return new BitSet {
            l0 = value.GetElement(0),
            l1 = value.GetElement(1),
            l2 = value.GetElement(2),
            l3 = value.GetElement(3),
        };
    }
#endif
}
