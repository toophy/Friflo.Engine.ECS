using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using NUnit.Framework;

namespace Tests.ECS.Buffer
{
public static class Test_BufferResourceLifetime
{
    [TestCase(17)] [TestCase(101)] [TestCase(8191)]
    public static void EightSyncedBuffersReserveUniqueIdsAndPreserveValues(int seed)
    {
        const int bufferCount = 8;
        const int entitiesPerBuffer = 256;
        var store = new EntityStore();
        var buffers = new CommandBuffer[bufferCount];
        var ids = new int[bufferCount * entitiesPerBuffer];
        for (int n = 0; n < bufferCount; n++) buffers[n] = store.GetCommandBuffer();
        var random = new Random(seed);
        var order = Enumerable.Range(0, bufferCount).OrderBy(_ => random.Next()).ToArray();
        Parallel.For(0, bufferCount, worker => {
            int bufferIndex = order[worker];
            var synced = buffers[bufferIndex].Synced;
            for (int n = 0; n < entitiesPerBuffer; n++) {
                int value = bufferIndex * entitiesPerBuffer + n;
                int id = synced.CreateEntity();
                ids[value] = id;
                synced.AddComponent(id, new Position(value, seed, -value));
            }
        });
        Assert.AreEqual(ids.Length, ids.Distinct().Count(), $"seed={seed}");
        Assert.AreEqual(0, store.Count);
        foreach (int index in order.Reverse()) buffers[index].Playback();
        Assert.AreEqual(ids.Length, store.Count);
        for (int n = 0; n < ids.Length; n++) {
            Assert.AreEqual(new Position(n, seed, -n), store.GetEntityById(ids[n]).GetComponent<Position>(), $"seed={seed}, value={n}");
        }
        var empty = store.GetCommandBuffer();
        empty.Playback();
        Assert.AreEqual(ids.Length, store.Count);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)]
    public static void ReleasedComponentValuesAreCollectedWhileOwnersRemainAlive(int operation)
    {
        var store = new EntityStore();
        object owner;
        var weak = UseComponentValue(store, operation, out owner);
        Collect();
        Assert.IsFalse(weak.IsAlive, $"operation={operation}");
        GC.KeepAlive(owner);
        GC.KeepAlive(store);
    }

    [Test]
    public static void ClearedScriptIsCollectedWhileBufferRemainsAlive()
    {
        var store = new EntityStore();
        var buffer = store.GetCommandBuffer();
        var weak = QueueScript(buffer);
        Collect();
        Assert.IsFalse(weak.IsAlive);
        GC.KeepAlive(buffer);
        GC.KeepAlive(store);
    }

    [TestCase(false, false)] [TestCase(true, false)]
    [TestCase(false, true)] [TestCase(true, true)]
    public static void DeletedTailAndSwappedValuesAreCollected(bool swap, bool migrate)
    {
        EntityStore store = null;
        WeakReference weak = null;
        Exception error = null;
        var worker = new Thread(() => {
            try {
                store = new EntityStore();
                weak = CreateAndDelete(store, swap, migrate);
            } catch (Exception exception) {
                error = exception;
            }
        });
        worker.Start();
        worker.Join();
        Assert.IsNull(error);
        Collect();
        Assert.IsFalse(weak.IsAlive);
        if (swap) CollectionAssert.AreEqual(new[] { 42 }, store.GetEntityById(2).GetComponent<CopyComponent>().list);
        Assert.AreEqual(swap ? 1 : 0, store.Count);
        GC.KeepAlive(store);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDelete(EntityStore store, bool swap, bool migrate)
    {
        var list = new List<int> { 1 };
        var weak = new WeakReference(list);
        var entity = store.CreateEntity(1);
        entity.AddComponent(new CopyComponent { list = list });
        if (swap) store.CreateEntity(2).AddComponent(new CopyComponent { list = new List<int> { 42 } });
        if (migrate) entity.AddTag<TestTag>();
        entity.DeleteEntity();
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference UseComponentValue(EntityStore store, int operation, out object owner)
    {
        var list = new List<int> { operation };
        var weak = new WeakReference(list);
        var component = new CopyComponent { list = list };
        var entity = store.CreateEntity();
        if (operation < 3) {
            var buffer = store.GetCommandBuffer();
            owner = buffer;
            buffer.AddComponent(entity.Id, component);
            if (operation == 0) buffer.Clear();
            if (operation == 1) buffer.ReturnBuffer();
            if (operation == 2) {
                buffer.Playback();
                entity.DeleteEntity();
            }
        } else if (operation < 6) {
            var batch = new EntityBatch();
            owner = batch;
            batch.Add(component);
            if (operation == 3) batch.Clear();
            if (operation == 4) batch.Remove<CopyComponent>();
            if (operation == 5) {
                batch.ApplyTo(entity);
                batch.Clear();
                entity.DeleteEntity();
            }
        } else {
            var batch = store.Batch(autoReturn: false);
            owner = batch;
            batch.Add(component);
            if (operation == 6) batch.Clear();
            if (operation == 7) batch.Return();
            if (operation == 8) {
                batch.CreateEntity().DeleteEntity();
                batch.Return();
            }
        }
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QueueScript(CommandBuffer buffer)
    {
        var script = new TestScript1();
        var weak = new WeakReference(script);
        buffer.AddScript(1, script);
        buffer.Clear();
        return weak;
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
}
