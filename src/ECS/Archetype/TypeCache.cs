// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal struct TypeCache
{
    private ushort  type0;  //  2
    private ushort  type1;  //  2
    private ushort  type2;  //  2
    private ushort  type3;  //  2
    
    private int     index0; //  4
    private int     index1; //  4
    private int     index2; //  4
    private int     index3; //  4
    
    private int     next;   //  4
    
    internal int FindType(int type)
    {
        if (type == type0)  return index0;
        if (type == type1)  return index1;
        if (type == type2)  return index2;
        if (type == type3)  return index3;
        return -1;
    }
    
    internal Archetype Cache(int type, Archetype archetype)
    {
        var compactType = SchemaTypeIndex.ToUInt16(type, nameof(type));
        var index   = archetype.archIndex;
        var at      = next++ % 4;
        switch (at) {
            case 0: type0 = compactType; index0 = index; break;
            case 1: type1 = compactType; index1 = index; break;
            case 2: type2 = compactType; index2 = index; break;
            case 3: type3 = compactType; index3 = index; break;
        }
        return archetype;
    }
}
