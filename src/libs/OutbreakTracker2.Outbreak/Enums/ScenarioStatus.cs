using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum ScenarioStatus : byte
{
    [EnumMember(Value = "None")]
    None = 0,

    [EnumMember(Value = "Post-intro Loading")]
    PostIntroLoading = 1,

    [EnumMember(Value = "In Game")]
    InGame = 2,

    [EnumMember(Value = "Room Transition Loading")]
    RoomTransitionLoading = 3,

    [EnumMember(Value = "Cinematic Playing")]
    CinematicPlaying = 4,

    [EnumMember(Value = "Generic Loading")]
    GenericLoading = 7,

    [EnumMember(Value = "Unknown8")]
    Unknown8 = 8,

    [EnumMember(Value = "Unknown9")]
    Unknown9 = 9,

    [EnumMember(Value = "Unknown10")]
    Unknown10 = 10,

    [EnumMember(Value = "Unknown11")]
    Unknown11 = 11,

    [EnumMember(Value = "Game Finished")]
    GameFinished = 12,

    [EnumMember(Value = "Rank Screen")]
    RankScreen = 13,

    [EnumMember(Value = "Intro Cinematic")]
    IntroCinematic = 14,

    [EnumMember(Value = "After Save to Memory Card")]
    AfterSaveToMemoryCard = 15,
}
