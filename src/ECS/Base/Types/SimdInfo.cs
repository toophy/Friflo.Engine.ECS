// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable CheckNamespace

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Friflo.Engine.ECS;

internal enum Layout
{
                            AoS,
      AoSAligned,
      SoA,
      AoSoA
}

public static class SimdInfo<T>
    where T : struct
{
    internal static readonly  Layout  Layout        = SimdUtils.GetLayout<T>();

    public static readonly    int     FieldCountSoA = SimdUtils.GetFieldCountSoA<T>();

    public static readonly    int     ComponentStep = SimdUtils.GetComponentStep<T>();
}


internal static class SimdUtils
{
    internal const int LaneWidth = 8;

    internal static Layout GetLayout<T>()  where T : struct
    {
        var fieldCount = GetFieldCountSoA<T>();
        var type = typeof(T);
        return GetLayout(type, fieldCount);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2090", Justification = "NativeAOT registration preserves public fields of component and relation types.")]
    internal static int GetFieldCountSoA<T>()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) return 0;
        var type        = typeof(T);
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (var field in fields)
        {
            var dimension = GetDimension(field);
            if (dimension > 0) {
                return Unsafe.SizeOf<T>() == dimension * 4 ? dimension : 0;
            }
        }
        return 0;
    }

    private static int GetDimension(FieldInfo fieldInfo)
    {
        var fieldName = fieldInfo.Name;
        if (fieldName != "value" && fieldName != "Value") {
            return 0;
        }
        var fieldType = fieldInfo.FieldType;
        if (fieldType == typeof(float))     return 1;
        if (fieldType == typeof(Vector2))   return 2;
        if (fieldType == typeof(Vector3))   return 3;
        if (fieldType == typeof(Vector4))   return 4;
        if (fieldType == typeof(Quaternion))return 4;
        return 0;
    }

    private static Layout GetLayout(Type type, int fieldCount)
    {
        foreach (var attr in type.CustomAttributes)
        {
            var attributeType = attr.AttributeType;
            if ((attributeType == typeof(AoSoAAttribute) || attributeType == typeof(SoAAttribute) ||
                 attributeType == typeof(AoSAlignedAttribute)) && typeof(IRelation).IsAssignableFrom(type)) {
                throw new ArgumentException($"Relation '{type.Name}' does not support explicit component layouts.");
            }
            if (attributeType == typeof(AoSoAAttribute)) {
                if (fieldCount == 2 || fieldCount == 3 || fieldCount == 4) return Layout.AoSoA;
                throw new ArgumentException($"Component '{type.Name}' does not support AoSoA storage.");
            }
            if (attributeType == typeof(SoAAttribute)) {
                if (fieldCount == 2 || fieldCount == 3 || fieldCount == 4) return Layout.SoA;
                throw new ArgumentException($"Component '{type.Name}' does not support SoA storage.");
            }
            if (attributeType == typeof(AoSAlignedAttribute)) {
                if (fieldCount == 1 || fieldCount == 2 || fieldCount == 4) return Layout.AoSAligned;
                throw new ArgumentException($"Component '{type.Name}' does not support aligned AoS storage.");
            }
        }
        return Layout.AoS;
    }

    internal static int  GetComponentStep<T>() where T : struct
    {
        var fieldCount = SimdInfo<T>.FieldCountSoA;
        switch (fieldCount)
        {
            case 1: return 32;
            case 2: return 16;
            case 3: return 8;
            case 4: return 8;
        }
        return 0;
    }


    internal static int CalcCapacity<TComponent>(int count) where TComponent : struct
    {
        var step = SimdInfo<TComponent>.ComponentStep;
        if (step <= 0) return count + Math.Max(0, StructPadding<TComponent>.PadCount512);

        int paddedCount = (count + step - 1) & ~(step - 1);

        return paddedCount + step;
    }
}
