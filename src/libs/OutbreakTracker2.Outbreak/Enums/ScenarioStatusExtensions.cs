namespace OutbreakTracker2.Outbreak.Enums;

public static class ScenarioStatusExtensions
{
    public static bool IsGameplayActive(this ScenarioStatus status) => status == ScenarioStatus.InGame;

    public static bool ShouldShowGameplayUi(this ScenarioStatus status, bool showGameplayUiDuringTransitions) =>
        status.IsGameplayActive() || (showGameplayUiDuringTransitions && status.IsTransitional());

    public static bool IsTransitional(this ScenarioStatus status) =>
        status
            is ScenarioStatus.TransitionLoading
                or ScenarioStatus.CinematicPlaying
                or ScenarioStatus.GenericLoading
                or ScenarioStatus.PostIntroLoading
                or ScenarioStatus.Unknown8
                or ScenarioStatus.Unknown9
                or ScenarioStatus.Unknown10
                or ScenarioStatus.Unknown11;
}
