using Friflo.Engine.ECS;
using NUnit.Framework;

namespace Internal.ECS {

public static class Test_EntityTypeTracking
{
    [Test]
    public static void Test_EntityTypeTracking_InlineAndSparseTypes()
    {
        var store = new EntityStore();
        var entity = store.CreateEntity();
        var id = entity.Id;

        EntityTypeTracking.AddOwner(store, id, 31);
        EntityTypeTracking.AddOwner(store, id, 32);
        EntityTypeTracking.AddOwner(store, id, 300);
        var ownerTypes = EntityTypeTracking.GetOwnerTypes(store, id);

        Assert.IsTrue(ownerTypes.bitSet.Has(31));
        Assert.IsTrue(ownerTypes.bitSet.Has(32));
        Assert.IsTrue(ownerTypes.bitSet.Has(300));

        EntityTypeTracking.RemoveOwner(store, id, 31);
        EntityTypeTracking.RemoveOwner(store, id, 32);
        EntityTypeTracking.RemoveOwner(store, id, 300);
        Assert.IsFalse(EntityTypeTracking.HasOwnerTypes(store, id));

        EntityTypeTracking.AddLinked(store, id, 31);
        EntityTypeTracking.AddLinked(store, id, 32);
        EntityTypeTracking.AddLinked(store, id, 300);
        var linkedTypes = EntityTypeTracking.GetLinkedTypes(store, id);

        Assert.IsTrue(linkedTypes.bitSet.Has(31));
        Assert.IsTrue(linkedTypes.bitSet.Has(32));
        Assert.IsTrue(linkedTypes.bitSet.Has(300));

        EntityTypeTracking.RemoveLinked(store, id, 31);
        EntityTypeTracking.RemoveEntity(store.extension, id);
        Assert.IsFalse(EntityTypeTracking.HasLinkedTypes(store, id));
    }
}

}
