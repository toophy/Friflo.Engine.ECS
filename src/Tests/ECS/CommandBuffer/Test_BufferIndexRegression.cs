using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.ECS.Index;
using Tests.SoA;

namespace Tests.ECS.Buffer
{
public static class Test_BufferIndexRegression
{
    [Test]
    public static void LinkIndexValuesSupportNonGenericEnumeration()
    {
        var store = new EntityStore();
        var target = store.CreateEntity();
        store.CreateEntity(new LinkComponent { entity = target });
        IEnumerable values = store.ComponentIndex<LinkComponent, Entity>().Values;
        var enumerator = values.GetEnumerator();
        try {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(target, (Entity)enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());
        } finally {
            ((IDisposable)enumerator).Dispose();
        }
    }

    [TestCase(false)] [TestCase(true)]
    public static void RepeatedAddsRetainOnlyTheFinalIndexedValue(bool existing)
    {
        AssertReplacement<IndexedInt, int>(new IndexedInt { value = 9 }, new IndexedInt { value = 3 }, new IndexedInt { value = 7 }, existing);
    }

    [TestCase(false, false)] [TestCase(true, false)]
    [TestCase(false, true)] [TestCase(true, true)]
    public static void RepeatedNullableAddsRetainOnlyTheFinalIndexedValue(bool existing, bool finalNull)
    {
        AssertReplacement<IndexedName, string>(new IndexedName { name = "initial" },
            new IndexedName { name = finalNull ? "superseded" : null }, new IndexedName { name = finalNull ? null : "final" }, existing);
    }

    [TestCase(false)] [TestCase(true)]
    public static void RepeatedRangeAddsRetainOnlyTheFinalIndexedValue(bool existing)
    {
        AssertReplacement<IndexedStringRange, string>(new IndexedStringRange { value = "initial" },
            new IndexedStringRange { value = "first" }, new IndexedStringRange { value = "last" }, existing);
    }

    [TestCase(0, false)] [TestCase(0, true)]
    [TestCase(1, false)] [TestCase(1, true)]
    [TestCase(2, false)] [TestCase(2, true)]
    public static void RepeatedLayoutAddsRetainOnlyTheFinalIndexedValue(int layout, bool existing)
    {
        var initial = new Vector2(9, 9);
        var first = new Vector2(3, 3);
        var last = new Vector2(7, 7);
        switch (layout) {
            case 0:
                AssertReplacement<Test_LayoutRegression.IndexedAlignedVector, Vector2>(new Test_LayoutRegression.IndexedAlignedVector { value = initial },
                    new Test_LayoutRegression.IndexedAlignedVector { value = first }, new Test_LayoutRegression.IndexedAlignedVector { value = last }, existing);
                break;
            case 1:
                AssertReplacement<Test_LayoutRegression.IndexedSoAVector, Vector2>(new Test_LayoutRegression.IndexedSoAVector { value = initial },
                    new Test_LayoutRegression.IndexedSoAVector { value = first }, new Test_LayoutRegression.IndexedSoAVector { value = last }, existing);
                break;
            case 2:
                AssertReplacement<Test_LayoutRegression.IndexedAoSoAVector, Vector2>(new Test_LayoutRegression.IndexedAoSoAVector { value = initial },
                    new Test_LayoutRegression.IndexedAoSoAVector { value = first }, new Test_LayoutRegression.IndexedAoSoAVector { value = last }, existing);
                break;
        }
    }

    [TestCase(false)] [TestCase(true)]
    public static void RepeatedLinksDoNotRetainSupersededTargets(bool existing)
    {
        var store = new EntityStore();
        var source = store.CreateEntity();
        var initial = store.CreateEntity();
        var first = store.CreateEntity();
        var last = store.CreateEntity();
        if (existing) source.AddComponent(new LinkComponent { entity = initial, data = 9 });
        var buffer = store.GetCommandBuffer();
        buffer.AddComponent(source.Id, new LinkComponent { entity = first, data = 3 });
        buffer.AddComponent(source.Id, new LinkComponent { entity = last, data = 7 });
        buffer.Playback();
        var index = store.ComponentIndex<LinkComponent, Entity>();
        CollectionAssert.AreEqual(new[] { last }, index.Values);
        Assert.AreEqual(0, initial.GetIncomingLinks<LinkComponent>().Count);
        Assert.AreEqual(0, first.GetIncomingLinks<LinkComponent>().Count);
        Assert.AreEqual(1, last.GetIncomingLinks<LinkComponent>().Count);
        first.DeleteEntity();
        Assert.AreEqual(last, source.GetComponent<LinkComponent>().entity);
        Assert.AreEqual(7, source.GetComponent<LinkComponent>().data);
        last.DeleteEntity();
        Assert.IsFalse(source.HasComponent<LinkComponent>());
        Assert.AreEqual(0, index.Values.Count);
        Assert.AreEqual(2, store.Count);
    }

    [TestCase(false)] [TestCase(true)]
    public static void RepeatedAddRemoveCommandsAndBufferReuseKeepIndexesConsistent(bool finalRemove)
    {
        var store = new EntityStore();
        var entity = store.CreateEntity(new IndexedInt { value = 9 });
        var index = store.ComponentIndex<IndexedInt, int>();
        var buffer = store.GetCommandBuffer();
        buffer.ReuseBuffer = true;
        for (int round = 0; round < 2; round++) {
            buffer.AddComponent(entity.Id, new IndexedInt { value = 3 });
            buffer.RemoveComponent<IndexedInt>(entity.Id);
            buffer.AddComponent(entity.Id, new IndexedInt { value = 7 });
            buffer.AddComponent(entity.Id, new IndexedInt { value = 7 });
            if (finalRemove) buffer.RemoveComponent<IndexedInt>(entity.Id);
            buffer.Playback();
            Assert.AreEqual(!finalRemove, entity.HasComponent<IndexedInt>());
            Assert.AreEqual(0, index[3].Count);
            Assert.AreEqual(0, index[9].Count);
            Assert.AreEqual(finalRemove ? 0 : 1, index[7].Count);
            CollectionAssert.AreEqual(finalRemove ? Array.Empty<int>() : new[] { 7 }, index.Values);
        }
        buffer.ReturnBuffer();
        entity.DeleteEntity();
        Assert.AreEqual(0, index.Values.Count);
    }

    [TestCase(20261008, true)] [TestCase(24301, true)] [TestCase(911, true)]
    [TestCase(20261008, false)] [TestCase(24301, false)] [TestCase(911, false)]
    public static void BufferedIndexesMatchIndependentModel(int seed, bool duplicateWrite)
    {
        var store = new EntityStore();
        var entities = Enumerable.Range(1, 8).ToDictionary(id => id, id => store.CreateEntity(id));
        var integers = new Dictionary<int, int>();
        var names = new Dictionary<int, string>();
        var intIndex = store.ComponentIndex<IndexedInt, int>();
        var nameIndex = store.ComponentIndex<IndexedName, string>();
        var range = store.Query().ValueInRange<IndexedInt, int>(-2, 2);
        var hasNull = store.Query().HasValue<IndexedName, string>(null);
        var random = new Random(seed);
        for (int step = 0; step < 256; step++) {
            int id = random.Next(1, 9);
            int operation = random.Next(6);
            int value = random.Next(-4, 5);
            string name = random.Next(3) == 0 ? null : "k" + value % 3;
            try {
                switch (operation) {
                    case 0:
                        entities[id].AddComponent(new IndexedInt { value = value });
                        integers[id] = value;
                        break;
                    case 1:
                        entities[id].RemoveComponent<IndexedInt>();
                        integers.Remove(id);
                        break;
                    case 2:
                        entities[id].AddComponent(new IndexedName { name = name });
                        names[id] = name;
                        break;
                    case 3:
                        entities[id].RemoveComponent<IndexedName>();
                        names.Remove(id);
                        break;
                    case 4:
                        var stale = entities[id];
                        stale.DeleteEntity();
                        entities[id] = store.CreateEntity(id);
                        integers.Remove(id);
                        names.Remove(id);
                        Assert.IsTrue(stale.IsNull);
                        break;
                    case 5:
                        var buffer = store.GetCommandBuffer();
                        if (duplicateWrite) buffer.AddComponent(id, new IndexedInt { value = value - 1 });
                        buffer.AddComponent(id, new IndexedInt { value = value });
                        buffer.AddComponent(id, new IndexedName { name = name });
                        buffer.AddComponent(id, new Position(step, 0, 0));
                        buffer.Playback();
                        integers[id] = value;
                        names[id] = name;
                        Assert.AreEqual(step, entities[id].GetComponent<Position>().x);
                        break;
                }
                Assert.AreEqual(8, store.Count);
                foreach (var entity in entities.Values) {
                    Assert.AreEqual(integers.ContainsKey(entity.Id), entity.HasComponent<IndexedInt>());
                    Assert.AreEqual(names.ContainsKey(entity.Id), entity.HasComponent<IndexedName>());
                    if (integers.TryGetValue(entity.Id, out int number)) Assert.AreEqual(number, entity.GetComponent<IndexedInt>().value);
                    if (names.TryGetValue(entity.Id, out string text)) Assert.AreEqual(text, entity.GetComponent<IndexedName>().name);
                }
                for (int key = -5; key <= 4; key++) {
                    CollectionAssert.AreEquivalent(integers.Where(pair => pair.Value == key).Select(pair => pair.Key), intIndex[key].Select(entity => entity.Id));
                }
                foreach (string key in new[] { null, "k-2", "k-1", "k0", "k1", "k2" }) {
                    CollectionAssert.AreEquivalent(names.Where(pair => pair.Value == key).Select(pair => pair.Key), nameIndex[key].Select(entity => entity.Id));
                }
                AssertQuery(range, integers.Where(pair => pair.Value >= -2 && pair.Value <= 2).Select(pair => pair.Key));
                AssertQuery(hasNull, names.Where(pair => pair.Value == null).Select(pair => pair.Key));
                CollectionAssert.AreEquivalent(integers.Values.Distinct(), intIndex.Values);
                CollectionAssert.AreEquivalent(names.Values.Where(text => text != null).Distinct(), nameIndex.Values);
            } catch (Exception error) {
                Assert.Fail($"seed={seed}, step={step}, id={id}, operation={operation}, value={value}, duplicateWrite={duplicateWrite}\n{error}");
            }
        }
    }

    private static void AssertQuery(ArchetypeQuery query, IEnumerable<int> expected)
    {
        var actual = query.Entities.Select(entity => entity.Id).ToArray();
        CollectionAssert.AreEquivalent(expected, actual);
        Assert.AreEqual(actual.Length, query.Count);
        Assert.AreEqual(actual.Length, actual.Distinct().Count());
    }

    private static void AssertReplacement<T, TValue>(T initial, T first, T last, bool existing) where T : struct, IIndexedComponent<TValue>
    {
        var store = new EntityStore();
        var entity = store.CreateEntity(1);
        if (existing) entity.AddComponent(initial);
        var initialValue = initial.GetIndexedValue();
        var firstValue = first.GetIndexedValue();
        var lastValue = last.GetIndexedValue();
        var index = store.ComponentIndex<T, TValue>();
        var oldQuery = store.Query().HasValue<T, TValue>(firstValue);
        Assert.AreEqual(0, oldQuery.Count);
        var buffer = store.GetCommandBuffer();
        buffer.AddComponent(entity.Id, first);
        buffer.AddComponent(entity.Id, last);
        buffer.Playback();
        Assert.IsTrue(entity.TryGetComponent<T>(out var component));
        Assert.AreEqual(lastValue, component.GetIndexedValue());
        CollectionAssert.AreEqual(new[] { 1 }, index[lastValue].Select(item => item.Id));
        Assert.AreEqual(0, index[firstValue].Count);
        Assert.AreEqual(0, index[initialValue].Count);
        CollectionAssert.AreEqual(lastValue == null ? Array.Empty<TValue>() : new[] { lastValue }, index.Values);
        Assert.AreEqual(0, oldQuery.Count);
        Assert.AreEqual(0, oldQuery.Entities.Count());
        Assert.AreEqual(0, store.Query().HasValue<T, TValue>(firstValue).Count);
        entity.DeleteEntity();
        Assert.AreEqual(0, store.Count);
        Assert.AreEqual(0, index[firstValue].Count);
        Assert.AreEqual(0, index[lastValue].Count);
        Assert.AreEqual(0, index.Values.Count);
        Assert.AreEqual(0, oldQuery.Count);
    }
}
}
