// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Engine.ECS.Utils;

public partial struct WideBitSet
{
    private enum BlockOperation : byte
    {
        Add,
        Remove,
        Added,
        Changed,
        Intersect,
    }

    internal readonly bool IsDefault()
    {
        return InlineIsDefault() && overflow == null;
    }

    public readonly bool Equals(WideBitSet other)
    {
        return InlineEquals(other) && BlocksEqual(overflow, other.overflow);
    }

    public readonly int GetBitCount()
    {
        var count = InlineBitCount();
        var blocks = overflow;
        if (blocks == null) return count;
        foreach (var block in blocks) {
            count += NumberOfSetBits(block.L0);
            count += NumberOfSetBits(block.L1);
            count += NumberOfSetBits(block.L2);
            count += NumberOfSetBits(block.L3);
        }
        return count;
    }

    public readonly bool HasAll(in WideBitSet bitSet)
    {
        if (!InlineHasAll(bitSet)) return false;
        var required = bitSet.overflow;
        if (required == null) return true;
        var available = overflow;
        if (available == null) return false;

        var availableIndex = 0;
        foreach (var requiredBlock in required) {
            while (availableIndex < available.Length && available[availableIndex].Index < requiredBlock.Index) {
                availableIndex++;
            }
            if (availableIndex == available.Length || available[availableIndex].Index != requiredBlock.Index) return false;
            if (!available[availableIndex].HasAll(requiredBlock)) return false;
        }
        return true;
    }

    public readonly bool HasAny(in WideBitSet bitSet)
    {
        if (InlineHasAny(bitSet)) return true;
        var left = overflow;
        var right = bitSet.overflow;
        if (left == null || right == null) return false;

        var leftIndex = 0;
        var rightIndex = 0;
        while (leftIndex < left.Length && rightIndex < right.Length) {
            var leftBlock = left[leftIndex];
            var rightBlock = right[rightIndex];
            if (leftBlock.Index < rightBlock.Index) {
                leftIndex++;
            } else if (leftBlock.Index > rightBlock.Index) {
                rightIndex++;
            } else {
                if (leftBlock.HasAny(rightBlock)) return true;
                leftIndex++;
                rightIndex++;
            }
        }
        return false;
    }

    internal void Add(in WideBitSet right)
    {
        InlineAdd(right);
        if (right.overflow != null) overflow = CombineBlocks(overflow, right.overflow, BlockOperation.Add);
    }

    internal void Remove(in WideBitSet right)
    {
        InlineRemove(right);
        if (right.overflow != null) overflow = CombineBlocks(overflow, right.overflow, BlockOperation.Remove);
    }

    internal static WideBitSet Add(in WideBitSet left, in WideBitSet right)
    {
        var result = left;
        result.Add(right);
        return result;
    }

    internal static WideBitSet Remove(in WideBitSet left, in WideBitSet right)
    {
        var result = left;
        result.Remove(right);
        return result;
    }

    internal static WideBitSet Added(in WideBitSet left, in WideBitSet right)
    {
        var result = InlineAdded(left, right);
        result.overflow = CombineBlocks(left.overflow, right.overflow, BlockOperation.Added);
        return result;
    }

    internal static WideBitSet Removed(in WideBitSet left, in WideBitSet right)
    {
        var result = InlineRemoved(left, right);
        result.overflow = CombineBlocks(left.overflow, right.overflow, BlockOperation.Remove);
        return result;
    }

    internal static WideBitSet Changed(in WideBitSet left, in WideBitSet right)
    {
        var result = InlineChanged(left, right);
        result.overflow = CombineBlocks(left.overflow, right.overflow, BlockOperation.Changed);
        return result;
    }

    internal static WideBitSet Intersect(in WideBitSet left, in WideBitSet right)
    {
        var result = InlineIntersect(left, right);
        result.overflow = CombineBlocks(left.overflow, right.overflow, BlockOperation.Intersect);
        return result;
    }

    private static BitSetBlock[] CombineBlocks(BitSetBlock[] left, BitSetBlock[] right, BlockOperation operation)
    {
        if (right == null) {
            return operation switch {
                BlockOperation.Add or BlockOperation.Remove or BlockOperation.Changed => left,
                _ => null,
            };
        }
        if (left == null) {
            return operation switch {
                BlockOperation.Add or BlockOperation.Added or BlockOperation.Changed => right,
                _ => null,
            };
        }

        switch (operation) {
            case BlockOperation.Add:
                if (BlocksContainAll(left, right)) return left;
                if (BlocksContainAll(right, left)) return right;
                break;
            case BlockOperation.Remove:
                if (!BlocksHaveAny(left, right)) return left;
                break;
            case BlockOperation.Added:
                if (!BlocksHaveAny(left, right)) return right;
                if (BlocksContainAll(left, right)) return null;
                break;
            case BlockOperation.Changed:
                if (BlocksEqual(left, right)) return null;
                break;
            case BlockOperation.Intersect:
                if (!BlocksHaveAny(left, right)) return null;
                if (BlocksContainAll(left, right)) return right;
                if (BlocksContainAll(right, left)) return left;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }

        var result = new BitSetBlock[CountCombinedBlocks(left, right, operation)];
        var leftIndex = 0;
        var rightIndex = 0;
        var resultIndex = 0;

        while (leftIndex < left.Length || rightIndex < right.Length) {
            BitSetBlock block;
            if (rightIndex == right.Length ||
                leftIndex < left.Length && left[leftIndex].Index < right[rightIndex].Index) {
                block = left[leftIndex++];
                if (operation is BlockOperation.Added or BlockOperation.Intersect) continue;
            } else if (leftIndex == left.Length || right[rightIndex].Index < left[leftIndex].Index) {
                block = right[rightIndex++];
                if (operation is BlockOperation.Remove or BlockOperation.Intersect) continue;
            } else {
                var leftBlock = left[leftIndex++];
                var rightBlock = right[rightIndex++];
                block = operation switch {
                    BlockOperation.Add       => BitSetBlock.Add(leftBlock, rightBlock),
                    BlockOperation.Remove    => BitSetBlock.Remove(leftBlock, rightBlock),
                    BlockOperation.Added     => BitSetBlock.Added(leftBlock, rightBlock),
                    BlockOperation.Changed   => BitSetBlock.Changed(leftBlock, rightBlock),
                    BlockOperation.Intersect => BitSetBlock.Intersect(leftBlock, rightBlock),
                    _ => throw new ArgumentOutOfRangeException(nameof(operation)),
                };
            }
            if (!block.IsDefault) result[resultIndex++] = block;
        }

        return result.Length == 0 ? null : result;
    }

    private static int CountCombinedBlocks(BitSetBlock[] left, BitSetBlock[] right, BlockOperation operation)
    {
        var leftIndex = 0;
        var rightIndex = 0;
        var count = 0;

        while (leftIndex < left.Length || rightIndex < right.Length) {
            BitSetBlock block;
            if (rightIndex == right.Length ||
                leftIndex < left.Length && left[leftIndex].Index < right[rightIndex].Index) {
                block = left[leftIndex++];
                if (operation is BlockOperation.Added or BlockOperation.Intersect) continue;
            } else if (leftIndex == left.Length || right[rightIndex].Index < left[leftIndex].Index) {
                block = right[rightIndex++];
                if (operation is BlockOperation.Remove or BlockOperation.Intersect) continue;
            } else {
                var leftBlock = left[leftIndex++];
                var rightBlock = right[rightIndex++];
                block = operation switch {
                    BlockOperation.Add       => BitSetBlock.Add(leftBlock, rightBlock),
                    BlockOperation.Remove    => BitSetBlock.Remove(leftBlock, rightBlock),
                    BlockOperation.Added     => BitSetBlock.Added(leftBlock, rightBlock),
                    BlockOperation.Changed   => BitSetBlock.Changed(leftBlock, rightBlock),
                    BlockOperation.Intersect => BitSetBlock.Intersect(leftBlock, rightBlock),
                    _ => throw new ArgumentOutOfRangeException(nameof(operation)),
                };
            }
            if (!block.IsDefault) count++;
        }
        return count;
    }

    private static bool BlocksContainAll(BitSetBlock[] available, BitSetBlock[] required)
    {
        var availableIndex = 0;
        foreach (var requiredBlock in required) {
            while (availableIndex < available.Length && available[availableIndex].Index < requiredBlock.Index) {
                availableIndex++;
            }
            if (availableIndex == available.Length || available[availableIndex].Index != requiredBlock.Index) {
                return false;
            }
            if (!available[availableIndex].HasAll(requiredBlock)) return false;
        }
        return true;
    }

    private static bool BlocksHaveAny(BitSetBlock[] left, BitSetBlock[] right)
    {
        var leftIndex = 0;
        var rightIndex = 0;
        while (leftIndex < left.Length && rightIndex < right.Length) {
            var leftBlock = left[leftIndex];
            var rightBlock = right[rightIndex];
            if (leftBlock.Index < rightBlock.Index) {
                leftIndex++;
            } else if (leftBlock.Index > rightBlock.Index) {
                rightIndex++;
            } else {
                if (leftBlock.HasAny(rightBlock)) return true;
                leftIndex++;
                rightIndex++;
            }
        }
        return false;
    }

    private static bool BlocksEqual(BitSetBlock[] left, BitSetBlock[] right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null || left.Length != right.Length) return false;
        for (var index = 0; index < left.Length; index++) {
            if (!left[index].Equals(right[index])) return false;
        }
        return true;
    }
}
