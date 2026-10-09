using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Friflo.Engine.ECS;
using NUnit.Framework;

namespace Tests.ECS.Buffer
{
public static class Test_BufferStateMachine
{
    private sealed class Expected
    {
        public Entity entity;
        public int? position;
        public int? vector;
        public bool tagged;
    }

    [TestCase(17)] [TestCase(101)] [TestCase(8191)]
    public static void InterleavedCommandsMatchIndependentState(int seed)
    {
        var random = new Random(seed);
        var store = new EntityStore();
        var expected = new Dictionary<int, Expected>();
        var trace = new StringBuilder();
        for (int n = 0; n < 17; n++) {
            var entity = store.CreateEntity();
            expected.Add(entity.Id, new Expected { entity = entity });
        }
        for (int step = 0; step < 500; step++) {
            var state = expected.Values.ElementAt(random.Next(expected.Count));
            int operation = random.Next(9);
            int value = random.Next(-1024, 1025);
            trace.AppendLine($"{step}: op={operation} id={state.entity.Id} value={value}");
            try {
                var buffer = store.GetCommandBuffer();
                switch (operation) {
                    case 0:
                        buffer.AddComponent(state.entity.Id, new Position(value, 0, 0));
                        state.entity.AddComponent(new Position(-value, 0, 0));
                        buffer.AddComponent(state.entity.Id, new Position(value + 1, 0, 0));
                        state.position = value + 1;
                        break;
                    case 1:
                        buffer.RemoveComponent<Position>(state.entity.Id);
                        state.entity.AddComponent(new Position(value, 0, 0));
                        state.position = null;
                        break;
                    case 2:
                        buffer.AddComponent(state.entity.Id, new Pos3SoA { value = new Vector3(value, -value, value + 1) });
                        buffer.RemoveComponent<Pos3SoA>(state.entity.Id);
                        buffer.AddComponent(state.entity.Id, new Pos3SoA { value = new Vector3(value + 2, -value - 2, value + 3) });
                        state.vector = value + 2;
                        break;
                    case 3:
                        buffer.AddTag<TestTag>(state.entity.Id);
                        state.entity.RemoveTag<TestTag>();
                        buffer.RemoveTag<TestTag>(state.entity.Id);
                        buffer.AddTag<TestTag>(state.entity.Id);
                        state.tagged = true;
                        break;
                    case 4:
                        buffer.AddComponent(state.entity.Id, new Position(value, 0, 0));
                        buffer.RemoveComponent<Pos3SoA>(state.entity.Id);
                        buffer.RemoveTag<TestTag>(state.entity.Id);
                        buffer.CreateEntity();
                        buffer.Clear();
                        break;
                    case 5:
                        buffer.AddComponent(state.entity.Id, new Position(value, 0, 0));
                        buffer.AddTag<TestTag>(state.entity.Id);
                        buffer.ReturnBuffer();
                        buffer = store.GetCommandBuffer();
                        Assert.AreEqual(0, buffer.ComponentCommandsCount);
                        Assert.AreEqual(0, buffer.TagCommandsCount);
                        break;
                    case 6:
                        if (expected.Count < 513) {
                            int id = buffer.CreateEntity();
                            buffer.AddComponent(id, new Position(value, 0, 0));
                            buffer.AddComponent(id, new Pos3SoA { value = new Vector3(value, -value, value + 1) });
                            buffer.Playback();
                            expected.Add(id, new Expected { entity = store.GetEntityById(id), position = value, vector = value });
                            AssertStore(store, expected);
                            continue;
                        }
                        break;
                    case 7:
                        if (expected.Count > 9) {
                            buffer.DeleteEntity(state.entity.Id);
                            expected.Remove(state.entity.Id);
                        }
                        break;
                    case 8:
                        buffer.ReuseBuffer = true;
                        buffer.RemoveTag<TestTag>(state.entity.Id);
                        buffer.Playback();
                        state.tagged = false;
                        AssertStore(store, expected);
                        buffer.RemoveComponent<Pos3SoA>(state.entity.Id);
                        state.vector = null;
                        buffer.ReuseBuffer = false;
                        break;
                }
                buffer.Playback();
                AssertStore(store, expected);
            } catch (Exception error) {
                Assert.Fail($"seed={seed}, step={step}\n{trace}\n{error}");
            }
        }
    }

    private static void AssertStore(EntityStore store, Dictionary<int, Expected> expected)
    {
        Assert.AreEqual(expected.Count, store.Count);
        CollectionAssert.AreEquivalent(expected.Keys, store.Entities.Select(entity => entity.Id));
        foreach (var state in expected.Values) {
            Assert.AreEqual(state.position.HasValue, state.entity.HasComponent<Position>());
            Assert.AreEqual(state.vector.HasValue, state.entity.HasComponent<Pos3SoA>());
            Assert.AreEqual(state.tagged, state.entity.Tags.Has<TestTag>());
            if (state.position.HasValue) Assert.AreEqual(new Position(state.position.Value, 0, 0), state.entity.GetComponent<Position>());
            if (state.vector.HasValue) {
                int value = state.vector.Value;
                Assert.AreEqual(new Vector3(value, -value, value + 1), state.entity.GetSoA<Pos3SoA>().value);
            }
        }
    }
}
}
