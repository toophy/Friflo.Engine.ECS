using System;
using System.Numerics;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.ECS;

namespace Tests.SoA
{
public static class Test_ComponentAccessRegression
{
    [Test]
    public static void OrdinaryReferencesFollowGrowthAndMigration()
    {
        AssertReferences(new Position(11, 12, 13), new Position(21, 22, 23));
    }

    [Test]
    public static void AlignedReferencesFollowGrowthAndMigration()
    {
        AssertReferences(new Pos2 { value = new Vector2(11, 12) }, new Pos2 { value = new Vector2(21, 22) });
    }

    private static void AssertReferences<T>(T initial, T replacement) where T : struct, IComponent
    {
        var store = new EntityStore();
        var entity = store.CreateEntity(initial);
        for (int n = 1; n < 513; n++) store.CreateEntity(initial);
        var data = entity.Data;
        data.Get<T>() = replacement;
        Assert.AreEqual(replacement, entity.GetComponent<T>());
        entity.AddComponent(new EntityName("migrated"));
        entity.Data.Get<T>() = initial;
        Assert.AreEqual(initial, entity.GetComponent<T>());
        foreach (var (chunk, _) in store.Query<T>().Chunks) {
            Assert.AreEqual(initial, chunk.GetComponentSpan()[0]);
        }
        entity.RemoveComponent<T>();
        Assert.Throws<NullReferenceException>(() => entity.GetComponent<T>());
    }

    [Test]
    public static void SoADataRejectsReferences()
    {
        var entity = new EntityStore().CreateEntity(new Pos2SoA { value = new Vector2(11, 12) });
        Assert.Throws<InvalidOperationException>(() => { var data = entity.Data; data.Get<Pos2SoA>().value = default; });
        Assert.AreEqual(new Vector2(11, 12), entity.GetSoA<Pos2SoA>().value);
    }

    [Test]
    public static void AoSoADataRejectsReferences()
    {
        var entity = new EntityStore().CreateEntity(new Pos2AoSoA { value = new Vector2(11, 12) });
        Assert.Throws<InvalidOperationException>(() => { var data = entity.Data; data.Get<Pos2AoSoA>().value = default; });
        Assert.AreEqual(new Vector2(11, 12), entity.GetSoA<Pos2AoSoA>().value);
    }
}
}
