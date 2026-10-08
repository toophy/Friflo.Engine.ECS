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

internal sealed class StructAoSFloat<T> : StructHeap<T>
    where T : struct
{
    public override T[]     Components      => throw new NotSupportedException();

    internal override (object,int) GetComponents () => (components, simdOffset);

    private         float[] components;
    private         int     simdOffset;

    internal StructAoSFloat(int structIndex)
        : base (structIndex)
    {
        var capacity = SimdUtils.CalcCapacity<T>(ArchetypeUtils.MinCapacity);
        components  = AllocateAligned(capacity, out simdOffset);
    }

    internal override ref T GetComponentRef(int index) {
        return ref Unsafe.As<float[], T[]>(ref components)[simdOffset + index];
    }

    internal override T GetComponentValue(int index) {
        return GetComponentFromAoS (index);
    }

    internal override void SetComponent(int index, T component) {
        ComponentToAoS(component, index);
    }

    internal override T GetSoA(int index) {
        return Unsafe.As<float[], T[]>(ref components)[simdOffset + index];
    }


    internal override void StashComponent(int compIndex) {
        componentStash = GetComponentFromAoS(compIndex);
    }

    internal override  void SetBatchComponent(BatchComponent batchComponent, int compIndex)
    {
        ComponentToAoS(((BatchComponent<T>)batchComponent).value, compIndex);
    }

    protected override  int     ComponentsLength    => components.Length - simdOffset;

    internal  override  Type    StructType          => typeof(T);

    internal override void ResizeComponents (int newCapacity, int count)
    {
        var capacity    = SimdUtils.CalcCapacity<T>(newCapacity);
        var oldOffset   = simdOffset;
        var dst         = AllocateAligned(capacity, out simdOffset);
        new ReadOnlySpan<float>(components, oldOffset, count).CopyTo(new Span<float>(dst, simdOffset, count));
        components = dst;
    }

    internal override void MoveComponent(int from, int to)
    {
        var localComponents = components;
        var offest          = simdOffset;
        localComponents[offest + to] = localComponents[offest + from];
    }

    internal override void CopyComponentTo(int sourcePos, StructHeap targetHeap, int targetPos)
    {
        var targetSoA = (StructAoSFloat<T>)targetHeap;
        var dst             = targetSoA.components;
        dst[targetSoA.simdOffset + targetPos] = components[simdOffset + sourcePos];
    }


    internal  override  void SetComponentDefault (int compIndex) {
        ComponentToAoS(default, compIndex);
    }

    internal  override  void SetComponentsDefault (int compIndexStart, int count)
    {
        new Span<float>(components, simdOffset + compIndexStart, count).Clear();
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
        ref var component               = ref Unsafe.As<T, float>(ref src);
        components[simdOffset + index]  = component;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T GetComponentFromAoS(int index)
    {
        T result = default;
        ref var component   = ref Unsafe.As<T, float>(ref result);
        component           = components[simdOffset + index];
        return result;
    }
}
