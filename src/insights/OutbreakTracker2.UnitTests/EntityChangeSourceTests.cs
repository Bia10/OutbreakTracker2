using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.UnitTests;

public sealed class EntityChangeSourceTests
{
    [Test]
    public async Task Diffs_StayWarmAcrossSubscriberGaps()
    {
        using Subject<FakeEntity[]> snapshots = new();
        using EntityChangeSource<FakeEntity> source = new(snapshots);

        FakeEntity entity1 = new(Ulid.NewUlid(), 1);
        FakeEntity entity2 = new(Ulid.NewUlid(), 2);
        FakeEntity entity3 = new(Ulid.NewUlid(), 3);

        List<CollectionDiff<FakeEntity>> firstDiffs = [];
        IDisposable firstSubscription = source.Diffs.Subscribe(diff => firstDiffs.Add(diff));

        snapshots.OnNext([entity1]);

        await Assert.That(firstDiffs.Count).IsEqualTo(1);
        await Assert.That(firstDiffs[0].Added.Count).IsEqualTo(1);
        await Assert.That(firstDiffs[0].Added[0].Id).IsEqualTo(entity1.Id);

        firstSubscription.Dispose();

        snapshots.OnNext([entity1, entity2]);

        List<CollectionDiff<FakeEntity>> secondDiffs = [];
        using IDisposable secondSubscription = source.Diffs.Subscribe(diff => secondDiffs.Add(diff));

        snapshots.OnNext([entity1, entity2, entity3]);

        await Assert.That(secondDiffs.Count).IsEqualTo(1);
        await Assert.That(secondDiffs[0].Added.Count).IsEqualTo(1);
        await Assert.That(secondDiffs[0].Added[0].Id).IsEqualTo(entity3.Id);
        await Assert.That(secondDiffs[0].Removed.Count).IsEqualTo(0);
        await Assert.That(secondDiffs[0].Changed.Count).IsEqualTo(0);
    }

    private readonly record struct FakeEntity(Ulid Id, int Value) : IHasId;
}
