using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum InventoryItem : byte
{
    [EnumMember(Value = "First aid spray")]
    FirstAidSpray = 4,
    
    [EnumMember(Value = "Handgun")]
    Handgun = 5,
    
    [EnumMember(Value = "Handgun rounds")]
    HandgunRounds = 6,
     
    [EnumMember(Value = "45 Auto magazine")]
    AutoMagazine45 = 173,
    
    [EnumMember(Value = "45 Auto for Kevin")]
    AutoHandgun45 = 220,
}