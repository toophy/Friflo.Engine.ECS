// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

// ReSharper disable ConvertToAutoProperty
namespace Friflo.Engine.ECS.Utils;

[StructLayout(LayoutKind.Explicit)]
public partial struct WideBitSet : IEquatable<WideBitSet>
{
    internal const int InlineCapacity = 256;
    internal const int MaxIndex = ushort.MaxValue;

    [FieldOffset(00)] internal long l0;
    [FieldOffset(08)] internal long l1;
    [FieldOffset(16)] internal long l2;
    [FieldOffset(24)] internal long l3;
    [FieldOffset(32)] private BitSetBlock[] overflow;

    public readonly          WideBitSetEnumerator    GetEnumerator() => new WideBitSetEnumerator(this);
    public readonly override string              ToString()      => AppendString(new StringBuilder()).ToString();
    public readonly override bool                Equals(object obj) => obj is WideBitSet other && Equals(other);
    public readonly override int                 GetHashCode()    => HashCode();

    internal WideBitSet(in BitSet value)
    {
        this = default;
        l0 = value.l0;
        l1 = value.l1;
        l2 = value.l2;
        l3 = value.l3;
    }

    public WideBitSet(int[] indices)
    {
        this = default;
        if (indices == null) throw new ArgumentNullException(nameof(indices));
        foreach (var index in indices) {
            SetBit(index);
        }
    }

    public readonly int HashCode()
    {
        const ulong offset = 1469598103934665603UL;
        const ulong prime = 1099511628211UL;

        var hash = offset;
        hash = (hash ^ (ulong)l0) * prime;
        hash = (hash ^ (ulong)l1) * prime;
        hash = (hash ^ (ulong)l2) * prime;
        hash = (hash ^ (ulong)l3) * prime;

        var blocks = overflow;
        if (blocks != null) {
            foreach (var block in blocks) {
                hash = (hash ^ block.Index) * prime;
                hash = (hash ^ (ulong)block.L0) * prime;
                hash = (hash ^ (ulong)block.L1) * prime;
                hash = (hash ^ (ulong)block.L2) * prime;
                hash = (hash ^ (ulong)block.L3) * prime;
            }
        }
        hash ^= hash >> 32;
        return unchecked((int)hash);
    }

    public void SetBit(int index)
    {
        ValidateIndex(index);
        if (index < InlineCapacity) {
            SetInlineBit(index);
            return;
        }

        var blockIndex = (byte)(index >> 8);
        var bitIndex = index & 255;
        var blocks = overflow;
        var position = FindBlock(blocks, blockIndex);
        if (position >= 0) {
            var current = blocks[position];
            if (current.Has(bitIndex)) return;
            var copy = (BitSetBlock[])blocks.Clone();
            copy[position] = current.SetBit(bitIndex);
            overflow = copy;
            return;
        }

        position = ~position;
        var result = new BitSetBlock[(blocks?.Length ?? 0) + 1];
        if (blocks != null) {
            Array.Copy(blocks, 0, result, 0, position);
            Array.Copy(blocks, position, result, position + 1, blocks.Length - position);
        }
        result[position] = new BitSetBlock(blockIndex, 0, 0, 0, 0).SetBit(bitIndex);
        overflow = result;
    }

    public void ClearBit(int index)
    {
        ValidateIndex(index);
        if (index < InlineCapacity) {
            ClearInlineBit(index);
            return;
        }

        var blocks = overflow;
        if (blocks == null) return;
        var blockIndex = (byte)(index >> 8);
        var position = FindBlock(blocks, blockIndex);
        if (position < 0) return;

        var current = blocks[position];
        var bitIndex = index & 255;
        if (!current.Has(bitIndex)) return;
        var changed = current.ClearBit(bitIndex);
        if (!changed.IsDefault) {
            var copy = (BitSetBlock[])blocks.Clone();
            copy[position] = changed;
            overflow = copy;
            return;
        }

        if (blocks.Length == 1) {
            overflow = null;
            return;
        }
        var result = new BitSetBlock[blocks.Length - 1];
        Array.Copy(blocks, 0, result, 0, position);
        Array.Copy(blocks, position + 1, result, position, blocks.Length - position - 1);
        overflow = result;
    }

    public readonly bool Has(int index)
    {
        ValidateIndex(index);
        if (index < InlineCapacity) return HasInlineBit(index);

        var blocks = overflow;
        if (blocks == null) return false;
        var position = FindBlock(blocks, (byte)(index >> 8));
        return position >= 0 && blocks[position].Has(index & 255);
    }

    internal readonly BitSetBlock[] GetOverflowBlocks() => overflow;

    private static void ValidateIndex(int index)
    {
        if ((uint)index <= MaxIndex) return;
        throw new ArgumentOutOfRangeException(nameof(index), index, $"Bit index must be in range [0, {MaxIndex}].");
    }

    private void SetInlineBit(int index)
    {
        switch (index) {
            case < 64:  l0 |= 1L << index;         return;
            case < 128: l1 |= 1L << (index - 64);  return;
            case < 192: l2 |= 1L << (index - 128); return;
            default:    l3 |= 1L << (index - 192); return;
        }
    }

    private void ClearInlineBit(int index)
    {
        switch (index) {
            case < 64:  l0 &= ~(1L << index);         return;
            case < 128: l1 &= ~(1L << (index - 64));  return;
            case < 192: l2 &= ~(1L << (index - 128)); return;
            default:    l3 &= ~(1L << (index - 192)); return;
        }
    }

    private readonly bool HasInlineBit(int index)
    {
        return index switch {
            < 64  => (l0 & (1L << index)) != 0,
            < 128 => (l1 & (1L << (index - 64))) != 0,
            < 192 => (l2 & (1L << (index - 128))) != 0,
            _     => (l3 & (1L << (index - 192))) != 0,
        };
    }

    private static int FindBlock(BitSetBlock[] blocks, byte blockIndex)
    {
        if (blocks == null) return ~0;
        var low = 0;
        var high = blocks.Length - 1;
        while (low <= high) {
            var middle = (low + high) >> 1;
            var current = blocks[middle].Index;
            if (current == blockIndex) return middle;
            if (current < blockIndex) low = middle + 1;
            else high = middle - 1;
        }
        return ~low;
    }

    private readonly StringBuilder AppendString(StringBuilder sb)
    {
        if (overflow != null || l3 != 0) {
            sb.Append($"{l0:x16} {l1:x16} {l2:x16} {l3:x16}");
        } else if (l2 != 0) {
            sb.Append($"{l0:x16} {l1:x16} {l2:x16}");
        } else if (l1 != 0) {
            sb.Append($"{l0:x16} {l1:x16}");
        } else {
            sb.Append($"{l0:x16}");
        }

        var blocks = overflow;
        if (blocks == null) return sb;
        foreach (var block in blocks) {
            sb.Append(" | ");
            sb.Append(block.Index);
            sb.Append(':');
            sb.Append($"{block.L0:x16} {block.L1:x16} {block.L2:x16} {block.L3:x16}");
        }
        return sb;
    }
}
