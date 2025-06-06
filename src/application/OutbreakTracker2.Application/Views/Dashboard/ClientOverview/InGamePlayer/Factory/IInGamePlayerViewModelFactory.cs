using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.Factory;

public interface IInGamePlayerViewModelFactory
{
    InGamePlayerViewModel Create(DecodedInGamePlayer playerData);
}