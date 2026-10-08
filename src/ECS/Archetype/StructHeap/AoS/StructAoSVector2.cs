// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable UseNullPropagation
// ReSharper disable StaticMemberInGenericType
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal sealed class StructAoSVector2<T> : StructHeap<T>
    where T : struct
{
    private const   int     FieldCount = 2;
    private const   int     Shift      = 1;

    public override T[]     Components      => throw new NotSupportedException();

    internal override (object,int) GetComponents () => (components, simdOffset);

    private         float[] components;
    private         int     simdOffset;

    internal StructAoSVector2(int structIndex)
        : base (structIndex)
    {
        var capacity = SimdUtils.CalcCapacity<T>(ArchetypeUtils.MinCapacity);
        components  = AllocateAligned(capacity * FieldCount, out simdOffset);
    }

    internal override ref T GetComponentRef(int index) {
        return ref Unsafe.As<float, T>(ref components[simdOffset + (index << Shift)]);
    }

    internal override T GetComponentValue(int index) {
        return GetComponentFromAoS (index);
    }

    internal override void SetComponent(int index, T component) {
        ComponentToAoS(component, index);
    }

    internal override T GetSoA(int index) {
        throw new InvalidOperationException($"Component '{typeof(T).Name}' is stored as AoS. GetSoA() requires AoSoA storage. Add attribute [AoSoA] or use GetComponent() instead.");
    }


    internal override void StashComponent(int compIndex) {
        componentStash = GetComponentFromAoS(compIndex);
    }

    internal override  void SetBatchComponent(BatchComponent batchComponent, int compIndex)
    {
        ComponentToAoS(((BatchComponent<T>)batchComponent).value, compIndex);
    }

    protected override  int     ComponentsLength    => (components.Length - simdOffset) / FieldCount;

    internal  override  Type    StructType          => typeof(T);

    internal override void ResizeComponents (int newCapacity, int count)
    {
        var capacity    = SimdUtils.CalcCapacity<T>(newCapacity);
        var oldOffset   = simdOffset;
        var dst         = AllocateAligned(capacity * FieldCount, out simdOffset);
        count          *= FieldCount;
        new ReadOnlySpan<float>(components, oldOffset, count).CopyTo(new Span<float>(dst, simdOffset, count));
        components = dst;
    }

    internal override void MoveComponent(int from, int to)
    {
        ComponentToAoS(GetComponentFromAoS(from), to);
    }

    internal override void CopyComponentTo(int sourcePos, StructHeap targetHeap, int targetPos)
    {
        var targetSoA       = (StructAoSVector2<T>)targetHeap;
        targetSoA.ComponentToAoS(GetComponentFromAoS(sourcePos), targetPos);
    }


    internal  override  void SetComponentDefault (int compIndex) {
        ComponentToAoS(default, compIndex);
    }

    internal  override  void SetComponentsDefault (int compIndexStart, int count)
    {
        new Span<float>(components, simdOffset + compIndexStart * FieldCount, count * FieldCount).Clear();
    }

    internal override object GetComponentDebug(int compIndex) => GetComponentFromAoS(compIndex);


    internal override Bytes Write(ObjectWriter writer, int compIndex) {
        var mapper = (TypeMapper<T>)writer.TypeCache.GetTypeMapper(typeof(T));
        var value = GetComponentFromAoS(compIndex);
        return writer.WriteAsBytesMapper(value, mapper);
    }

    internal override void Read(ObjectReader reader, int compIndex, JsonValue json) {
        var mapper = (TypeMapper<T>)reader.TypeCache.GetTypeMapper(typeof(T));
        var value = reader.ReadMapper(mapper, json);
        ComponentToAoS(value, compIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ComponentToAoS(T src, int index)
    {
        Unsafe.As<float, T>(ref components[simdOffset + (index << Shift)]) = src;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T GetComponentFromAoS(int index)
    {
        return Unsafe.As<float, T>(ref components[simdOffset + (index << Shift)]);
    }
}
