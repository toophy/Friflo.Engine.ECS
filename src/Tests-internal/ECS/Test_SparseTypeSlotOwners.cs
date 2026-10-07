using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.ECS;

namespace Internal.ECS {

public static class Test_SparseTypeSlotOwners
{
    [Test]
    public static void EntityBatch_ClearRemoveAndPoolReturn_ClearManagedValues()
    {
        var structIndex = EntityStore.GetEntitySchema().GetComponentType<CopyComponent>().StructIndex;
        var applicationBatch = new EntityBatch();
        Assert.IsNull(applicationBatch.batchComponents);
        var firstValue = new List<int> { 1 };
        applicationBatch.Add(new CopyComponent { list = firstValue });
        var applicationSlot = (BatchComponent<CopyComponent>)applicationBatch.batchComponents[structIndex];
        Assert.AreSame(firstValue, applicationSlot.value.list);

        applicationBatch.Clear();
        Assert.IsNull(applicationSlot.value.list);

        applicationBatch.Add(new CopyComponent { list = new List<int> { 2 } });
        applicationBatch.Remove<CopyComponent>();
        Assert.IsNull(applicationSlot.value.list);

        var store = new EntityStore();
        var entity = store.CreateEntity();
        var pooledBatch = entity.Batch();
        pooledBatch.Add(new CopyComponent { list = new List<int> { 3 } });
        var pooledSlot = (BatchComponent<CopyComponent>)pooledBatch.batchComponents[structIndex];
        pooledBatch.Apply();

        Assert.IsNull(pooledSlot.value.list);
        var reusedBatch = entity.Batch();
        Assert.AreSame(pooledBatch, reusedBatch);
        reusedBatch.Apply();
    }

    [Test]
    public static void CreateEntityBatch_ClearAndReturn_ClearManagedValuesAndReuseSlot()
    {
        var structIndex = EntityStore.GetEntitySchema().GetComponentType<CopyComponent>().StructIndex;
        var store = new EntityStore();
        var batch = store.Batch(autoReturn: false);
        Assert.IsNull(batch.batchComponents);
        var firstValue = new List<int> { 1 };
        batch.Add(new CopyComponent { list = firstValue });
        var slot = (BatchComponent<CopyComponent>)batch.batchComponents[structIndex];
        Assert.AreSame(firstValue, slot.value.list);

        batch.Clear();
        Assert.IsNull(slot.value.list);

        batch.Add(new CopyComponent { list = new List<int> { 2 } });
        batch.Return();
        Assert.IsNull(slot.value.list);

        var reusedBatch = store.Batch(autoReturn: false);
        Assert.AreSame(batch, reusedBatch);
        reusedBatch.Add(new CopyComponent { list = new List<int> { 3 } });
        Assert.AreSame(slot, reusedBatch.batchComponents[structIndex]);
        reusedBatch.Return();
        Assert.IsNull(slot.value.list);
    }

    [Test]
    public static void CommandBuffer_ClearPlaybackAndPoolReturn_ClearManagedValues()
    {
        var store = new EntityStore();
        var entity = store.CreateEntity();
        entity.AddComponent(new CopyComponent { list = new List<int> { 1 } });
        store.OnComponentAdded += _ => { };

        var buffer = store.GetCommandBuffer();
        buffer.AddComponent(entity.Id, new CopyComponent { list = new List<int> { 2 } });
        var commands = (ComponentCommands<CopyComponent>)buffer.ComponentCommands[0];
        buffer.Playback();

        Assert.AreEqual(0, commands.commandCount);
        Assert.IsNull(commands.componentCommands[0].component.list);
        Assert.IsNull(commands.componentCommands[0].oldComponent.list);
        Assert.IsNull(((CopyComponent)commands.GetStashDebug()).list);

        var reusedBuffer = store.GetCommandBuffer();
        Assert.AreSame(buffer, reusedBuffer);
        reusedBuffer.AddComponent(entity.Id, new CopyComponent { list = new List<int> { 3 } });
        commands = (ComponentCommands<CopyComponent>)reusedBuffer.ComponentCommands[0];
        reusedBuffer.Clear();
        Assert.AreEqual(0, commands.commandCount);
        Assert.IsNull(commands.componentCommands[0].component.list);

        reusedBuffer.CreateEntity();
        reusedBuffer.ReturnBuffer();
        var returnedBuffer = store.GetCommandBuffer();
        Assert.AreSame(reusedBuffer, returnedBuffer);
        Assert.AreEqual(0, returnedBuffer.EntityCommandsCount);
        returnedBuffer.Playback();
        Assert.AreEqual(1, store.Count);
    }

    [Test]
    public static void CommandBuffer_Clear_ReleasesScriptReference()
    {
        var store = new EntityStore();
        var buffer = store.GetCommandBuffer();
        var weakReference = QueueScriptAndClear(buffer);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsFalse(weakReference.IsAlive);
        buffer.ReturnBuffer();
    }

    [Test]
    public static void MultipleSyncedCommandBuffers_AllocateUniqueEntityIds()
    {
        const int bufferCount = 4;
        const int entitiesPerBuffer = 128;
        var store = new EntityStore();
        var buffers = new CommandBuffer[bufferCount];
        var ids = new int[bufferCount * entitiesPerBuffer];
        for (var index = 0; index < bufferCount; index++) {
            buffers[index] = store.GetCommandBuffer();
        }

        Parallel.For(0, bufferCount, bufferIndex => {
            var synced = buffers[bufferIndex].Synced;
            var offset = bufferIndex * entitiesPerBuffer;
            for (var entityIndex = 0; entityIndex < entitiesPerBuffer; entityIndex++) {
                ids[offset + entityIndex] = synced.CreateEntity();
            }
        });

        Assert.AreEqual(ids.Length, ids.Distinct().Count());
        Assert.AreEqual(ids.Length, buffers.Sum(static buffer => buffer.EntityCommandsCount));
        foreach (var buffer in buffers) {
            buffer.Playback();
        }
        Assert.AreEqual(ids.Length, store.Count);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QueueScriptAndClear(CommandBuffer buffer)
    {
        var script = new TestScript1();
        var weakReference = new WeakReference(script);
        buffer.AddScript(1, script);
        buffer.Clear();
        return weakReference;
    }
}

}
