// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// A <see cref="Chunk{T}"/> is container of <b>struct</b> components of Type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// <see cref="Chunk{T}"/>'s are typically returned a <see cref="ArchetypeQuery{T1}"/>.<see cref="ArchetypeQuery{T1}.Chunks"/> enumerator.<br/>
/// <br/>
/// Its items can be accessed or changed with <see cref="this[int]"/> or <see cref="Span"/>.<br/>
/// </remarks>
/// <typeparam name="T"><see cref="IComponent"/> type of a struct component.</typeparam>
[DebuggerTypeProxy(typeof(ChunkDebugView<>))]
public struct Chunk<T>
    where T : struct
{
    /// <summary> Return the number of components in a <see cref="Chunk{T}"/>. </summary>
    public   readonly   int         Length;                 //  4

    public   readonly   T[]         ArchetypeComponents;
    
    // ReSharper disable once NotAccessedField.Local
    private  readonly   int         start;                  //  4
    
    private  readonly   int         simdOffset;
    
    private             object      _components;
    
    public              Span<T>     Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetComponentSpan().Slice(0, Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetComponentSpan()
    {
        if (SimdInfo<T>.Layout == Layout.AoS) {
            return Unsafe.As<T[]>(_components).AsSpan(start);
        }
        if (SimdInfo<T>.Layout == Layout.AoSAligned) {
#if NET6_0_OR_GREATER
            if (_components == null) return Span<T>.Empty;
            float[] buffer = Unsafe.As<object, float[]>(ref _components);
            int floatsPerT = Unsafe.SizeOf<T>() / sizeof(float);
            int startIdx   = simdOffset + (start * floatsPerT);
            int paddedLength = (buffer.Length - startIdx) / floatsPerT;

            return MemoryMarshal.CreateSpan(
                ref Unsafe.As<float, T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), startIdx)), paddedLength);
#else
            float[] buffer      = Unsafe.As<object, float[]>(ref _components);
            int floatsPerT      = Unsafe.SizeOf<T>() / sizeof(float);
            int startInFloats   = simdOffset + (start * floatsPerT);
            return MemoryMarshal.Cast<float, T>(buffer.AsSpan(startInFloats));
#endif
        }
        throw ChunkExtensions.ExpectCallForAoSComponent();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<float> GetLanesSoA()
    {
        if (SimdInfo<T>.Layout != Layout.SoA && SimdInfo<T>.Layout != Layout.AoSoA) ChunkExtensions.ExpectCallForSoAComponent();
        if (SimdInfo<T>.Layout == Layout.SoA) {
            return Unsafe.As<object, float[]>(ref _components).AsSpan(simdOffset + start);
        }
        if ((start & 7) != 0) throw new InvalidOperationException("AoSoA lanes require a chunk start aligned to a tile boundary.");
        var offset = (start >> 3) * SimdInfo<T>.FieldCountSoA * SimdUtils.LaneWidth;
        return Unsafe.As<object, float[]>(ref _components).AsSpan(simdOffset + offset);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetStrideSoA() {
        if (SimdInfo<T>.Layout != Layout.SoA) ChunkExtensions.ExpectCallForSoAComponent();
        var lanes = Unsafe.As<object, float[]>(ref _components);
        return lanes.Length / SimdInfo<T>.FieldCountSoA;
    }
    
    public T GetSoA(int index)
    {
        if (SimdInfo<T>.Layout != Layout.SoA) ChunkExtensions.ExpectCallForSoAComponent();
        if ((uint)index >= (uint)Length) throw new IndexOutOfRangeException();
        index += simdOffset + start;
        var lanes  = Unsafe.As<object, float[]>(ref _components);
        var stride = lanes.Length / SimdInfo<T>.FieldCountSoA;
        T result = default;
        Span<float> component = MemoryMarshal.CreateSpan(ref Unsafe.As<T, float>(ref result), SimdInfo<T>.FieldCountSoA);
        switch (SimdInfo<T>.FieldCountSoA) {
            case 1: goto Count_1;
            case 2: goto Count_2;
            case 3: goto Count_3;
        }
        component[3] = lanes[index + stride * 3];
    Count_3:
        component[2] = lanes[index + stride * 2];
    Count_2:
        component[1] = lanes[index + stride];
    Count_1:
        component[0] = lanes[index];
        return result;
    }
    
    public void SetSoA(int index, T value)
    {
        if (SimdInfo<T>.Layout != Layout.SoA) ChunkExtensions.ExpectCallForSoAComponent();
        if ((uint)index >= (uint)Length) throw new IndexOutOfRangeException();
        index += simdOffset + start;
        var lanes       = Unsafe.As<object, float[]>(ref _components);
        var stride      = lanes.Length / SimdInfo<T>.FieldCountSoA;
        var component   = MemoryMarshal.CreateSpan(ref Unsafe.As<T, float>(ref value), SimdInfo<T>.FieldCountSoA);
        switch (SimdInfo<T>.FieldCountSoA) {
            case 1: goto Count_1;
            case 2: goto Count_2;
            case 3: goto Count_3;
        }
        lanes[index + stride * 3] = component[3];
    Count_3:
        lanes[index + stride * 2] = component[2];
    Count_2:
        lanes[index + stride]     = component[1];
    Count_1:
        lanes[index]              = component[0];
    }
    
    public T GetAoSoA(int index)
    {
        if (SimdInfo<T>.Layout != Layout.AoSoA) ChunkExtensions.ExpectCallForAoSoAComponent();
        if ((uint)index >= (uint)Length) throw new IndexOutOfRangeException();
        index += start;
        int step        = SimdUtils.LaneWidth;
        int tileIndex   = index >> 3; 
        int lane        = index & 7;  
        var fieldCount  = SimdInfo<T>.FieldCountSoA;
        int tileStart   = simdOffset + tileIndex * (fieldCount * step);

        T result = default;
        ref float componentBase = ref Unsafe.As<T, float>(ref result);
        var components = Unsafe.As<object, float[]>(ref _components);
        switch (fieldCount)
        {
            case 4:
                Unsafe.Add(ref componentBase, 3) = components[tileStart + lane + (step * 3)];
                goto case 3;
            case 3:
                Unsafe.Add(ref componentBase, 2) = components[tileStart + lane + (step * 2)];
                goto case 2;
            case 2:
                Unsafe.Add(ref componentBase, 1) = components[tileStart + lane + step];
                Unsafe.Add(ref componentBase, 0) = components[tileStart + lane];
                break;
        }
        return result;
    }
    
    public void SetAoSoA(int index, T value)
    {
        if (SimdInfo<T>.Layout != Layout.AoSoA) ChunkExtensions.ExpectCallForAoSoAComponent();
        if ((uint)index >= (uint)Length) throw new IndexOutOfRangeException();
        index += start;
        int step        = SimdUtils.LaneWidth;       
        int tileIndex   = index >> 3; 
        int lane        = index & 7;  
        var fieldCount  = SimdInfo<T>.FieldCountSoA;
        int tileStart   = simdOffset + tileIndex * (fieldCount * step);

        ref float valueBase = ref Unsafe.As<T, float>(ref value);
        var components = Unsafe.As<object, float[]>(ref _components);
        int baseIdx = tileStart + lane;
        switch (fieldCount)
        {
            case 4:
                components[baseIdx + (step * 3)] = Unsafe.Add(ref valueBase, 3);
                goto case 3;
            case 3:
                components[baseIdx + (step * 2)] = Unsafe.Add(ref valueBase, 2);
                goto case 2;
            case 2:
                components[baseIdx + step]       = Unsafe.Add(ref valueBase, 1);
                components[baseIdx]              = valueBase;
                break;
        }
    }
    public Span<TTo> AsSpan128<TTo>() where TTo : struct
        => GetPaddedSpan<TTo>(StructPadding<T>.PadCount128, 0x7fff_fff0);

    public Span<TTo> AsSpan256<TTo>() where TTo : struct
        => GetPaddedSpan<TTo>(StructPadding<T>.PadCount256, 0x7fff_ffe0);

    public Span<TTo> AsSpan512<TTo>() where TTo : struct
        => GetPaddedSpan<TTo>(StructPadding<T>.PadCount512, 0x7fff_ffc0);

    [Browse(Never)]
    public int StepSpan128 => 16 / StructPadding<T>.ByteSize;

    [Browse(Never)]
    public int StepSpan256 => 32 / StructPadding<T>.ByteSize;

    [Browse(Never)]
    public int StepSpan512 => 64 / StructPadding<T>.ByteSize;

    private Span<TTo> GetPaddedSpan<TTo>(int padding, int mask) where TTo : struct
    {
        if (SimdInfo<T>.Layout != Layout.AoS) throw ChunkExtensions.ExpectCallForAoSComponent();
        return MemoryMarshal.Cast<T, TTo>(new Span<T>(ArchetypeComponents, start, (Length + padding) & mask));
    }
    public override     string      ToString()  => $"{typeof(T).Name}[{Length}]";


    internal Chunk((object components, int simdOffset) heap, int length, int start) {
        Length      = length;
        this.start  = start;
        _components = heap.components;
        simdOffset  = heap.simdOffset;
        ArchetypeComponents = SimdInfo<T>.Layout == Layout.AoS ? (T[])heap.components : null;
    }
    
    internal Chunk(Chunk<T> chunk, int start, int length) {
        Length      = length;
        this.start  = chunk.start + start;
        _components = chunk._components;
        simdOffset  = chunk.simdOffset;
        ArchetypeComponents = chunk.ArchetypeComponents;
    }
    
    /// <summary> Return the component at the passed <paramref name="index"/> as a reference. </summary>
    public ref T this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if ((uint)index < (uint)Length) {
                if (SimdInfo<T>.Layout == Layout.AoS) {
                    return ref Unsafe.As<T[]>(_components)[start + index];
                }
                if (SimdInfo<T>.Layout == Layout.AoSAligned) {
#if NET6_0_OR_GREATER
                    float[] buffer = Unsafe.As<object, float[]>(ref _components);
                    int floatsPerT = Unsafe.SizeOf<T>() / sizeof(float);
                    return ref Unsafe.As<float, T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), simdOffset + (start + index) * floatsPerT));
#else
                    return ref Unsafe.As<T[]>(_components)[start + index];  
#endif
                }
                throw ChunkExtensions.ExpectCallForAoSComponent();
            }
            throw new IndexOutOfRangeException();
        }
    }
}

internal class ChunkDebugView<T>
    where T : struct, IComponent
{
    [Browse(RootHidden)]
    public      Array       Components => GetComponents();

    [Browse(Never)]
    private     Chunk<T>    chunk;
        
    internal ChunkDebugView(Chunk<T> chunk)
    {
        this.chunk = chunk;
    }
    
    private Array GetComponents()
    {
        switch (SimdInfo<T>.Layout)
        {
            case Layout.AoS:
            case Layout.AoSAligned:
                return chunk.Span.ToArray();
            case Layout.SoA:
                var components = new T[chunk.Length];
                for (int n = 0; n < chunk.Length; n++) {
                    components[n] = chunk.GetSoA(n);
                }
                return components;
            case Layout.AoSoA:
                components = new T[chunk.Length];
                for (int n = 0; n < chunk.Length; n++) {
                    components[n] = chunk.GetAoSoA(n);
                }
                return components;
        }
        return null;
    }
}

public static class ChunkExtensions
{
    public static Span<Vector3> AsSpanVector3(this Span<Position> position) => MemoryMarshal.Cast<Position, Vector3>(position);
    public static Span<Vector3> AsSpanVector3(this Chunk<Position> position) => MemoryMarshal.Cast<Position, Vector3>(position.Span);
    public static Span<Quaternion> AsSpanQuaternion(this Span<Rotation> rotation) => MemoryMarshal.Cast<Rotation, Quaternion>(rotation);
    public static Span<Quaternion> AsSpanQuaternion(this Chunk<Rotation> rotation) => MemoryMarshal.Cast<Rotation, Quaternion>(rotation.Span);
    public static Span<Vector3> AsSpanVector3(this Span<Scale3> scale) => MemoryMarshal.Cast<Scale3, Vector3>(scale);
    public static Span<Vector3> AsSpanVector3(this Chunk<Scale3> scale) => MemoryMarshal.Cast<Scale3, Vector3>(scale.Span);
    public static Span<Matrix4x4> AsSpanMatrix4x4(this Span<Transform> transform) => MemoryMarshal.Cast<Transform, Matrix4x4>(transform);
    public static Span<Matrix4x4> AsSpanMatrix4x4(this Chunk<Transform> transform) => MemoryMarshal.Cast<Transform, Matrix4x4>(transform.Span);
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ExpectCallForRegularComponent() {
        throw new InvalidOperationException("Expect call for AoS component data.");
    }
    
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ExpectCallForSoAComponent() {
        throw new InvalidOperationException("Expect call for SoA component data.");
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ExpectCallForAoSoAComponent() {
        throw new InvalidOperationException("Expect call for AoSoA component data.");
    }
    
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static InvalidOperationException ExpectCallForAoSComponent() {
        throw new InvalidOperationException("Expect call for AoS component data.");
    }
}
