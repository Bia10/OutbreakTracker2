using Avalonia.Media;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;

namespace OutbreakTracker2.UnitTests;

public sealed class InGameEnemyViewModelTests
{
    [Test]
    public async Task GetEnemyColorForFileTwo_ReturnsDefaultColor_ForLiveLicker()
    {
        Color color = InGameEnemyViewModel.GetEnemyColorForFileTwo(slotId: 49, nameId: 0);

        await Assert.That(color).IsEqualTo(Color.FromArgb(255, 255, 255, 255));
    }
}
