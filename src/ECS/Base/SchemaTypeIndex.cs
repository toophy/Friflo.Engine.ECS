// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal static class SchemaTypeIndex
{
    internal const int MaxValue = ushort.MaxValue;

    internal static int Next(int count, string category)
    {
        if ((uint)count < MaxValue) return count + 1;
        throw new InvalidOperationException($"The number of {category} types exceeds the supported maximum of {MaxValue}.");
    }

    internal static ushort ToUInt16(int index, string parameterName)
    {
        if ((uint)index <= MaxValue) return (ushort)index;
        throw new ArgumentOutOfRangeException(parameterName, index, $"Schema type index must be in range [0, {MaxValue}].");
    }
}
