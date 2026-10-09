using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.ECS;

namespace Tests.SoA
{
public static class Test_LayoutStateMachine
{
    private sealed class Expected
    {
        public Entity entity;
        public int mask;
        public int value;
        public bool tagged;
    }

    [TestCase(17)] [TestCase(101)] [TestCase(8191)]
    public static void MixedLifecycleMatchesIndependentState(int seed)
    {
        var random = new Random(seed);
        var store = new EntityStore();
        var expected = new Dictionary<int, Expected>();
        var trace = new StringBuilder();
        for (int n = 0; n < 33; n++) Add(store, expected, n, 15);
        for (int step = 0; step < 500; step++) {
            var state = expected.Values.ElementAt(random.Next(expected.Count));
            int operation = random.Next(7);
            int value = random.Next(-1024, 1025);
            int mask = random.Next(16);
            trace.AppendLine($"{step}: op={operation} id={state.entity.Id} value={value} mask={mask}");
            try {
                switch (operation) {
                    case 0:
                        if (expected.Count < 513) Add(store, expected, value, mask);
                        break;
                    case 1:
                        Write(state.entity, state.mask, value);
                        state.value = value;
                        break;
                    case 2:
                        Write(state.entity, mask, state.value);
                        state.mask = mask;
                        break;
                    case 3:
                        state.tagged = !state.tagged;
                        if (state.tagged) state.entity.AddTag<TestTag>();
                        else state.entity.RemoveTag<TestTag>();
                        break;
                    case 4:
                        if (expected.Count > 9) {
                            expected.Remove(state.entity.Id);
                            state.entity.DeleteEntity();
                            Assert.IsTrue(state.entity.IsNull);
                        }
                        break;
                    case 5:
                        var batch = state.entity.Batch();
                        if ((mask & 1) != 0) batch.Add(new Position(value, -value, value + 1));
                        else batch.Remove<Position>();
                        if ((mask & 2) != 0) batch.Add(new Pos2 { value = new Vector2(value, -value) });
                        else batch.Remove<Pos2>();
                        if ((mask & 4) != 0) batch.Add(new Pos3SoA { value = new Vector3(value, -value, value + 1) });
                        else batch.Remove<Pos3SoA>();
                        if ((mask & 8) != 0) batch.Add(new Pos3AoSoA { value = new Vector3(value, -value, value + 1) });
                        else batch.Remove<Pos3AoSoA>();
                        batch.Apply();
                        state.mask = mask;
                        state.value = value;
                        break;
                    case 6:
                        if (expected.Count < 513) {
                            var clone = state.entity.CloneEntity();
                            expected.Add(clone.Id, new Expected { entity = clone, mask = state.mask, value = state.value, tagged = state.tagged });
                        }
                        break;
                }
                AssertStore(store, expected);
            } catch (Exception error) {
                Assert.Fail($"seed={seed}, step={step}\n{trace}\n{error}");
            }
        }
    }

    [TestCase(0)] [TestCase(1)] [TestCase(7)] [TestCase(8)] [TestCase(9)]
    [TestCase(255)] [TestCase(256)] [TestCase(257)] [TestCase(511)] [TestCase(512)] [TestCase(513)]
    public static void GrowthAndSwapRemovalPreserveLanes(int count)
    {
        var store = new EntityStore();
        var expected = new Dictionary<int, Expected>();
        for (int n = 0; n < count; n++) Add(store, expected, n + 1, 15);
        AssertStore(store, expected);
        foreach (var state in expected.Values.ToArray()) {
            if ((state.value & 1) == 0) {
                state.entity.DeleteEntity();
                expected.Remove(state.entity.Id);
            } else {
                state.entity.AddTag<TestTag>();
                state.tagged = true;
            }
        }
        AssertStore(store, expected);
    }

    private static void Add(EntityStore store, Dictionary<int, Expected> expected, int value, int mask)
    {
        var entity = store.CreateEntity();
        Write(entity, mask, value);
        expected.Add(entity.Id, new Expected { entity = entity, mask = mask, value = value });
    }

    private static void Write(Entity entity, int mask, int value)
    {
        if ((mask & 1) != 0) entity.AddComponent(new Position(value, -value, value + 1));
        else entity.RemoveComponent<Position>();
        if ((mask & 2) != 0) entity.AddComponent(new Pos2 { value = new Vector2(value, -value) });
        else entity.RemoveComponent<Pos2>();
        if ((mask & 4) != 0) entity.AddComponent(new Pos3SoA { value = new Vector3(value, -value, value + 1) });
        else entity.RemoveComponent<Pos3SoA>();
        if ((mask & 8) != 0) entity.AddComponent(new Pos3AoSoA { value = new Vector3(value, -value, value + 1) });
        else entity.RemoveComponent<Pos3AoSoA>();
    }

    private static void AssertStore(EntityStore store, Dictionary<int, Expected> expected)
    {
        Assert.AreEqual(expected.Count, store.Count);
        CollectionAssert.AreEquivalent(expected.Keys, store.Entities.Select(entity => entity.Id));
        foreach (var state in expected.Values) {
            var entity = state.entity;
            int value = state.value;
            Assert.IsFalse(entity.IsNull);
            Assert.AreEqual((state.mask & 1) != 0, entity.HasComponent<Position>());
            Assert.AreEqual((state.mask & 2) != 0, entity.HasComponent<Pos2>());
            Assert.AreEqual((state.mask & 4) != 0, entity.HasComponent<Pos3SoA>());
            Assert.AreEqual((state.mask & 8) != 0, entity.HasComponent<Pos3AoSoA>());
            Assert.AreEqual(state.tagged, entity.Tags.Has<TestTag>());
            if ((state.mask & 1) != 0) Assert.AreEqual(new Position(value, -value, value + 1), entity.GetComponent<Position>());
            if ((state.mask & 2) != 0) Assert.AreEqual(new Vector2(value, -value), entity.GetComponent<Pos2>().value);
            if ((state.mask & 4) != 0) Assert.AreEqual(new Vector3(value, -value, value + 1), entity.GetSoA<Pos3SoA>().value);
            if ((state.mask & 8) != 0) Assert.AreEqual(new Vector3(value, -value, value + 1), entity.GetSoA<Pos3AoSoA>().value);
        }
        CollectionAssert.AreEquivalent(expected.Values.Where(state => (state.mask & 4) != 0).Select(state => state.entity.Id), store.Query<Pos3SoA>().Entities.Select(entity => entity.Id));
        foreach (var (chunk, entities) in store.Query<Pos3SoA>().Chunks) {
            for (int n = 0; n < chunk.Length; n++) {
                int value = expected[entities[n]].value;
                Assert.AreEqual(new Vector3(value, -value, value + 1), chunk.GetSoA(n).value);
            }
        }
        foreach (var (chunk, entities) in store.Query<Pos3AoSoA>().Chunks) {
            for (int n = 0; n < chunk.Length; n++) {
                int value = expected[entities[n]].value;
                Assert.AreEqual(new Vector3(value, -value, value + 1), chunk.GetAoSoA(n).value);
            }
        }
    }
}
}
