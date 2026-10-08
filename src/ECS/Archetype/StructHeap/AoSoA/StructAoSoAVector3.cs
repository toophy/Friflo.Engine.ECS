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

internal sealed class StructAoSoAVector3<T> : StructHeap<T>
    where T : struct
{
    private const   int     FieldCount = 3;

    public override T[]     Components      => throw new NotSupportedException();

    internal override (object,int) GetComponents () => (components, simdOffset);

    private         float[] components;
    private         int     simdOffset;


    internal StructAoSoAVector3(int structIndex)
        : base (structIndex)
    {
        var capacity = SimdUtils.CalcCapacity<T>(ArchetypeUtils.MinCapacity);
        components  = AllocateAligned(capacity * FieldCount, out simdOffset);
    }

    internal override ref T GetComponentRef(int index) {
        throw new InvalidOperationException($"Component '{typeof(T).Name}' is stored as AoSoA. GetComponent() requires AoS storage. Remove attribute [AoSoA] or use GetSoA() instead.");
    }

    internal override T GetComponentValue(int index) {
        return GetComponentFromSoA (index);
    }

    internal override void SetComponent(int index, T component) {
        ComponentToSoA(component, index);
    }

    internal override T GetSoA(int index) {
        return GetComponentFromSoA(index);
    }


    internal override void StashComponent(int compIndex) {
        componentStash = GetComponentFromSoA(compIndex);
    }

    internal override  void SetBatchComponent(BatchComponent batchComponent, int compIndex)
    {
        ComponentToSoA(((BatchComponent<T>)batchComponent).value, compIndex);
    }

    protected override  int     ComponentsLength    => (components.Length - simdOffset) / FieldCount;

    internal  override  Type    StructType          => typeof(T);

    internal override void ResizeComponents    (int newCapacity, int count)
    {
        var capacity    = SimdUtils.CalcCapacity<T>(newCapacity);
        var oldOffset   = simdOffset;
        var dst         = AllocateAligned(capacity * FieldCount, out simdOffset);
        int tilesToCopy = (count + 7) >> 3;
        int floatsToCopy = tilesToCopy * 24;
        if (floatsToCopy > 0) {
            new ReadOnlySpan<float>(components, oldOffset, floatsToCopy).CopyTo(new Span<float>(dst, simdOffset, floatsToCopy));
        }
        components = dst;
    }

    internal override void MoveComponent(int from, int to)
    {
        ComponentToSoA(GetComponentFromSoA(from), to);
    }

    internal override void CopyComponentTo(int sourcePos, StructHeap targetHeap, int targetPos)
    {
        var targetSoA       = (StructAoSoAVector3<T>)targetHeap;
        targetSoA.ComponentToSoA(GetComponentFromSoA(sourcePos), targetPos);
    }


    internal  override  void SetComponentDefault (int compIndex) {
        ComponentToSoA(default, compIndex);
    }

    internal  override  void SetComponentsDefault (int compIndexStart, int count)
    {
        int i = 0;
        var localComponents = components;
        while (i < count && ((compIndexStart + i) & 7) != 0) {
            ComponentToSoA(default, compIndexStart + i++);
        }
        int remaining = count - i;
        int fullTiles = remaining >> 3;

        if (fullTiles > 0) {
            int stride      = SimdUtils.LaneWidth * FieldCount;
            int startTile   = (compIndexStart + i) >> 3;
            int startFloat  = startTile * stride;
            int totalFloats = fullTiles * stride;
            localComponents.AsSpan(simdOffset + startFloat, totalFloats).Clear();
            i += fullTiles << 3;
        }
        while (i < count) {
            ComponentToSoA(default, compIndexStart + i++);
        }
    }

    internal override object GetComponentDebug(int compIndex) => GetComponentFromSoA(compIndex);


    internal override Bytes Write(ObjectWriter writer, int compIndex) {
        var mapper = (TypeMapper<T>)writer.TypeCache.GetTypeMapper(typeof(T));
        var value = GetComponentFromSoA(compIndex);
        return writer.WriteAsBytesMapper(value, mapper);
    }

    internal override void Read(ObjectReader reader, int compIndex, JsonValue json) {
        var mapper = (TypeMapper<T>)reader.TypeCache.GetTypeMapper(typeof(T));
        var value = reader.ReadMapper(mapper, json);
        ComponentToSoA(value, compIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ComponentToSoA(T src, int index)
    {
        float[] dst   = components;
        int tileIndex = index >> 3;
        int lane      = index & 7;

        int tileStart = tileIndex * 24;
        int baseIdx   = simdOffset + tileStart + lane;

        ref float srcBase = ref Unsafe.As<T, float>(ref src);

        dst[baseIdx + 16] = Unsafe.Add(ref srcBase, 2);
        dst[baseIdx + 8]  = Unsafe.Add(ref srcBase, 1);
        dst[baseIdx]      = srcBase;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T GetComponentFromSoA(int index)
    {
        float[] src   = components;
        int tileIndex = index >> 3;
        int lane      = index & 7;
        int tileStart = tileIndex * 24;
        int baseIdx   = simdOffset + tileStart + lane;

        T result = default;
        ref float resBase = ref Unsafe.As<T, float>(ref result);
        Unsafe.Add(ref resBase, 2) = src[baseIdx + 16];
        Unsafe.Add(ref resBase, 1) = src[baseIdx + 8];
        resBase                    = src[baseIdx];
        return result;
    }
}
