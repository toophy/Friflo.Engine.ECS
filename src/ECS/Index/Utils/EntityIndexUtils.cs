// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Engine.ECS.Collections;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS.Index;

/// <summary>
/// Similar to <see cref="DictionaryUtils"/> and additionally updates <see cref="EntityNode.isLinked"/> state.
/// </summary>
internal static class EntityIndexUtils
{
    internal static void RemoveComponentValue(int id, int target, EntityIndex componentIndex, bool updateOwner = true)
    {
        var map     = componentIndex.entityMap;
        var idHeap  = componentIndex.idHeap;
        map.TryGetValue(target, out var ids);
        var idSpan  = ids.GetSpan(idHeap, componentIndex.store);
        int index   = idSpan.IndexOf(id);
        if (index == -1) {
            return; // unexpected. Better safe than sorry. Used belts with suspenders :)
        }
        var store = componentIndex.store;
        if (updateOwner) {
            EntityTypeTracking.RemoveOwner(store, id, componentIndex.structIndex);
        }
        if (ids.Count == 1) {
            EntityTypeTracking.RemoveLinked(store, target, componentIndex.structIndex);
            componentIndex.modified = true;
            map.Remove(target);
            return;
        }
        ids.RemoveAt(index, idHeap);
        map[target] = ids;
    }
    
    internal static void AddComponentValue(int id, int target, EntityIndex componentIndex, bool updateOwner = true)
    {
        var map     = componentIndex.entityMap;
        var idHeap  = componentIndex.idHeap;
        map.TryGetValue(target, out var ids);
        var idSpan = ids.GetSpan(idHeap, componentIndex.store);
        if (idSpan.IndexOf(id) != -1) {
            return; // unexpected. Better safe than sorry. Used belts with suspenders :)
        }
        var store = componentIndex.store;
        if (updateOwner) {
            EntityTypeTracking.AddOwner(store, id, componentIndex.structIndex);
        }
        if (ids.Count == 0) {
            EntityTypeTracking.AddLinked(store, target, componentIndex.structIndex);
            componentIndex.modified = true;
        }
        ids.Add(id, idHeap);
        map[target] = ids;
    }
}
