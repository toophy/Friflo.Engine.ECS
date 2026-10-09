using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Serialize;
using NUnit.Framework;
using Tests.ECS.Index;
using Tests.ECS.Relations;

namespace Tests.ECS.Serialize
{
public static class Test_MixedStoreRoundtrip
{
    private sealed class Expected
    {
        public int id;
        public int mask = 15;
        public int value;
        public int? score;
        public int? link;
        public bool tagged;
        public int parent;
        public readonly List<int> children = new List<int>();
        public readonly Dictionary<int, int> relations = new Dictionary<int, int>();
    }

    [TestCase(17)] [TestCase(101)] [TestCase(8191)]
    public static void MixedStoreAndRepeatedReadPreserveIndependentState(int seed)
    {
        var random = new Random(seed);
        var source = new EntityStore();
        var expected = new Dictionary<int, Expected>();
        var trace = new StringBuilder();
        for (int id = 1; id <= 257; id++) {
            var state = new Expected { id = id, value = id };
            expected.Add(id, state);
            Write(source.CreateEntity(id), state);
            if (id > 3) SetParent(source, expected, state, id % 3 + 1);
        }
        AssertRoundtrip(source, expected);
        for (int step = 0; step < 500; step++) {
            var state = expected.Values.ElementAt(random.Next(expected.Count));
            var entity = source.GetEntityById(state.id);
            int operation = random.Next(7);
            int value = random.Next(-1024, 1025);
            int target = random.Next(1, 258);
            trace.AppendLine($"{step}: op={operation} id={state.id} value={value} target={target}");
            try {
                switch (operation) {
                    case 0:
                        state.value = value;
                        state.mask = random.Next(16);
                        Write(entity, state);
                        break;
                    case 1:
                        state.tagged = !state.tagged;
                        if (state.tagged) entity.AddTag<TestTag>();
                        else entity.RemoveTag<TestTag>();
                        break;
                    case 2:
                        if ((value & 1) == 0) {
                            state.score = value % 5;
                            entity.AddComponent(new IndexedInt { value = state.score.Value });
                        } else {
                            state.score = null;
                            entity.RemoveComponent<IndexedInt>();
                        }
                        break;
                    case 3:
                        if ((value & 1) == 0) {
                            state.link = target;
                            entity.AddComponent(new LinkComponent { entity = source.GetEntityById(target), data = state.id });
                        } else {
                            state.link = null;
                            entity.RemoveComponent<LinkComponent>();
                        }
                        break;
                    case 4:
                        state.relations[target] = value;
                        entity.AddRelation(new AttackRelation { target = source.GetEntityById(target), speed = value });
                        break;
                    case 5:
                        state.relations.Remove(target);
                        entity.RemoveRelation<AttackRelation>(source.GetEntityById(target));
                        break;
                    case 6:
                        if (state.id > 3) SetParent(source, expected, state, random.Next(4));
                        break;
                }
                if (step % 50 == 49) AssertRoundtrip(source, expected);
            } catch (Exception error) {
                Assert.Fail($"seed={seed}, step={step}\n{trace}\n{error}");
            }
        }
    }

    private static void SetParent(EntityStore store, Dictionary<int, Expected> expected, Expected state, int parent)
    {
        if (state.parent == parent) return;
        var entity = store.GetEntityById(state.id);
        if (state.parent != 0) {
            store.GetEntityById(state.parent).RemoveChild(entity);
            expected[state.parent].children.Remove(state.id);
        }
        if (parent != 0) {
            store.GetEntityById(parent).AddChild(entity);
            expected[parent].children.Add(state.id);
        }
        state.parent = parent;
    }

    private static void Write(Entity entity, Expected state)
    {
        int value = state.value;
        if ((state.mask & 1) != 0) entity.AddComponent(new Position(value, -value, value + 1));
        else entity.RemoveComponent<Position>();
        if ((state.mask & 2) != 0) entity.AddComponent(new Pos2 { value = new Vector2(value, -value) });
        else entity.RemoveComponent<Pos2>();
        if ((state.mask & 4) != 0) entity.AddComponent(new Pos3SoA { value = new Vector3(value, -value, value + 1) });
        else entity.RemoveComponent<Pos3SoA>();
        if ((state.mask & 8) != 0) entity.AddComponent(new Pos3AoSoA { value = new Vector3(value, -value, value + 1) });
        else entity.RemoveComponent<Pos3AoSoA>();
    }

    private static void AssertRoundtrip(EntityStore source, Dictionary<int, Expected> expected)
    {
        var serializer = new EntitySerializer();
        using var stream = new MemoryStream();
        serializer.WriteStore(source, stream);
        var target = new EntityStore();
        for (int read = 0; read < 2; read++) {
            stream.Position = 0;
            Assert.IsNull(serializer.ReadIntoStore(target, stream).error);
            AssertStore(target, expected);
        }
    }

    private static void AssertStore(EntityStore store, Dictionary<int, Expected> expected)
    {
        Assert.AreEqual(expected.Count, store.Count);
        CollectionAssert.AreEquivalent(expected.Keys, store.Entities.Select(entity => entity.Id));
        foreach (var state in expected.Values) {
            var entity = store.GetEntityById(state.id);
            int value = state.value;
            Assert.AreEqual((state.mask & 1) != 0, entity.HasComponent<Position>());
            Assert.AreEqual((state.mask & 2) != 0, entity.HasComponent<Pos2>());
            Assert.AreEqual((state.mask & 4) != 0, entity.HasComponent<Pos3SoA>());
            Assert.AreEqual((state.mask & 8) != 0, entity.HasComponent<Pos3AoSoA>());
            if ((state.mask & 1) != 0) Assert.AreEqual(new Position(value, -value, value + 1), entity.GetComponent<Position>());
            if ((state.mask & 2) != 0) Assert.AreEqual(new Vector2(value, -value), entity.GetComponent<Pos2>().value);
            if ((state.mask & 4) != 0) Assert.AreEqual(new Vector3(value, -value, value + 1), entity.GetSoA<Pos3SoA>().value);
            if ((state.mask & 8) != 0) Assert.AreEqual(new Vector3(value, -value, value + 1), entity.GetSoA<Pos3AoSoA>().value);
            Assert.AreEqual(state.tagged, entity.Tags.Has<TestTag>());
            Assert.AreEqual(state.parent, entity.Parent.Id);
            CollectionAssert.AreEqual(state.children, entity.ChildIds.ToArray());
            Assert.AreEqual(state.score.HasValue, entity.HasComponent<IndexedInt>());
            if (state.score.HasValue) Assert.AreEqual(state.score.Value, entity.GetComponent<IndexedInt>().value);
            Assert.AreEqual(state.link.HasValue, entity.HasComponent<LinkComponent>());
            if (state.link.HasValue) {
                Assert.AreEqual(store.GetEntityById(state.link.Value), entity.GetComponent<LinkComponent>().entity);
                Assert.AreEqual(state.id, entity.GetComponent<LinkComponent>().data);
            }
            var relations = entity.GetRelations<AttackRelation>();
            Assert.AreEqual(state.relations.Count, relations.Length);
            var keys = new HashSet<int>();
            for (int n = 0; n < relations.Length; n++) {
                var relation = relations[n];
                Assert.IsTrue(keys.Add(relation.target.Id));
                Assert.AreEqual(store.GetEntityById(relation.target.Id), relation.target);
                Assert.AreEqual(state.relations[relation.target.Id], relation.speed);
            }
            CollectionAssert.AreEquivalent(state.relations.Keys, keys);
            CollectionAssert.AreEquivalent(expected.Values.Where(source => source.link == state.id).Select(source => source.id), entity.GetIncomingLinks<LinkComponent>().Entities.Select(source => source.Id));
            CollectionAssert.AreEquivalent(expected.Values.Where(source => source.relations.ContainsKey(state.id)).Select(source => source.id), entity.GetIncomingLinks<AttackRelation>().Entities.Select(source => source.Id));
        }
        var index = store.ComponentIndex<IndexedInt, int>();
        CollectionAssert.AreEquivalent(expected.Values.Where(state => state.score.HasValue).Select(state => state.score.Value).Distinct(), index.Values);
        foreach (int key in index.Values) {
            CollectionAssert.AreEquivalent(expected.Values.Where(state => state.score == key).Select(state => state.id), index[key].Select(entity => entity.Id));
        }
    }
}
}
