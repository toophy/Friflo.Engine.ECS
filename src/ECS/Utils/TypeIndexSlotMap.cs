// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal sealed class TypeIndexSlotMap<TSlot>
    where TSlot : class
{
    internal const int InlineCapacity = 256;

    private readonly int schemaLength;
    private TSlot[] inline;
    private OverflowEntry[] overflow;
    private int overflowCount;

    internal int InlineLength => inline?.Length ?? 0;
    internal int OverflowCount => overflowCount;

    internal TSlot this[int typeIndex]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ValidateIndex(typeIndex);
            if (typeIndex < InlineCapacity) {
                var slots = inline;
                return typeIndex < (slots?.Length ?? 0) ? slots[typeIndex] : null;
            }
            return GetOverflow(typeIndex);
        }
        set {
            ValidateIndex(typeIndex);
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            if (typeIndex < InlineCapacity) {
                SetInline(typeIndex, value);
                return;
            }
            SetOverflow(typeIndex, value);
        }
    }

    internal TypeIndexSlotMap(int schemaLength)
    {
        if (schemaLength <= 0) {
            throw new ArgumentOutOfRangeException(nameof(schemaLength), schemaLength, "Schema length must be positive.");
        }
        this.schemaLength = schemaLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateIndex(int typeIndex)
    {
        if ((uint)typeIndex >= (uint)schemaLength) {
            ThrowIndexOutOfRange();
        }
    }

    private void SetInline(int typeIndex, TSlot value)
    {
        var slots = inline;
        if (typeIndex >= (slots?.Length ?? 0)) {
            var maxLength = Math.Min(InlineCapacity, schemaLength);
            var newLength = slots?.Length ?? 0;
            newLength = newLength == 0 ? Math.Min(4, maxLength) : newLength;
            while (newLength <= typeIndex) {
                newLength = Math.Min(maxLength, newLength * 2);
            }
            ArrayUtils.Resize(ref inline, newLength);
        }
        inline[typeIndex] = value;
    }

    private TSlot GetOverflow(int typeIndex)
    {
        var position = FindOverflow(typeIndex);
        return position >= 0 ? overflow[position].value : null;
    }

    private void SetOverflow(int typeIndex, TSlot value)
    {
        var position = FindOverflow(typeIndex);
        if (position >= 0) {
            overflow[position].value = value;
            return;
        }

        var insertion = ~position;
        if (overflowCount == (overflow?.Length ?? 0)) {
            var newLength = overflowCount == 0 ? 4 : overflowCount * 2;
            ArrayUtils.Resize(ref overflow, newLength, overflowCount);
        }
        if (insertion < overflowCount) {
            Array.Copy(overflow, insertion, overflow, insertion + 1, overflowCount - insertion);
        }
        overflow[insertion] = new OverflowEntry(typeIndex, value);
        overflowCount++;
    }

    private int FindOverflow(int typeIndex)
    {
        var low = 0;
        var high = overflowCount - 1;
        while (low <= high) {
            var middle = (low + high) >> 1;
            var currentIndex = overflow[middle].typeIndex;
            if (currentIndex == typeIndex) return middle;
            if (currentIndex < typeIndex) low = middle + 1;
            else high = middle - 1;
        }
        return ~low;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }

    private struct OverflowEntry
    {
        internal readonly int typeIndex;
        internal TSlot value;

        internal OverflowEntry(int typeIndex, TSlot value)
        {
            this.typeIndex = typeIndex;
            this.value = value;
        }
    }
}
