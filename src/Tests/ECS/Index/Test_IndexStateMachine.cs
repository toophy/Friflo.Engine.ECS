using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.ECS.Relations;

namespace Tests.ECS.Index
{
public static class Test_IndexStateMachine
{
    private sealed class Expected
    {
        public Entity entity;
        public int? score;
        public int? link;
        public int data;
        public readonly Dictionary<int, int> relations = new Dictionary<int, int>();
    }

    [TestCase(17)] [TestCase(101)] [TestCase(8191)]
    public static void IndexesAndLinksMatchIndependentState(int seed)
    {
        var random = new Random(seed);
        var store = new EntityStore();
        var expected = new Dictionary<int, Expected>();
        var trace = new StringBuilder();
        for (int id = 1; id <= 33; id++) expected.Add(id, new Expected { entity = store.CreateEntity(id) });
        for (int step = 0; step < 500; step++) {
            var source = expected.Values.ElementAt(random.Next(expected.Count));
            var target = expected.Values.ElementAt(random.Next(expected.Count));
            int operation = random.Next(8);
            int value = random.Next(-2, 3);
            trace.AppendLine($"{step}: op={operation} source={source.entity.Id} target={target.entity.Id} value={value}");
            try {
                switch (operation) {
                    case 0:
                        source.entity.AddComponent(new IndexedInt { value = value });
                        source.score = value;
                        break;
                    case 1:
                        source.entity.RemoveComponent<IndexedInt>();
                        source.score = null;
                        break;
                    case 2:
                        source.entity.AddComponent(new LinkComponent { entity = target.entity, data = step });
                        source.link = target.entity.Id;
                        source.data = step;
                        break;
                    case 3:
                        source.entity.RemoveComponent<LinkComponent>();
                        source.link = null;
                        break;
                    case 4:
                        source.entity.AddRelation(new AttackRelation { target = target.entity, speed = step });
                        source.relations[target.entity.Id] = step;
                        break;
                    case 5:
                        source.entity.RemoveRelation<AttackRelation>(target.entity);
                        source.relations.Remove(target.entity.Id);
                        break;
                    case 6:
                        int deletedId = target.entity.Id;
                        var stale = target.entity;
                        stale.DeleteEntity();
                        expected.Remove(deletedId);
                        foreach (var state in expected.Values) {
                            if (state.link == deletedId) state.link = null;
                            state.relations.Remove(deletedId);
                        }
                        expected.Add(deletedId, new Expected { entity = store.CreateEntity(deletedId) });
                        Assert.IsTrue(stale.IsNull);
                        break;
                    case 7:
                        if (expected.Count < 513) {
                            var entity = store.CreateEntity();
                            expected.Add(entity.Id, new Expected { entity = entity });
                        }
                        break;
                }
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
        var index = store.ComponentIndex<IndexedInt, int>();
        CollectionAssert.AreEquivalent(expected.Values.Where(state => state.score.HasValue).Select(state => state.score.Value).Distinct(), index.Values);
        for (int key = -2; key <= 2; key++) {
            CollectionAssert.AreEquivalent(expected.Values.Where(state => state.score == key).Select(state => state.entity.Id), index[key].Select(entity => entity.Id));
        }
        var links = store.ComponentIndex<LinkComponent, Entity>();
        CollectionAssert.AreEquivalent(expected.Values.Where(state => state.link.HasValue).Select(state => state.link.Value).Distinct(), links.Values.Select(entity => entity.Id));
        foreach (var state in expected.Values) {
            Assert.AreEqual(state.score.HasValue, state.entity.HasComponent<IndexedInt>());
            if (state.score.HasValue) Assert.AreEqual(state.score.Value, state.entity.GetComponent<IndexedInt>().value);
            Assert.AreEqual(state.link.HasValue, state.entity.HasComponent<LinkComponent>());
            if (state.link.HasValue) {
                var link = state.entity.GetComponent<LinkComponent>();
                Assert.AreEqual(expected[state.link.Value].entity, link.entity);
                Assert.AreEqual(state.data, link.data);
            }
            CollectionAssert.AreEquivalent(expected.Values.Where(source => source.link == state.entity.Id).Select(source => source.entity.Id), state.entity.GetIncomingLinks<LinkComponent>().Entities.Select(entity => entity.Id));
            CollectionAssert.AreEquivalent(expected.Values.Where(source => source.relations.ContainsKey(state.entity.Id)).Select(source => source.entity.Id), state.entity.GetIncomingLinks<AttackRelation>().Entities.Select(entity => entity.Id));
            var relations = state.entity.GetRelations<AttackRelation>();
            Assert.AreEqual(state.relations.Count, relations.Length);
            var keys = new HashSet<int>();
            for (int n = 0; n < relations.Length; n++) {
                var relation = relations[n];
                Assert.IsTrue(keys.Add(relation.target.Id));
                Assert.AreEqual(expected[relation.target.Id].entity, relation.target);
                Assert.AreEqual(state.relations[relation.target.Id], relation.speed);
            }
            CollectionAssert.AreEquivalent(state.relations.Keys, keys);
        }
    }
}
}
