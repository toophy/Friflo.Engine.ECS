using System;
using System.Numerics;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.ECS;

namespace Tests.SoA
{
public static class Test_LayoutRegression
{
    public struct LayoutKey : IIndexedComponent<int>
    {
        public int value;
        public int GetIndexedValue() => value;
    }

    public struct IndexedVector : IIndexedComponent<Vector2>
    {
        public Vector2 value;
        public Vector2 GetIndexedValue() => value;
    }

    [AoSAligned]
    public struct IndexedAlignedVector : IIndexedComponent<Vector2>
    {
        public Vector2 value;
        public Vector2 GetIndexedValue() => value;
    }

    [SoA]
    public struct IndexedSoAVector : IIndexedComponent<Vector2>
    {
        public Vector2 value;
        public Vector2 GetIndexedValue() => value;
    }

    [AoSoA]
    public struct IndexedAoSoAVector : IIndexedComponent<Vector2>
    {
        public Vector2 value;
        public Vector2 GetIndexedValue() => value;
    }

    [TestCase(0, false)] [TestCase(0, true)]
    [TestCase(1, false)] [TestCase(1, true)]
    [TestCase(2, false)] [TestCase(2, true)]
    [TestCase(3, false)] [TestCase(3, true)]
    [TestCase(4, false)] [TestCase(4, true)]
    [TestCase(5, false)] [TestCase(5, true)]
    [TestCase(6, false)] [TestCase(6, true)]
    [TestCase(7, false)] [TestCase(7, true)]
    public static void NestedMemberUsesPathAndPreservesCallbackChanges(int layout, bool write)
    {
        switch (layout) {
            case 0: AssertNestedMember(new Pos2 { value = new Vector2(11, 12) }, new Pos2 { value = new Vector2(11, 101) }, new Pos2 { value = new Vector2(201, 202) }, write, false); break;
            case 1: AssertNestedMember(new Pos4 { value = new Vector4(11, 12, 13, 14) }, new Pos4 { value = new Vector4(11, 101, 13, 14) }, new Pos4 { value = new Vector4(201, 202, 203, 204) }, write, false); break;
            case 2: AssertNestedMember(new Pos2SoA { value = new Vector2(11, 12) }, new Pos2SoA { value = new Vector2(11, 101) }, new Pos2SoA { value = new Vector2(201, 202) }, write, true); break;
            case 3: AssertNestedMember(new Pos3SoA { value = new Vector3(11, 12, 13) }, new Pos3SoA { value = new Vector3(11, 101, 13) }, new Pos3SoA { value = new Vector3(201, 202, 203) }, write, true); break;
            case 4: AssertNestedMember(new Pos4SoA { value = new Vector4(11, 12, 13, 14) }, new Pos4SoA { value = new Vector4(11, 101, 13, 14) }, new Pos4SoA { value = new Vector4(201, 202, 203, 204) }, write, true); break;
            case 5: AssertNestedMember(new Pos2AoSoA { value = new Vector2(11, 12) }, new Pos2AoSoA { value = new Vector2(11, 101) }, new Pos2AoSoA { value = new Vector2(201, 202) }, write, true); break;
            case 6: AssertNestedMember(new Pos3AoSoA { value = new Vector3(11, 12, 13) }, new Pos3AoSoA { value = new Vector3(11, 101, 13) }, new Pos3AoSoA { value = new Vector3(201, 202, 203) }, write, true); break;
            case 7: AssertNestedMember(new Pos4AoSoA { value = new Vector4(11, 12, 13, 14) }, new Pos4AoSoA { value = new Vector4(11, 101, 13, 14) }, new Pos4AoSoA { value = new Vector4(201, 202, 203, 204) }, write, true); break;
        }
    }

    private static void AssertNestedMember<T>(T initial, T afterSetter, T afterCallback, bool write, bool soa)
        where T : struct, IComponent
    {
        var store = new EntityStore();
        var entity = store.CreateEntity(initial);
        var path = MemberPath.Get(typeof(T), "value.Y");
        if (!write) {
            Assert.IsTrue(EntityUtils.GetEntityComponentMember<float>(entity, path, out var field, out var exception));
            Assert.IsNull(exception);
            Assert.AreEqual(12, field);
            return;
        }
        int calls = 0;
        OnMemberChanged<T> handler = (ref T component, Entity changed, string name, in T old) => {
            Assert.AreEqual(entity, changed);
            Assert.AreEqual("value.Y", name);
            Assert.AreEqual(initial, old);
            Assert.AreEqual(afterSetter, component);
            Assert.AreEqual(afterSetter, soa ? changed.GetSoA<T>() : changed.GetComponent<T>());
            component = afterCallback;
            calls++;
        };
        Assert.IsTrue(EntityUtils.SetEntityComponentMember(entity, path, 101f, handler, out var error), error?.ToString());
        Assert.IsNull(error);
        Assert.AreEqual(1, calls);
        Assert.AreEqual(afterCallback, soa ? entity.GetSoA<T>() : entity.GetComponent<T>());
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public static void IndexedLayoutMaintainsIndexAcrossLifecycle(int layout)
    {
        switch (layout) {
            case 0: AssertIndexedLifecycle(new IndexedVector { value = new Vector2(11, 12) }, new IndexedVector { value = new Vector2(21, 22) }); break;
            case 1: AssertIndexedLifecycle(new IndexedAlignedVector { value = new Vector2(11, 12) }, new IndexedAlignedVector { value = new Vector2(21, 22) }); break;
            case 2: AssertIndexedLifecycle(new IndexedSoAVector { value = new Vector2(11, 12) }, new IndexedSoAVector { value = new Vector2(21, 22) }); break;
            case 3: AssertIndexedLifecycle(new IndexedAoSoAVector { value = new Vector2(11, 12) }, new IndexedAoSoAVector { value = new Vector2(21, 22) }); break;
        }
    }

    private static void AssertIndexedLifecycle<T>(T initial, T replacement) where T : struct, IIndexedComponent<Vector2>
    {
        var store = new EntityStore();
        var index = store.ComponentIndex<T, Vector2>();
        var entity = store.CreateEntity(initial);
        Assert.AreEqual(1, index[initial.GetIndexedValue()].Count);
        entity.Set(replacement);
        Assert.AreEqual(0, index[initial.GetIndexedValue()].Count);
        Assert.AreEqual(1, index[replacement.GetIndexedValue()].Count);
        var batch = new EntityBatch().Add(initial);
        batch.ApplyTo(entity);
        entity.AddComponent(new Position(1, 2, 3));
        Assert.AreEqual(1, index[initial.GetIndexedValue()].Count);
        Assert.AreEqual(0, index[replacement.GetIndexedValue()].Count);
        var clone = entity.CloneEntity();
        Assert.AreEqual(2, index[initial.GetIndexedValue()].Count);
        clone.Set(replacement);
        entity.CopyEntity(clone);
        Assert.AreEqual(2, index[initial.GetIndexedValue()].Count);
        Assert.AreEqual(0, index[replacement.GetIndexedValue()].Count);
        clone.RemoveComponent<T>();
        Assert.AreEqual(1, index[initial.GetIndexedValue()].Count);
        entity.DeleteEntity();
        Assert.AreEqual(0, index.Values.Count);
    }

    [TestCase(false)] [TestCase(true)]
    public static void BulkAlignedCreationPreservesExistingComponents(bool vector4)
    {
        if (vector4) AssertBulkCreation(new Pos4 { value = new Vector4(11, 12, 13, 14) });
        else AssertBulkCreation(new Pos2 { value = new Vector2(11, 12) });
    }

    private static void AssertBulkCreation<T>(T initial) where T : struct, IComponent
    {
        var store = new EntityStore();
        var first = store.CreateEntity(initial);
        var created = first.Archetype.CreateEntities(3);
        Assert.AreEqual(initial, first.GetComponent<T>());
        foreach (var entity in created) Assert.AreEqual(default(T), entity.GetComponent<T>());
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public static void AoSoARefRejectionNamesActualLayout(int dimension)
    {
        if (dimension == 0) AssertRefRejection<Pos2AoSoA>();
        else if (dimension == 1) AssertRefRejection<Pos3AoSoA>();
        else AssertRefRejection<Pos4AoSoA>();
    }

    private static void AssertRefRejection<T>() where T : struct, IComponent
    {
        var store = new EntityStore();
        var entity = store.CreateEntity(default(T));
        var error = Assert.Throws<InvalidOperationException>(() => entity.GetComponent<T>());
        StringAssert.Contains("stored as AoSoA", error.Message);
        StringAssert.Contains("Remove attribute [AoSoA]", error.Message);
        foreach (var (chunk, _) in store.Query<T>().Chunks) {
            Assert.Throws<InvalidOperationException>(() => chunk.GetComponentSpan().ToArray());
            Assert.Throws<InvalidOperationException>(() => chunk.AsSpan256<float>().ToArray());
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public static void FilteredChunkUpdatesOnlySelectedEntity(bool tiled, bool parallel)
    {
        if (tiled) {
            AssertFilteredChunk(new Pos2AoSoA { value = new Vector2(1, 2) },
                new Pos2AoSoA { value = new Vector2(91, 92) },
                new Pos2AoSoA { value = new Vector2(501, 502) },
                chunk => chunk.GetAoSoA(0), (chunk, value) => chunk.SetAoSoA(0, value), true, parallel);
        } else {
            AssertFilteredChunk(new Pos2SoA { value = new Vector2(1, 2) },
                new Pos2SoA { value = new Vector2(91, 92) },
                new Pos2SoA { value = new Vector2(501, 502) },
                chunk => chunk.GetSoA(0), (chunk, value) => chunk.SetSoA(0, value), false, parallel);
        }
    }

    private static void AssertFilteredChunk<T>(T initial, T selected, T replacement,
        Func<Chunk<T>, T> read, Action<Chunk<T>, T> write, bool tiled, bool parallel)
        where T : struct, IComponent
    {
        using var runner = new ParallelJobRunner(2);
        var store = new EntityStore { JobRunner = runner };
        var first = store.CreateEntity(initial, new LayoutKey { value = 0 });
        for (int n = 1; n < 9; n++) store.CreateEntity(initial, new LayoutKey { value = n });
        var target = store.CreateEntity(selected, new LayoutKey { value = 9 });
        var query = store.Query<T>().HasValue<LayoutKey, int>(9);
        int calls = 0;
        Action<Chunk<T>, ChunkEntities> action = (chunk, entities) => {
            if (chunk.Length == 0) return;
            Assert.AreEqual(1, chunk.Length);
            Assert.AreEqual(target.Id, entities[0]);
            Assert.AreEqual(selected, read(chunk));
            if (tiled) {
                Assert.Throws<InvalidOperationException>(() => chunk.GetLanesSoA().ToArray());
            } else {
                Assert.AreEqual(91, chunk.GetLanesSoA()[0]);
                Assert.AreEqual(92, chunk.GetLanesSoA()[chunk.GetStrideSoA()]);
            }
            write(chunk, replacement);
            calls++;
        };
        if (parallel) {
            var job = query.ForEach(action);
            job.MinParallelChunkLength = 1;
            job.RunParallel();
        } else {
            foreach (var (chunk, entities) in query.Chunks) action(chunk, entities);
        }
        Assert.AreEqual(1, calls);
        Assert.AreEqual(initial, first.GetSoA<T>());
        Assert.AreEqual(replacement, target.GetSoA<T>());
    }
}
}
