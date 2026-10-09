using System;
using System.Numerics;
using Friflo.Engine.ECS;
using NUnit.Framework;

namespace Tests.SoA
{
public static class Test_ExplicitLayoutValidation
{
    [SoA]
    public struct SoAStaticValue : IComponent
    {
        public static Vector2 value;
        public object payload;
    }

    [AoSoA]
    public struct AoSoAStaticValue : IComponent
    {
        public static Vector2 value;
        public object payload;
    }

    [AoSAligned]
    public struct AlignedStaticValue : IComponent
    {
        public static Vector2 value;
        public object payload;
    }

    [SoA]
    public struct SoAStaticUnmanagedValue : IComponent
    {
        public static Vector2 value;
        public long payload;
    }

    [SoA]
    public struct SoAWithStaticValue : IComponent
    {
        public static Vector4 Value;
        public Vector2 value;
    }

    public struct PlainWithStaticValue : IComponent
    {
        public static Vector2 value;
        public object payload;
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public static void StaticValueRejectsEntityCreation(int layout)
    {
        switch (layout) {
            case 0: AssertUnsupported<SoAStaticValue>("SoA"); break;
            case 1: AssertUnsupported<AoSoAStaticValue>("AoSoA"); break;
            case 2: AssertUnsupported<AlignedStaticValue>("aligned AoS"); break;
            case 3: AssertUnsupported<SoAStaticUnmanagedValue>("SoA"); break;
        }
    }

    [Test]
    public static void SoAInstanceValueIgnoresStaticFields()
    {
        var store = new EntityStore();
        var entity = store.CreateEntity(new SoAWithStaticValue { value = new Vector2(11, 12) });
        foreach (var (chunk, _) in store.Query<SoAWithStaticValue>().Chunks) {
            Assert.AreEqual(new Vector2(11, 12), chunk.GetSoA(0).value);
            chunk.SetSoA(0, new SoAWithStaticValue { value = new Vector2(21, 22) });
        }
        Assert.AreEqual(new Vector2(21, 22), entity.GetSoA<SoAWithStaticValue>().value);
    }

    [Test]
    public static void PlainManagedComponentIgnoresStaticFields()
    {
        var store = new EntityStore();
        var payload = new object();
        var entity = store.CreateEntity(new PlainWithStaticValue { payload = payload });
        Assert.AreSame(payload, entity.GetComponent<PlainWithStaticValue>().payload);
        foreach (var (chunk, _) in store.Query<PlainWithStaticValue>().Chunks) {
            Assert.AreSame(payload, chunk.ArchetypeComponents[0].payload);
            chunk[0].payload = null;
        }
        Assert.IsNull(entity.GetComponent<PlainWithStaticValue>().payload);
    }

    [SoA]
    public struct SoARelation : IRelation<float>
    {
        public Vector2 value;
        public float GetRelationKey() => value.X;
    }

    [AoSoA]
    public struct AoSoARelation : IRelation<float>
    {
        public Vector2 value;
        public float GetRelationKey() => value.X;
    }

    [AoSAligned]
    public struct AlignedRelation : IRelation<float>
    {
        public Vector2 value;
        public float GetRelationKey() => value.X;
    }

    public struct PlainVectorRelation : IRelation<float>
    {
        public Vector2 value;
        public float GetRelationKey() => value.X;
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public static void ExplicitRelationRejectsStorageCreation(int layout)
    {
        switch (layout) {
            case 0: AssertUnsupportedRelation(new SoARelation { value = new Vector2(11, 12) }); break;
            case 1: AssertUnsupportedRelation(new AoSoARelation { value = new Vector2(11, 12) }); break;
            case 2: AssertUnsupportedRelation(new AlignedRelation { value = new Vector2(11, 12) }); break;
        }
    }

    private static void AssertUnsupportedRelation<T>(T relation) where T : struct, IRelation<float>
    {
        var store = new EntityStore();
        var entity = store.CreateEntity();
        var error = Assert.Catch<Exception>(() => entity.AddRelation(relation)).GetBaseException();
        Assert.That(error, Is.TypeOf<ArgumentException>());
        StringAssert.Contains(typeof(T).Name, error.Message);
        StringAssert.Contains("relation", error.Message.ToLowerInvariant());
        Assert.AreEqual(1, store.Count);
        Assert.IsFalse(entity.TryGetRelation<T, float>(11, out _));
        var secondError = Assert.Catch<Exception>(() => store.EntityRelations<T>()).GetBaseException();
        Assert.That(secondError, Is.TypeOf<ArgumentException>());
    }

    [Test]
    public static void PlainVectorRelationSharesStorageWithChunk()
    {
        var store = new EntityStore();
        var entity = store.CreateEntity();
        entity.AddRelation(new PlainVectorRelation { value = new Vector2(11, 12) });
        entity.AddRelation(new PlainVectorRelation { value = new Vector2(21, 22) });
        var (entities, relations) = store.EntityRelations<PlainVectorRelation>().Pairs;
        Assert.AreEqual(2, relations.Length);
        Assert.AreEqual(2, entities.Count);
        Assert.AreEqual(new Vector2(11, 12), relations[0].value);
        Assert.AreEqual(new Vector2(21, 22), relations[1].value);
        relations[0].value.Y = 99;
        Assert.AreEqual(new Vector2(11, 99), entity.GetRelation<PlainVectorRelation, float>(11).value);
        entity.DeleteEntity();
        Assert.AreEqual(0, store.EntityRelations<PlainVectorRelation>().Pairs.relations.Length);
    }

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
