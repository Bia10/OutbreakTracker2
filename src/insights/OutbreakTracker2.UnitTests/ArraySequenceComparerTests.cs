using OutbreakTracker2.Application.Comparers;

namespace OutbreakTracker2.UnitTests;

public sealed class ArraySequenceComparerTests
{
    [Test]
    public async Task Equals_ReturnsTrue_ForArraysWithSameSequence()
    {
        ArraySequenceComparer<int> comparer = new();

        bool result = comparer.Equals([1, 2, 3], [1, 2, 3]);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Equals_ReturnsFalse_ForArraysWithDifferentSequence()
    {
        ArraySequenceComparer<int> comparer = new();

        bool result = comparer.Equals([1, 2, 3], [3, 2, 1]);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task GetHashCode_ReturnsZero_ForNullArray()
    {
        ArraySequenceComparer<int> comparer = new();

        int hashCode = comparer.GetHashCode(null);

        await Assert.That(hashCode).IsEqualTo(0);
    }

    [Test]
    public async Task GetHashCode_ReturnsSameValue_ForEquivalentArrays()
    {
        ArraySequenceComparer<int> comparer = new();

        int left = comparer.GetHashCode([4, 5, 6]);
        int right = comparer.GetHashCode([4, 5, 6]);

        await Assert.That(left).IsEqualTo(right);
    }
}
