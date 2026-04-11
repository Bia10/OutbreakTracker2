using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer;

namespace OutbreakTracker2.UnitTests;

public sealed class PlayerGaugesViewModelTests
{
    [Test]
    public async Task Update_ClampsPercentagesAboveProgressBarRange()
    {
        PlayerGaugesViewModel viewModel = new();

        viewModel.Update(
            currentHealth: 120,
            maximumHealth: 100,
            healthPercentage: 120.0,
            curVirus: 125,
            maxVirus: 100,
            virusPercentage: 125.0
        );

        await Assert.That(viewModel.HealthPercentage).IsEqualTo(100.0);
        await Assert.That(viewModel.VirusPercentage).IsEqualTo(100.0);
    }

    [Test]
    public async Task Update_ClampsNegativePercentagesToZero()
    {
        PlayerGaugesViewModel viewModel = new();

        viewModel.Update(
            currentHealth: 0,
            maximumHealth: 100,
            healthPercentage: -5.0,
            curVirus: 0,
            maxVirus: 100,
            virusPercentage: -2.5
        );

        await Assert.That(viewModel.HealthPercentage).IsEqualTo(0.0);
        await Assert.That(viewModel.VirusPercentage).IsEqualTo(0.0);
    }
}
