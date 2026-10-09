// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal sealed class StructHeapMap
{
    internal const int InlineCapacity = 256;

    private readonly StructHeap[] inline;
    private readonly StructHeap[] overflow;
    private readonly int schemaLength;

    internal int InlineLength => inline?.Length ?? 0;
    internal int OverflowCount => overflow?.Length ?? 0;

    internal StructHeap this[int structIndex]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if ((uint)structIndex >= (uint)schemaLength) {
                return ThrowIndexOutOfRange();
            }
            if (structIndex < InlineCapacity) {
                var heaps = inline;
                return structIndex < (heaps?.Length ?? 0) ? heaps[structIndex] : null;
            }
            return FindOverflow(structIndex);
        }
    }

    internal StructHeapMap(int schemaLength)
    {
        this.schemaLength = schemaLength;
    }

    internal StructHeapMap(StructHeap heap, int schemaLength)
    {
        this.schemaLength = schemaLength;
        var structIndex = heap.structIndex;
        if (structIndex < InlineCapacity) {
            inline = new StructHeap[structIndex + 1];
            inline[structIndex] = heap;
            overflow = null;
            return;
        }

        inline = null;
        overflow = new [] { heap };
    }

    internal StructHeapMap(StructHeap[] heaps, int schemaLength)
    {
        this.schemaLength = schemaLength;
        var inlineLength = 0;
        var overflowCount = 0;
        foreach (var heap in heaps) {
            var structIndex = heap.structIndex;
            if (structIndex < InlineCapacity) {
                inlineLength = System.Math.Max(inlineLength, structIndex + 1);
            } else {
                overflowCount++;
            }
        }

        inline = inlineLength == 0 ? null : new StructHeap[inlineLength];
        overflow = overflowCount == 0 ? null : new StructHeap[overflowCount];

        var overflowPosition = 0;
        foreach (var heap in heaps) {
            var structIndex = heap.structIndex;
            if (structIndex < InlineCapacity) {
                inline[structIndex] = heap;
            } else {
                overflow[overflowPosition++] = heap;
            }
        }

        SortOverflow(overflow);
    }

    private static void SortOverflow(StructHeap[] heaps)
    {
        if (heaps == null) return;
        System.Array.Sort(heaps, static (left, right) => left.structIndex.CompareTo(right.structIndex));
    }

    private StructHeap FindOverflow(int structIndex)
    {
        var heaps = overflow;
        if (heaps == null) return null;

        var low = 0;
        var high = heaps.Length - 1;
        while (low <= high) {
            var middle = (low + high) >> 1;
            var heap = heaps[middle];
            if (heap.structIndex == structIndex) return heap;
            if (heap.structIndex < structIndex) low = middle + 1;
            else high = middle - 1;
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static StructHeap ThrowIndexOutOfRange()
    {
        throw new System.IndexOutOfRangeException();
    }
}
