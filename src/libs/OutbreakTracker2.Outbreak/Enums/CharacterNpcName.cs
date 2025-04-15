using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

internal enum CharacterNpcName : byte
{
    [EnumMember(Value = "Character is not NPC!")]
    NotNpc = 0,

    [EnumMember(Value = "MacDowell")] //HPtype3
    MacDowell = 1,

    [EnumMember(Value = "Rodriguez")] //HPtype4
    Rodriguez = 2,

    [EnumMember(Value = "Conrad")] //HPtype2
    Conrad = 3,

    [EnumMember(Value = "Hunk:B")] //HPtype3
    HunkB = 4,

    [EnumMember(Value = "Hunk")] //HPtype5
    Hunk = 5,

    [EnumMember(Value = "Miguel")] //HPtype3
    Miguel = 6,

    [EnumMember(Value = "Miguel:B")] //Wrong texture
    MiguelB = 7,

    [EnumMember(Value = "Luke")] //HPtype4
    Luke = 8,

    [EnumMember(Value = "MacDowell:B")] //Broken skeleton
    MacDowellB = 9,

    [EnumMember(Value = "Arnold")] //HPtype5
    Arnold = 10,

    [EnumMember(Value = "Matt")] //HPtype2
    Matt = 11,

    [EnumMember(Value = "Billy")] //HPtype3
    Billy = 12,

    [EnumMember(Value = "Harsh")] //HPtype3
    Harsh = 13,

    [EnumMember(Value = "Harsh:B")] //Duplicate
    HarshB = 14,

    [EnumMember(Value = "Harsh:C")] //Leechman
    HarshC = 15,

    [EnumMember(Value = "Scott")] //Original Leechman
    Scott = 16,

    [EnumMember(Value = "Peter")] //HPtype3
    Peter = 17,

    [EnumMember(Value = "Marvin")] //HPtype3
    Marvin = 18,

    [EnumMember(Value = "Fred")] //HPtype2
    Fred = 19,

    [EnumMember(Value = "Andy")] //HPtype3
    Andy = 20,

    [EnumMember(Value = "Marvin:B")] //Broken skeleton/Injured
    MarvinB = 21,

    [EnumMember(Value = "Jean")] //HPtype4
    Jean = 22,

    [EnumMember(Value = "Tony")] //HPtype4
    Tony = 23,

    [EnumMember(Value = "Patrick:B")] //Injured/Zombie
    PatrickB = 24,

    [EnumMember(Value = "Patrick")] //HPtype3
    Patrick = 25,

    [EnumMember(Value = "Lloyd")] //HPtype3
    Lloyd = 26,

    [EnumMember(Value = "Austin")] //HPtype3
    Austin = 27,

    [EnumMember(Value = "Clint")] //HPtype4
    Clint = 28,

    [EnumMember(Value = "Bone")] //HPtype2
    Bone = 29,

    [EnumMember(Value = "Bob")] //HPtype5
    Bob = 30,

    [EnumMember(Value = "Jack Zombie")] //FIRST LEECHMAN?
    JackZombie = 31,

    [EnumMember(Value = "Nathan")] //HPtype3
    Nathan = 32,

    [EnumMember(Value = "Samuel")] //HPtype4
    Samuel = 33,

    [EnumMember(Value = "MacDowell:C")] //Broken skeleton bug
    MacDowellC = 34,

    [EnumMember(Value = "Will")] //HPtype2
    Will = 35,

    [EnumMember(Value = "Will:B")] //Zombie
    WillB = 36,

    [EnumMember(Value = "Roger")] //HPtype3
    Roger = 37,

    [EnumMember(Value = "MacDowell:D")] //Broken skeleton bug
    MacDowellD = 38,

    [EnumMember(Value = "Carter")] //HPtype3
    Carter = 39,

    [EnumMember(Value = "Greg")] //HPtype6
    Greg = 40,

    [EnumMember(Value = "Frost")] //HPtype4
    Frost = 41,

    [EnumMember(Value = "Frost:B")] //HPtype3
    FrostB = 42,

    [EnumMember(Value = "Jake")] //HPtype1
    Jake = 43,

    [EnumMember(Value = "Gary")] //HPtype3
    Gary = 44,

    [EnumMember(Value = "Richard")] //HPtype4
    Richard = 45,

    [EnumMember(Value = "Mickey:B")] //Zombie
    MickeyB = 46,

    [EnumMember(Value = "Mickey")] //HPtype2
    Mickey = 47,

    [EnumMember(Value = "Al")] //HPtype3
    Al = 48,

    [EnumMember(Value = "Axeman")] //HPtype5
    Axeman = 49,

    [EnumMember(Value = "Al:B")] //HPtype2
    AlB = 50,

    [EnumMember(Value = "Ben")] //HPtype3
    Ben = 51,

    [EnumMember(Value = "Lucy")] //LUCY
    Lucy = 52,

    [EnumMember(Value = "Regan")] //HPtype3
    Regan = 53,

    [EnumMember(Value = "Regan:B")] //HPtype3
    ReganB = 54,

    [EnumMember(Value = "Monica")] //HPtype2
    Monica = 55,

    [EnumMember(Value = "Linda")] //HPtype2
    Linda = 56,

    [EnumMember(Value = "Rita")] //HPtype4
    Rita = 57,

    [EnumMember(Value = "MacDowell:E")] //Broken skeleton bug
    MacDowellE = 58,

    [EnumMember(Value = "Mary")] //HPtype3
    Mary = 59,

    [EnumMember(Value = "Kate")] //HPtype3
    Kate = 60,

    [EnumMember(Value = "MacDowell:F")] //Broken skeleton bug
    MacDowellF = 61,

    [EnumMember(Value = "MacDowell:G")] //Broken skeleton bug
    MacDowellG = 62,

    [EnumMember(Value = "Danny")] //HPtype4
    Danny = 63,

    [EnumMember(Value = "Danny:B")] //HPtype5
    DannyB = 64,

    [EnumMember(Value = "Gill")] //HPtype2
    Gill = 65,

    [EnumMember(Value = "Gill:B")] //HPtype3
    GillB = 66,

    // Gap 67-69

    [EnumMember(Value = "Ethan:C")] //Duplicate
    EthanC = 70,

    // Gap 71-72

    [EnumMember(Value = "Cindy")] //Wild Things cameo
    Cindy = 73,

    [EnumMember(Value = "Keith")] //HPtype4
    Keith = 74,

    // Gap 75-80

    [EnumMember(Value = "Kurt")] //HPtype3
    Kurt = 81,

    [EnumMember(Value = "Kurt:B")] //HPtype3
    KurtB = 82,

    [EnumMember(Value = "Gary:B")] //HPtype3
    GaryB = 83,

    [EnumMember(Value = "Al:C")] //HPtype3
    AlC = 84,

    // Gap 85-86

    [EnumMember(Value = "Dorothy")] //HPtype5
    Dorothy = 87,

    // Gap 88-90

    [EnumMember(Value = "Yoko:Z")] //HPtype4
    YokoZ = 91,

    // Gap 92-99

    [EnumMember(Value = "Mr.Grey")] //MR.GREY
    MrGrey = 100,

    [EnumMember(Value = "Raymond")] //HPtype3
    Raymond = 101,

    [EnumMember(Value = "Arthur")] //HPtype2
    Arthur = 102,

    [EnumMember(Value = "Aaron")] //HPtype5
    Aaron = 103,

    [EnumMember(Value = "Dorian")] //HPtype3
    Dorian = 104,

    [EnumMember(Value = "Elliot")] //HPtype4
    Elliot = 105,

    [EnumMember(Value = "Eric")] //HPtype2
    Eric = 106,

    [EnumMember(Value = "Harry")] //HPtype2
    Harry = 107,

    [EnumMember(Value = "Mr.Red")] //HPtype0
    MrRed = 108,

    [EnumMember(Value = "Mr.Blue")] //HPtype0
    MrBlue = 109,

    [EnumMember(Value = "Mr.Green")] //HPtype6
    MrGreen = 110,

    [EnumMember(Value = "Mr.Gold")] //HPtype6
    MrGold = 111,

    [EnumMember(Value = "Mr.Black")] //HPtype6
    MrBlack = 112,

    [EnumMember(Value = "Karl")] //HPtype5
    Karl = 113,

    [EnumMember(Value = "Dustin")] //HPtype4
    Dustin = 114,

    [EnumMember(Value = "Dustin:B")] //With helmet
    DustinB = 115,

    [EnumMember(Value = "Derek")] //HPtype3
    Derek = 116,

    [EnumMember(Value = "Ms.White")] //HPtype0
    MsWhite = 117,

    [EnumMember(Value = "Ms.Peach")] //HPtype6
    MsPeach = 118,

    [EnumMember(Value = "Ms.Water")] //HPtype6
    MsWater = 119,

    [EnumMember(Value = "Len")] //HPtype4
    Len = 120,

    [EnumMember(Value = "Nick")] //HPtype3
    Nick = 121,

    [EnumMember(Value = "Sean")] //HPtype5
    Sean = 122,

    [EnumMember(Value = "Philip")] //HPtype4
    Philip = 123,

    [EnumMember(Value = "Don")] //HPtype3
    Don = 124,

    [EnumMember(Value = "Matthew")] //HPtype3
    Matthew = 125,

    [EnumMember(Value = "Robert")] //HPtype4
    Robert = 126,

    [EnumMember(Value = "Chuck")] //HPtype2
    Chuck = 127,

    [EnumMember(Value = "Ginger")] //HPtype4
    Ginger = 128,

    [EnumMember(Value = "Laura")] //HPtype2
    Laura = 129,

    [EnumMember(Value = "Amelia")] //HPtype3
    Amelia = 130,

    [EnumMember(Value = "Ethan")] //HPtype2
    Ethan = 131,

    [EnumMember(Value = "Ethan:B")] //Zombie
    EthanB = 132,

    [EnumMember(Value = "Howard")] //HPtype3
    Howard = 133,

    [EnumMember(Value = "Howard:B")] //HOWARD:B
    HowardB = 134,

    [EnumMember(Value = "Isaac")] //HPtype4
    Isaac = 135,

    [EnumMember(Value = "Isaac:B")] //ISAAC:B
    IsaacB = 136,

    [EnumMember(Value = "Kathy")] //HPtype2
    Kathy = 137,

    [EnumMember(Value = "Kathy:B")] //Injured
    KathyB = 138,

    [EnumMember(Value = "Elena")] //HPtype4
    Elena = 139,

    [EnumMember(Value = "Elena:B")] //Zombie
    ElenaB = 140,

    [EnumMember(Value = "Frank")] //HPtype4
    Frank = 141,

    [EnumMember(Value = "Kathy:C")] //Missing texture
    KathyC = 142,

    // Gap 143-150

    [EnumMember(Value = "Rodney")] //HPtype2
    Rodney = 151,

    // Gap 152-199

    [EnumMember(Value = "Cindy:B")] //Missing skirt
    CindyB = 200,

    [EnumMember(Value = "Kevin")] //Elimination cameo
    Kevin = 201,

    [EnumMember(Value = "Mark")] //Elimination cameo
    Mark = 202,

    [EnumMember(Value = "Jim")] //Elimination cameo
    Jim = 203,

    [EnumMember(Value = "George")] //Elimination cameo
    George = 204,

    [EnumMember(Value = "David")] //Elimination cameo
    David = 205,

    [EnumMember(Value = "Alyssa")] //Elimination cameo
    Alyssa = 206,

    [EnumMember(Value = "Yoko")] //Elimination cameo
    Yoko = 207,

    [EnumMember(Value = "Cindy:C")] //Elimination cameo
    CindyC = 208,

    [EnumMember(Value = "Unknown")]
    Unknown = 255
}
