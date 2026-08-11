// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace Friflo.Engine.ECS;

/// <summary>
/// Note: The order of struct type indices matters.<br/>
/// The struct is used when dealing with generic types like: T1, T2, T3, ...   
/// </summary>
internal readonly struct SignatureIndexes
{
    internal readonly   int     length; // 4    - values: [1, 10] ensured by constructors
    
    internal readonly   ushort  T1;     // 2
    internal readonly   ushort  T2;     // 2
    internal readonly   ushort  T3;     // 2
    internal readonly   ushort  T4;     // 2
    internal readonly   ushort  T5;     // 2
    private  readonly   ushort  T6;     // 2
    private  readonly   ushort  T7;     // 2
    private  readonly   ushort  T8;     // 2
    private  readonly   ushort  T9;     // 2
    private  readonly   ushort  T10;    // 2
    
    public   SignatureIndexesEnumerator GetEnumerator() => new (this);
    
    public override     string          ToString()      => GetString("SignatureIndexes: ");
    
    internal SignatureIndexes (
        int T1
    ) {
        length = 1;
        this.T1     = SchemaTypeIndex.ToUInt16(T1, nameof(T1));
    }
    
    internal SignatureIndexes (
        int T1,
        int T2

    ) {
        this.length = 2;
        this.T1     = SchemaTypeIndex.ToUInt16(T1, nameof(T1));
        this.T2     = SchemaTypeIndex.ToUInt16(T2, nameof(T2));
    }
    
    internal SignatureIndexes (
        int T1,
        int T2,
        int T3
    ) {
        this.length = 3;
        this.T1     = SchemaTypeIndex.ToUInt16(T1, nameof(T1));
        this.T2     = SchemaTypeIndex.ToUInt16(T2, nameof(T2));
        this.T3     = SchemaTypeIndex.ToUInt16(T3, nameof(T3));
    }
    
    internal SignatureIndexes (
        int T1,
        int T2,
        int T3,
        int T4
    ) {
        this.length = 4;
        this.T1     = SchemaTypeIndex.ToUInt16(T1, nameof(T1));
        this.T2     = SchemaTypeIndex.ToUInt16(T2, nameof(T2));
        this.T3     = SchemaTypeIndex.ToUInt16(T3, nameof(T3));
        this.T4     = SchemaTypeIndex.ToUInt16(T4, nameof(T4));
    }
    
    internal SignatureIndexes (
        int T1,
        int T2,
        int T3,
        int T4,
        int T5
    ) {
        this.length = 5;
        this.T1     = SchemaTypeIndex.ToUInt16(T1, nameof(T1));
        this.T2     = SchemaTypeIndex.ToUInt16(T2, nameof(T2));
        this.T3     = SchemaTypeIndex.ToUInt16(T3, nameof(T3));
        this.T4     = SchemaTypeIndex.ToUInt16(T4, nameof(T4));
        this.T5     = SchemaTypeIndex.ToUInt16(T5, nameof(T5));
    }
    
    internal SignatureIndexes (
        int T1,
        int T2,
        int T3,
        int T4,
        int T5,
        int T6
    ) {
        this.length = 6;
        this.T1     = SchemaTypeIndex.ToUInt16(T1, nameof(T1));
        this.T2     = SchemaTypeIndex.ToUInt16(T2, nameof(T2));
        this.T3     = SchemaTypeIndex.ToUInt16(T3, nameof(T3));
        this.T4     = SchemaTypeIndex.ToUInt16(T4, nameof(T4));
        this.T5     = SchemaTypeIndex.ToUInt16(T5, nameof(T5));
        this.T6     = SchemaTypeIndex.ToUInt16(T6, nameof(T6));
    }
    
    internal SignatureIndexes (
        int T1,
        int T2,
        int T3,
        int T4,
        int T5,
        int T6,
        int T7
    ) {
        this.length = 7;
        this.T1     = SchemaTypeIndex.ToUInt16(T1, nameof(T1));
        this.T2     = SchemaTypeIndex.ToUInt16(T2, nameof(T2));
        this.T3     = SchemaTypeIndex.ToUInt16(T3, nameof(T3));
        this.T4     = SchemaTypeIndex.ToUInt16(T4, nameof(T4));
        this.T5     = SchemaTypeIndex.ToUInt16(T5, nameof(T5));
        this.T6     = SchemaTypeIndex.ToUInt16(T6, nameof(T6));
        this.T7     = SchemaTypeIndex.ToUInt16(T7, nameof(T7));
    }
    
    internal SignatureIndexes (
        int T1,
        int T2,
        int T3,
        int T4,
        int T5,
        int T6,
        int T7,
        int T8
    ) {
        this.length = 8;
        this.T1     = SchemaTypeIndex.ToUInt16(T1, nameof(T1));
        this.T2     = SchemaTypeIndex.ToUInt16(T2, nameof(T2));
        this.T3     = SchemaTypeIndex.ToUInt16(T3, nameof(T3));
        this.T4     = SchemaTypeIndex.ToUInt16(T4, nameof(T4));
        this.T5     = SchemaTypeIndex.ToUInt16(T5, nameof(T5));
        this.T6     = SchemaTypeIndex.ToUInt16(T6, nameof(T6));
        this.T7     = SchemaTypeIndex.ToUInt16(T7, nameof(T7));
        this.T8     = SchemaTypeIndex.ToUInt16(T8, nameof(T8));
    }
    
    internal SignatureIndexes (
        int T1,
        int T2,
        int T3,
        int T4,
        int T5,
        int T6,
        int T7,
        int T8,
        int T9
    ) {
        this.length = 9;
        this.T1     = SchemaTypeIndex.ToUInt16(T1, nameof(T1));
        this.T2     = SchemaTypeIndex.ToUInt16(T2, nameof(T2));
        this.T3     = SchemaTypeIndex.ToUInt16(T3, nameof(T3));
        this.T4     = SchemaTypeIndex.ToUInt16(T4, nameof(T4));
        this.T5     = SchemaTypeIndex.ToUInt16(T5, nameof(T5));
        this.T6     = SchemaTypeIndex.ToUInt16(T6, nameof(T6));
        this.T7     = SchemaTypeIndex.ToUInt16(T7, nameof(T7));
        this.T8     = SchemaTypeIndex.ToUInt16(T8, nameof(T8));
        this.T9     = SchemaTypeIndex.ToUInt16(T9, nameof(T9));
    }
    
    internal SignatureIndexes (
        int T1,
        int T2,
        int T3,
        int T4,
        int T5,
        int T6,
        int T7,
        int T8,
        int T9,
        int T10
    ) {
        this.length = 10;
        this.T1     = SchemaTypeIndex.ToUInt16(T1, nameof(T1));
        this.T2     = SchemaTypeIndex.ToUInt16(T2, nameof(T2));
        this.T3     = SchemaTypeIndex.ToUInt16(T3, nameof(T3));
        this.T4     = SchemaTypeIndex.ToUInt16(T4, nameof(T4));
        this.T5     = SchemaTypeIndex.ToUInt16(T5, nameof(T5));
        this.T6     = SchemaTypeIndex.ToUInt16(T6, nameof(T6));
        this.T7     = SchemaTypeIndex.ToUInt16(T7, nameof(T7));
        this.T8     = SchemaTypeIndex.ToUInt16(T8, nameof(T8));
        this.T9     = SchemaTypeIndex.ToUInt16(T9, nameof(T9));
        this.T10    = SchemaTypeIndex.ToUInt16(T10, nameof(T10));
    }
    
    [ExcludeFromCodeCoverage]
    [Conditional("DEBUG")]
    private static void AssertLength(int length) {
        if (length is < 1 or > 10) {
            throw new IndexOutOfRangeException();
        }
    }
    
    internal int GetStructIndex(int index)
    {
        switch (index) {
            case 0:     return T1;
            case 1:     return T2;
            case 2:     return T3;
            case 3:     return T4;
            case 4:     return T5;
            case 5:     return T6;
            case 6:     return T7;
            case 7:     return T8;
            case 8:     return T9;
            case 9:     return T10;
        //  default:    throw new IndexOutOfRangeException(); // unreachable - already ensured by constructor
        }
        return -1;  // unreachable - all valid cases are covered
    }
    
    internal string GetString (string prefix) {
        var sb = new StringBuilder();
        if (prefix != null) {
            sb.Append(prefix);
        }
        sb.Append('[');
        var components = EntityStoreBase.Static.EntitySchema.components;
        for (int n = 0; n < length; n++)
        {
            var structIndex = GetStructIndex(n);
            sb.Append(components[structIndex].Name);
            sb.Append(", "); 
        }
        sb.Length -= 2;
        sb.Append(']');
        return sb.ToString();
    }
}


internal struct SignatureIndexesEnumerator
{
    private readonly    SignatureIndexes    indexes;    // 24
    private             int                 index;      //  4
    
    internal SignatureIndexesEnumerator(in SignatureIndexes indexes)
    {
        this.indexes    = indexes;
        index           = -1;
    }
    
    public int Current => indexes.GetStructIndex(index);

    // --- IEnumerator
    public bool MoveNext() {
        if (index < indexes.length - 1) {
            index++;
            return true;
        }
        return false;
    }
}


