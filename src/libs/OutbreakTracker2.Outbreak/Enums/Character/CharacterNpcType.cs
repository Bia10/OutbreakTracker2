using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Character;

internal enum CharacterNpcType : byte
{
    [EnumMember(Value = "Main Characters")]
    MainCharacters = 0,

    [EnumMember(Value = "Other NPCs")]
    OtherNPCs = 1,

    [EnumMember(Value = "Unknown")]
    Unknown = 255
}
