// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Engine.ECS.Utils;

public struct BitSetEnumerator
{
    private readonly    long    l0;
    private readonly    long    l1;
    private readonly    long    l2;
    private readonly    long    l3;

    private             long    lng;
    private             int     lngPos;
    private             int     curPos;

    internal BitSetEnumerator(in BitSet bitSet) {
        l0  = bitSet.l0;
        l1  = bitSet.l1;
        l2  = bitSet.l2;
        l3  = bitSet.l3;
        lng = l0;
    }

    public readonly int Current => curPos;

    public void Reset() {
        lng     = l0;
        lngPos  = 0;
        curPos  = 0;
    }

    public bool MoveNext()
    {
        while (true)
        {
            if (lng != 0) {
#if NETCOREAPP3_0_OR_GREATER
                var bitPos  = System.Numerics.BitOperations.TrailingZeroCount(lng);
#else
                var bitPos  = BitSet.TrailingZeroCount(lng);
#endif
                lng        ^= 1L << bitPos;
                curPos      = (lngPos << 6) + bitPos;
                return true;
            }
            switch (++lngPos) {
                case 1:     lng = l1;   break;
                case 2:     lng = l2;   break;
                case 3:     lng = l3;   break;
                default:    return false;
            }
        }
    }
}
