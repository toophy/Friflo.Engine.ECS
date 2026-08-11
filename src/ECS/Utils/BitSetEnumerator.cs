// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Engine.ECS.Utils;

public struct BitSetEnumerator
{
    private readonly long l0;
    private readonly long l1;
    private readonly long l2;
    private readonly long l3;
    private readonly BitSetBlock[] overflow;

    private long word;
    private int wordIndex;
    private int overflowIndex;
    private int blockBase;
    private int current;

    internal BitSetEnumerator(in BitSet bitSet)
    {
        l0 = bitSet.l0;
        l1 = bitSet.l1;
        l2 = bitSet.l2;
        l3 = bitSet.l3;
        overflow = bitSet.GetOverflowBlocks();
        word = l0;
        wordIndex = 0;
        overflowIndex = -1;
        blockBase = 0;
        current = 0;
    }

    /// <returns>The index of the current set bit.</returns>
    public readonly int Current => current;

    public void Reset()
    {
        word = l0;
        wordIndex = 0;
        overflowIndex = -1;
        blockBase = 0;
        current = 0;
    }

    public bool MoveNext()
    {
        while (true) {
            if (word != 0) {
#if NETCOREAPP3_0_OR_GREATER
                var bitPosition = System.Numerics.BitOperations.TrailingZeroCount(word);
#else
                var bitPosition = BitSet.TrailingZeroCount(word);
#endif
                word ^= 1L << bitPosition;
                current = blockBase + (wordIndex << 6) + bitPosition;
                return true;
            }
            if (!MoveToNextWord()) return false;
        }
    }

    private bool MoveToNextWord()
    {
        if (wordIndex < 3) {
            wordIndex++;
            word = GetCurrentWord(wordIndex);
            return true;
        }

        var blocks = overflow;
        if (blocks == null || overflowIndex + 1 >= blocks.Length) return false;
        overflowIndex++;
        wordIndex = 0;
        blockBase = blocks[overflowIndex].Index << 8;
        word = blocks[overflowIndex].L0;
        return true;
    }

    private readonly long GetCurrentWord(int index)
    {
        if (overflowIndex >= 0) return overflow[overflowIndex].GetWord(index);
        return index switch {
            0 => l0,
            1 => l1,
            2 => l2,
            _ => l3,
        };
    }
}
