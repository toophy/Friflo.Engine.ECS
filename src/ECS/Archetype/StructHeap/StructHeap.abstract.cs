// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using Friflo.Engine.ECS.Index;

// ReSharper disable UseNullPropagation
// ReSharper disable StaticMemberInGenericType
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal abstract class StructHeap<T> : StructHeap, IComponentStash<T>
    where T : struct
{
    internal        T       componentStash;


    internal StructHeap(int structIndex)
        : base (structIndex)
    {
    }

    public          ref T   GetStashRef()   => ref componentStash;
    public override object  GetStashDebug() => componentStash;


    public   abstract   T[]     Components { get; }

    internal abstract   (object components, int simdOffset) GetComponents ();

    internal abstract   ref T   GetComponentRef(int index);

    internal abstract   T       GetComponentValue(int index);

    internal abstract   void    SetComponent(int index, T component);

    internal abstract   T       GetSoA(int index);

    internal override void UpdateIndex(Entity entity) {
        StoreIndex.UpdateIndex(entity.store, entity.Id, GetComponentValue(entity.compIndex), this);
    }

    internal override void AddIndex(Entity entity) {
        StoreIndex.AddIndex(entity.store, entity.Id, GetComponentValue(entity.compIndex));
    }

    internal override void RemoveIndex(Entity entity) {
        StoreIndex.RemoveIndex(entity.store, entity.Id, this);
    }

    internal override void CopyComponent(int sourcePos, StructHeap targetHeap, int targetPos, in CopyContext context, in ComponentTypes updateIndexTypes)
    {
        var source = GetComponentValue(sourcePos);
        var target = (StructHeap<T>)targetHeap;
        var value = target.GetComponentValue(targetPos);
        if (StructInfo<T>.HasIndex) {
            if (!updateIndexTypes.bitSet.Has(StructInfo<T>.Index)) {
                StoreIndex.AddIndex(context.target.store, context.target.Id, source);
            } else {
                target.componentStash = value;
                StoreIndex.UpdateIndex(context.target.store, context.target.Id, source, target);
            }
        }
        var copyValue = CopyValueUtils<T>.CopyValue;
        if (copyValue == null) value = source;
        else copyValue(source, ref value, context);
        target.SetComponent(targetPos, value);
    }

    internal override bool GetComponentMember<TField>(int compIndex, MemberPath memberPath, out TField value, out Exception exception)
    {
        var getter = (MemberPathGetter<T, TField>)memberPath.getter;
        try {
            value = getter(GetComponentValue(compIndex));
            exception = null;
            return true;
        }
        catch (Exception e) {
            value = default;
            exception = e;
            return false;
        }
    }

    internal override bool SetComponentMember<TField>(Entity entity, MemberPath memberPath, TField value, Delegate onMemberChanged, out Exception exception)
    {
        var setter = (MemberPathSetter<T, TField>)memberPath.setter;
        var component = GetComponentValue(entity.compIndex);
        var oldValue = component;
        try {
            setter(ref component, value);
            SetComponent(entity.compIndex, component);
            if (onMemberChanged != null) {
                ((OnMemberChanged<T>)onMemberChanged)(ref component, entity, memberPath.path, oldValue);
            }
            exception = null;
            return true;
        }
        catch (Exception e) {
            exception = e;
            return false;
        }
        finally {
            SetComponent(entity.compIndex, component);
        }
    }
}
