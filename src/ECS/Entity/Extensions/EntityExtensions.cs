// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using System;

// ReSharper disable UseNullPropagation
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// Provide extension methods to optimize <see cref="Entity"/> modifications.<br/>
/// <c>Add()</c> and <c>Remove()</c> cause only none or one structural change.   
/// </summary>
public static partial class EntityExtensions
{
#region add components
    internal static readonly ComponentTypes IndexTypes = EntityStoreBase.Static.EntitySchema.indexTypes;
    
    private static void UpdateIndexedComponents(Entity entity, Archetype archetype, in ComponentTypes indexTypes)
    {
        foreach (var indexType in indexTypes) {
            archetype.heapMap[indexType.StructIndex].UpdateIndex(entity);
        }
    }
    
    private static void AddIndexedComponents(Entity entity, Archetype newType, Archetype oldType, in ComponentTypes indexTypes)
    {
        foreach (var indexType in indexTypes) {
            var heap = newType.heapMap[indexType.StructIndex]; 
            if (oldType.heapMap[indexType.StructIndex] == null) {
                heap.AddIndex(entity);
            } else {
                heap.UpdateIndex(entity);
            }
        }
    }
    
    private static void RemoveIndexedComponents(Entity entity, Archetype archetype, in ComponentTypes indexTypes)
    {
        foreach (var indexType in indexTypes) {
            archetype.heapMap[indexType.StructIndex]?.RemoveIndex(entity);
        }
    }
    
    private static void StashAddComponents(EntityStoreBase store, in ComponentTypes types, in SignatureIndexes indexes, Archetype oldType, int oldCompIndex)
    {
        if (store.ComponentAdded == null && !types.HasAny(IndexTypes)) {
            return;
        }
        var oldHeapMap  = oldType.heapMap;
        foreach (var addTypeIndex in indexes)
        {
            var oldHeap = oldHeapMap[addTypeIndex];
            if (oldHeap == null) {
                continue;
            }
            oldHeap.StashComponent(oldCompIndex);
        }
    }
    
    private static void SendAddEvents(Entity entity, in ComponentTypes types, in SignatureIndexes indexes, Archetype newType, Archetype oldType)
    {
        var store = entity.store;
        
        // --- add indexed components to indexes
        var indexTypes = ComponentTypes.Intersect(types, IndexTypes);
        if (!indexTypes.IsDefault()) {
            AddIndexedComponents(entity, newType, oldType, indexTypes);
        }
        // --- tag event
        var tagsChanged = store.TagsChanged;
        if (tagsChanged != null && !newType.tags.bitSet.Equals(oldType.Tags.bitSet)) {
            tagsChanged(new TagsChanged(store, entity.Id, newType.tags, oldType.Tags));
        }
        // --- component events 
        var componentAdded = store.ComponentAdded;
        if (componentAdded == null) {
            return;
        }
        var oldHeapMap  = oldType.heapMap;
        var id          = entity.Id;
        foreach (var addTypeIndex in indexes)
        {
            var oldHeap = oldHeapMap[addTypeIndex];
            var action  = oldHeap == null ? ComponentChangedAction.Add : ComponentChangedAction.Update;
            componentAdded(new ComponentChanged (store, id, action, addTypeIndex, oldHeap));
        }
    }
    #endregion


#region remove components
    private static void StashRemoveComponents(EntityStoreBase store, in SignatureIndexes removeComponents, Archetype oldType, int oldCompIndex)
    {
        if (store.ComponentRemoved == null && !oldType.componentTypes.HasAny(IndexTypes)) {
            return;
        }
        var oldHeapMap = oldType.heapMap;
        foreach (var removeTypeIndex in removeComponents)
        {
            oldHeapMap[removeTypeIndex]?.StashComponent(oldCompIndex);
        }
    }
    
    private static void SendRemoveEvents(Entity entity, in ComponentTypes types, in SignatureIndexes removeComponents, Archetype newType, Archetype oldType)
    {
        var store = entity.store;
        // --- remove indexed components from indexes
        var indexTypes = ComponentTypes.Intersect(types, IndexTypes);
        if (!indexTypes.IsDefault()) {
            RemoveIndexedComponents(entity, oldType, indexTypes);
        }
        // --- tag event
        var tagsChanged = store.TagsChanged;
        if (tagsChanged != null && !newType.tags.bitSet.Equals(oldType.Tags.bitSet)) {
            tagsChanged(new TagsChanged(store, entity.Id, newType.tags, oldType.Tags));
        }
        // --- component events 
        var componentRemoved = store.ComponentRemoved;
        if (componentRemoved == null) {
            return;
        }
        var oldHeapMap = oldType.heapMap;
        var id          = entity.Id;
        foreach (var removeTypeIndex in removeComponents)
        {
            var oldHeap = oldHeapMap[removeTypeIndex];
            if (oldHeap == null) {
                continue;
            }
            componentRemoved(new ComponentChanged (store, id, ComponentChangedAction.Remove, removeTypeIndex, oldHeap));
        }
    }
    #endregion


#region set components
    private static void StashSetComponents(in Entity entity, in ComponentTypes types, in SignatureIndexes indexes, Archetype type, int compIndex)
    {
        if (entity.store.ComponentAdded == null && !types.HasAny(IndexTypes)) {
            return;
        }
        var heapMap = type.heapMap;
        foreach (var structIndex in indexes) {
            heapMap[structIndex]?.StashComponent(compIndex);
        }
    }
    
    private static MissingComponentException MissingComponentException(Entity entity, SignatureIndexes indexes, Archetype type)
    {
        bool isFirst = true;
        var sb = new StringBuilder();
        sb.Append("entity ");
        EntityUtils.EntityToString(entity.Id, type, sb);
        
        var schemaComponents = EntityStore.GetEntitySchema().components;
        sb.Append(" - missing: [");
        foreach (var index in indexes) {
            if (type.componentTypes.bitSet.Has(index)) {
                continue;
            }
            if (isFirst) {
                isFirst = false;
            } else {
                sb.Append(", ");
            }
            sb.Append(schemaComponents[index].Name);
        }
        sb.Append(']');
        return new MissingComponentException(sb.ToString());
    }
    
    private static void SendSetEvents(Entity entity, in ComponentTypes types, in SignatureIndexes indexes, Archetype type)
    {
        var store = entity.store;
        // --- update indexed component indexes
        var indexTypes = ComponentTypes.Intersect(types, IndexTypes);
        if (!indexTypes.IsDefault()) {
            UpdateIndexedComponents(entity, type, indexTypes);
        }
        var componentAdded = store.ComponentAdded;
        if (componentAdded == null) {
            return;
        }
        var heapMap = type.heapMap;
        foreach (var index in indexes) {
            componentAdded(new ComponentChanged (store, entity.Id, ComponentChangedAction.Update, index, heapMap[index]));    
        }
    }
    #endregion
}

/// <summary>
/// Is thrown when calling <c>Entity.Set()</c> on an entity missing the specified components.
/// </summary>
public class MissingComponentException : Exception
{
    internal MissingComponentException(string message) : base (message) { }
}
