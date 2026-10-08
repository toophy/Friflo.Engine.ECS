using System;
using Friflo.Engine.ECS;
using NUnit.Framework;

namespace Tests.SoA
{
public static class Test_ExplicitLayoutValidation
{
    [SoA]
    public struct SoAScalar : IComponent
    {
        public float value;
    }

    [AoSoA]
    public struct AoSoAScalar : IComponent
    {
        public float value;
    }

    [SoA]
    public struct SoAInteger : IComponent
    {
        public int value;
    }

    [AoSoA]
    public struct AoSoAInteger : IComponent
    {
        public int value;
    }

    [AoSAligned]
    public struct AlignedInteger : IComponent
    {
        public int value;
    }

    [Test]
    public static void SoAScalarRejectsEntityCreation() => AssertUnsupported<SoAScalar>("SoA");

    [Test]
    public static void AoSoAScalarRejectsEntityCreation() => AssertUnsupported<AoSoAScalar>("AoSoA");

    [Test]
    public static void SoAIntegerRejectsEntityCreation() => AssertUnsupported<SoAInteger>("SoA");

    [Test]
    public static void AoSoAIntegerRejectsEntityCreation() => AssertUnsupported<AoSoAInteger>("AoSoA");

    [Test]
    public static void AlignedIntegerRejectsEntityCreation() => AssertUnsupported<AlignedInteger>("aligned AoS");

    private static void AssertUnsupported<T>(string layout) where T : struct, IComponent
    {
        var store = new EntityStore();
        var error = Assert.Catch<Exception>(() => store.CreateEntity(default(T))).GetBaseException();
        Assert.That(error, Is.TypeOf<ArgumentException>());
        StringAssert.Contains(typeof(T).Name, error.Message);
        StringAssert.Contains(layout + " storage", error.Message);
        Assert.AreEqual(0, store.Count);
    }
}
}
