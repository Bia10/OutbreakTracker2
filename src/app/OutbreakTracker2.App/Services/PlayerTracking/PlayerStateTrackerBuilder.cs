using OutbreakTracker2.Outbreak.Models;
using System;
using System.Collections.Generic;

namespace OutbreakTracker2.App.Services.PlayerTracking;

public sealed class PlayerStateTrackerBuilder
{
    private readonly List<PlayerStateTrackingRule> _rules = [];

    public PlayerStateTrackerBuilder TrackCondition(string conditionValue, Func<string, string, (string message, ToastType type)> toastDetailsFactory)
    {
        _rules.Add(new ConditionTrackingRule(conditionValue, toastDetailsFactory));
        return this;
    }

    public PlayerStateTrackerBuilder TrackStatus(string statusValue, Func<string, string, (string message, ToastType type)> toastDetailsFactory)
    {
        _rules.Add(new StatusTrackingRule(statusValue, toastDetailsFactory));
        return this;
    }

    public PlayerStateTrackerBuilder TrackGeneralChange(
        Func<DecodedInGamePlayer, DecodedInGamePlayer, bool> condition,
        Func<DecodedInGamePlayer, PlayerStateChangeEventArgs> notificationFactory)
    {
        _rules.Add(new CustomTrackingRule(condition, notificationFactory));
        return this;
    }

    public IReadOnlyList<PlayerStateTrackingRule> BuildRules()
        => _rules.AsReadOnly();
}