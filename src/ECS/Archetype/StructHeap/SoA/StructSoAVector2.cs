// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

internal sealed class StructSoAVector2<T> : StructHeap<T>
    where T : struct
{
    private const   int     LaneCount = 2;

    public override T[]     Components      => throw new NotSupportedException();

    internal override (object,int) GetComponents () => (components, 0);

    private         float[] components;
    private         int     stride;

    internal StructSoAVector2(int structIndex)
        : base (structIndex)
    {
        stride      = SimdUtils.CalcCapacity<T>(ArchetypeUtils.MinCapacity);
        components  = new float[stride * LaneCount];
    }

    internal override ref T GetComponentRef(int index) {
        throw new InvalidOperationException($"Component '{typeof(T).Name}' is stored as SoA. GetComponent() requires AoS storage. Remove attribute [SoA] or use GetSoA() instead.");
    }

    internal override T GetComponentValue(int index) {
        return GetComponentFromSoA (components, index, stride);
    }

    internal override void SetComponent(int index, T component) {
        ComponentToSoA(component, components, index, stride);
    }

    internal override T GetSoA(int index) {
        return GetComponentFromSoA(components, index, stride);
    }


    internal override void StashComponent(int compIndex) {
        componentStash = GetComponentFromSoA(components, compIndex, stride);
    }

    internal override  void SetBatchComponent(BatchComponent batchComponent, int compIndex)
    {
        ComponentToSoA(((BatchComponent<T>)batchComponent).value, components, compIndex, stride);
    }

    protected override  int     ComponentsLength    => components.Length / LaneCount;

    internal  override  Type    StructType          => typeof(T);

    internal override void ResizeComponents    (int capacity, int count) {
        int srcStride   = stride;
        stride          = SimdUtils.CalcCapacity<T>(capacity);
        var dst         = new float[stride * LaneCount];
        var src         = components;
        new ReadOnlySpan<float>(src, 0,             count).CopyTo(new Span<float>(dst, 0,       count));
        new ReadOnlySpan<float>(src, srcStride,     count).CopyTo(new Span<float>(dst, stride,  count));
        components = dst;
    }

    internal override void MoveComponent(int from, int to)
    {
        var localComponents = components;
        var localStride     = stride;
        localComponents[to]                     = localComponents[from];
        localComponents[to + localStride]       = localComponents[from + localStride];
    }

    internal override void CopyComponentTo(int sourcePos, StructHeap targetHeap, int targetPos)
    {
        var targetSoA       = (StructSoAVector2<T>)targetHeap;
        var src             = components;
        var dst             = targetSoA.components;
        var targetStride    = targetSoA.stride;
        dst[targetPos]                      = src[sourcePos];
        dst[targetPos + targetStride]       = src[sourcePos + stride];
    }


    internal  override  void SetComponentDefault (int compIndex) {
        ComponentToSoA(default, components, compIndex, stride);
    }

    internal  override  void SetComponentsDefault (int compIndexStart, int count)
    {
        new Span<float>(components, compIndexStart,              count).Clear();
        new Span<float>(components, compIndexStart + stride,     count).Clear();
    }

    internal override object GetComponentDebug(int compIndex) => GetComponentFromSoA(components, compIndex, stride);


    internal override Bytes Write(ObjectWriter writer, int compIndex) {
        var mapper = (TypeMapper<T>)writer.TypeCache.GetTypeMapper(typeof(T));
        var value = GetComponentFromSoA(components, compIndex, stride);
        return writer.WriteAsBytesMapper(value, mapper);
    }

    internal override void Read(ObjectReader reader, int compIndex, JsonValue json) {
        var mapper = (TypeMapper<T>)reader.TypeCache.GetTypeMapper(typeof(T));
        var value = reader.ReadMapper(mapper, json);
        ComponentToSoA(value, components, compIndex, stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ComponentToSoA(T src, float[] dst, int index, int stride)
    {
        Span<float> span        = MemoryMarshal.CreateSpan(ref Unsafe.As<T, float>(ref src), LaneCount);
        dst[index]              = span[0];
        dst[index + stride]     = span[1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T GetComponentFromSoA(float[] src, int index, int stride)
    {
        T result = default;
        var component   = MemoryMarshal.CreateSpan(ref Unsafe.As<T, float>(ref result), LaneCount);
        component[0] = src[index];
        component[1] = src[index + stride];
        return result;
    }
}
