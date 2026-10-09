// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Engine.ECS.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal static class EntityTypeTracking
{
    private const int InlineCapacity = 32;

    internal static bool HasOwnerTypes(EntityStore store, int entityId)
    {
        return store.nodes[entityId].isOwner != 0 ||
               store.extension.ownerTypeOverflow?.ContainsKey(entityId) == true;
    }

    internal static bool HasLinkedTypes(EntityStore store, int entityId)
    {
        return store.nodes[entityId].isLinked != 0 ||
               store.extension.linkedTypeOverflow?.ContainsKey(entityId) == true;
    }

    internal static ComponentTypes GetOwnerTypes(EntityStore store, int entityId)
    {
        return GetTypes(store.extension.ownerTypeOverflow, entityId, store.nodes[entityId].isOwner);
    }

    internal static ComponentTypes GetLinkedTypes(EntityStore store, int entityId)
    {
        return GetTypes(store.extension.linkedTypeOverflow, entityId, store.nodes[entityId].isLinked);
    }

    internal static void AddOwner(EntityStore store, int entityId, int typeIndex)
    {
        if ((uint)typeIndex < InlineCapacity) {
            store.nodes[entityId].isOwner |= 1 << typeIndex;
            return;
        }
        SetOverflowBit(ref store.extension.ownerTypeOverflow, entityId, typeIndex);
    }

    internal static void RemoveOwner(EntityStore store, int entityId, int typeIndex)
    {
        if ((uint)typeIndex < InlineCapacity) {
            store.nodes[entityId].isOwner &= ~(1 << typeIndex);
            return;
        }
        ClearOverflowBit(store.extension.ownerTypeOverflow, entityId, typeIndex);
    }

    internal static void AddLinked(EntityStore store, int entityId, int typeIndex)
    {
        if ((uint)typeIndex < InlineCapacity) {
            store.nodes[entityId].isLinked |= 1 << typeIndex;
            return;
        }
        SetOverflowBit(ref store.extension.linkedTypeOverflow, entityId, typeIndex);
    }

    internal static void RemoveLinked(EntityStore store, int entityId, int typeIndex)
    {
        if ((uint)typeIndex < InlineCapacity) {
            store.nodes[entityId].isLinked &= ~(1 << typeIndex);
            return;
        }
        ClearOverflowBit(store.extension.linkedTypeOverflow, entityId, typeIndex);
    }

    internal static void RemoveEntity(in StoreExtension extension, int entityId)
    {
        extension.ownerTypeOverflow?.Remove(entityId);
        extension.linkedTypeOverflow?.Remove(entityId);
    }

    private static ComponentTypes GetTypes(Dictionary<int, WideBitSet> overflowMap, int entityId, int inlineBits)
    {
        var bits = default(WideBitSet);
        if (overflowMap != null) {
            overflowMap.TryGetValue(entityId, out bits);
        }
        bits.l0 |= (uint)inlineBits;
        return new ComponentTypes { bitSet = bits };
    }

    private static void SetOverflowBit(ref Dictionary<int, WideBitSet> overflowMap, int entityId, int typeIndex)
    {
        overflowMap ??= new Dictionary<int, WideBitSet>();
        overflowMap.TryGetValue(entityId, out var bits);
        bits.SetBit(typeIndex);
        overflowMap[entityId] = bits;
    }

    private static void ClearOverflowBit(Dictionary<int, WideBitSet> overflowMap, int entityId, int typeIndex)
    {
        if (overflowMap == null || !overflowMap.TryGetValue(entityId, out var bits)) return;
        bits.ClearBit(typeIndex);
        if (bits.IsDefault()) {
            overflowMap.Remove(entityId);
        } else {
            overflowMap[entityId] = bits;
        }
    }
}
