using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum InventoryItem : byte
{
    [EnumMember(Value = "Green herb")]
    GreenHerb1 = 1,

    [EnumMember(Value = "Green herb")]
    GreenHerb2 = 2,

    [EnumMember(Value = "First aid spray")]
    FirstAidSpray1 = 3,

    [EnumMember(Value = "First aid spray")]
    FirstAidSpray2 = 4,

    [EnumMember(Value = "Handgun")]
    Handgun1 = 5,

    [EnumMember(Value = "Handgun rounds")]
    HandgunRounds = 6,

    [EnumMember(Value = "Handgun")]
    Handgun2 = 10,

    [EnumMember(Value = "Scrub brush")]
    ScrubBrush = 17,

    [EnumMember(Value = "Red herb")]
    RedHerb = 70,

    [EnumMember(Value = "Employee area key")]
    EmployeeAreaKey = 93,

    [EnumMember(Value = "Alcohol bottle")]
    AlcoholBottle = 95,

    [EnumMember(Value = "Assault rifle")]
    AssaultRifle = 96,

    // TODO: handle special items
    [EnumMember(Value = "45 Auto magazine")]
    AutoMagazine45 = 173,

    [EnumMember(Value = "Lighter")]
    Lighter = 173,

    [EnumMember(Value = "Bandage")]
    Bandage = 173,

    [EnumMember(Value = "Charm")]
    Charm = 173,

    [EnumMember(Value = "Lucky coin")]
    LuckyCoin = 173,

    [EnumMember(Value = "Stun gun")]
    StunGun = 173,

    [EnumMember(Value = "Capsule shooter blue")]
    CapsuleShooter = 173,
    
    [EnumMember(Value = "HerbCase")]
    HerbCase = 174,

    [EnumMember(Value = "Kanpsack")]
    Knapsack = 174,

    [EnumMember(Value = "Coin")]
    Coin = 174,

    [EnumMember(Value = "Picking tool")]
    PickingTool = 174,
    
    [EnumMember(Value = "Medical set")]
    MedicalSet = 174,
    
    [EnumMember(Value = "Folding knife")]
    FoldingKnife = 175,

    [EnumMember(Value = "Green herb")]
    GreenHerb3 = 175,

    [EnumMember(Value = "Employee area key")]
    EmployeeAreaKey2 = 175,

    [EnumMember(Value = "I-Shaped pick")]
    IShapedPick = 175,

    [EnumMember(Value = "Monkey wrench")]
    MonkeyWrench = 176,

    [EnumMember(Value = "S-Shaped pick")]
    SShapedPick = 176,

    [EnumMember(Value = "Blue herb")]
    BlueHerb = 177,

    [EnumMember(Value = "Vinyl tape")]
    VinylTape = 177,

    [EnumMember(Value = "W-Shaped pick")]
    WShapedPick = 177,

    [EnumMember(Value = "Junk parts")]
    JunkParts = 178,

    [EnumMember(Value = "Mixed herbs")]
    MixedHerbsBR = 178,

    [EnumMember(Value = "P-Shaped pick")]
    PShapedPick = 178,

    [EnumMember(Value = "Red herb")]
    RedHerb2 = 179,

    [EnumMember(Value = "45 Auto for Kevin")]
    AutoHandgun45 = 220,
}