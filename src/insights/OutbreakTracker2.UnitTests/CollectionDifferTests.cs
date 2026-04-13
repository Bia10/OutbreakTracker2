using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.UnitTests;

public sealed class CollectionDifferTests
{
    [Test]
    public async Task Diff_ReturnsAddedRemovedAndChangedEntitiesById()
    {
        FakeEntity unchanged = new(Ulid.NewUlid(), 1);
        FakeEntity changedBefore = new(Ulid.NewUlid(), 10);
        FakeEntity changedAfter = changedBefore with { Value = 11 };
        FakeEntity removed = new(Ulid.NewUlid(), 20);
        FakeEntity added = new(Ulid.NewUlid(), 30);

        CollectionDiff<FakeEntity> diff = CollectionDiffer.Diff(
            [unchanged, changedBefore, removed],
            [unchanged, changedAfter, added]
        );

        await Assert.That(diff.Added.Count).IsEqualTo(1);
        await Assert.That(diff.Added[0].Id).IsEqualTo(added.Id);
        await Assert.That(diff.Removed.Count).IsEqualTo(1);
        await Assert.That(diff.Removed[0].Id).IsEqualTo(removed.Id);
        await Assert.That(diff.Changed.Count).IsEqualTo(1);
        await Assert.That(diff.Changed[0].Previous.Id).IsEqualTo(changedBefore.Id);
        await Assert.That(diff.Changed[0].Current.Value).IsEqualTo(11);
    }

    [Test]
    public async Task Diff_ReturnsEmptyCollections_WhenSnapshotsMatch()
    {
        FakeEntity first = new(Ulid.NewUlid(), 1);
        FakeEntity second = new(Ulid.NewUlid(), 2);

        CollectionDiff<FakeEntity> diff = CollectionDiffer.Diff([first, second], [first, second]);

        await Assert.That(diff.Added.Count).IsEqualTo(0);
        await Assert.That(diff.Removed.Count).IsEqualTo(0);
        await Assert.That(diff.Changed.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Diff_DoesNotTreatInventorySnapshotsWithSameContentsAsChanged()
    {
        Ulid playerId = Ulid.NewUlid();
        DecodedInGamePlayer previous = new()
        {
            Id = playerId,
            IsEnabled = true,
            Name = "Kevin",
            Inventory = new InventorySnapshot(1, 2, 3, 4),
            SpecialInventory = new InventorySnapshot(5, 6, 7, 8),
            DeadInventory = InventorySnapshot.Empty,
            SpecialDeadInventory = InventorySnapshot.Empty,
        };
        DecodedInGamePlayer current = previous with
        {
            Inventory = new InventorySnapshot(1, 2, 3, 4),
            SpecialInventory = new InventorySnapshot(5, 6, 7, 8),
        };

        CollectionDiff<DecodedInGamePlayer> diff = CollectionDiffer.Diff([previous], [current]);

        await Assert.That(diff.Added.Count).IsEqualTo(0);
        await Assert.That(diff.Removed.Count).IsEqualTo(0);
        await Assert.That(diff.Changed.Count).IsEqualTo(0);
    }

    private readonly record struct FakeEntity(Ulid Id, int Value) : IHasId;
}
