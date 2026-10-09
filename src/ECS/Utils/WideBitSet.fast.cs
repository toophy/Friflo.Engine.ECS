// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if NETCOREAPP3_0_OR_GREATER
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.Intrinsics;
#endif

namespace Friflo.Engine.ECS.Utils;

public partial struct WideBitSet
{
#if NETCOREAPP3_0_OR_GREATER
    private readonly Vector256<long> InlineVector => Unsafe.ReadUnaligned<Vector256<long>>(ref Unsafe.As<long, byte>(ref Unsafe.AsRef(in l0)));

    private readonly bool InlineIsDefault() => InlineVector.Equals(default);

    private readonly bool InlineEquals(in WideBitSet other) => InlineVector.Equals(other.InlineVector);

    private readonly int InlineBitCount()
    {
        return BitOperations.PopCount((ulong)l0) +
               BitOperations.PopCount((ulong)l1) +
               BitOperations.PopCount((ulong)l2) +
               BitOperations.PopCount((ulong)l3);
    }
#endif

#if NET7_0_OR_GREATER
    private readonly bool InlineHasAll(in WideBitSet bitSet)
    {
        var right = bitSet.InlineVector;
        return (InlineVector & right) == right;
    }

    private readonly bool InlineHasAny(in WideBitSet bitSet) => !(InlineVector & bitSet.InlineVector).Equals(default);

    private void InlineAdd(in WideBitSet right) => SetInlineVector(InlineVector | right.InlineVector);

    private void InlineRemove(in WideBitSet right) => SetInlineVector(InlineVector & ~right.InlineVector);

    private static WideBitSet InlineAdded(in WideBitSet left, in WideBitSet right)
    {
        return FromInlineVector(~left.InlineVector & right.InlineVector);
    }

    private static WideBitSet InlineRemoved(in WideBitSet left, in WideBitSet right)
    {
        return FromInlineVector(left.InlineVector & ~right.InlineVector);
    }

    private static WideBitSet InlineChanged(in WideBitSet left, in WideBitSet right)
    {
        return FromInlineVector(left.InlineVector ^ right.InlineVector);
    }

    private static WideBitSet InlineIntersect(in WideBitSet left, in WideBitSet right)
    {
        return FromInlineVector(left.InlineVector & right.InlineVector);
    }

    private void SetInlineVector(Vector256<long> value)
    {
        Unsafe.WriteUnaligned(ref Unsafe.As<long, byte>(ref l0), value);
    }

    private static WideBitSet FromInlineVector(Vector256<long> value)
    {
        return new WideBitSet {
            l0 = value.GetElement(0),
            l1 = value.GetElement(1),
            l2 = value.GetElement(2),
            l3 = value.GetElement(3),
        };
    }
#endif
}
