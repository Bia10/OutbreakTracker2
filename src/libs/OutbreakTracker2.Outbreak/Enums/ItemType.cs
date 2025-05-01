namespace OutbreakTracker2.Outbreak.Enums;

using System.Runtime.Serialization;

public enum ItemType : short
{
    [EnumMember(Value = "Unknown")]
    Unknown = -1,

    // 0-32: Firearms
    [EnumMember(Value = "Handgun")]
    Handgun = 0,

    [EnumMember(Value = "Handgun SG")]
    HandgunSG = 1,

    [EnumMember(Value = "Handgun GL")]
    HandgunGL = 2,

    [EnumMember(Value = "Magnum Handgun")]
    MagnumHandgun = 3,

    [EnumMember(Value = "Magnum Revolver")]
    MagnumRevolver = 4,

    [EnumMember(Value = "Handgun HP")]
    HandgunHP = 5,

    [EnumMember(Value = "S&W model 500")]
    SWModel500 = 6,

    [EnumMember(Value = "Revolver")]
    Revolver = 7,

    [EnumMember(Value = "Burst Handgun")]
    BurstHandgun = 8,

    [EnumMember(Value = "Unused1(no name)")]
    Unused1 = 9,

    [EnumMember(Value = "Submachine Gun")]
    SubmachineGun = 10,

    [EnumMember(Value = "Shotgun")]
    Shotgun = 11,

    [EnumMember(Value = "Shotgun E")]
    ShotgunE = 12,

    [EnumMember(Value = "Hunting Rifle")]
    HuntingRifle = 13,

    [EnumMember(Value = "Assault Rifle")]
    AssaultRifle = 14,

    [EnumMember(Value = "Capsule Shooter(BLUE)")]
    CapsuleShooterBlue = 15,

    [EnumMember(Value = "Capsule Shooter(RED)")]
    CapsuleShooterRed = 16,

    [EnumMember(Value = "Capsule Shooter(GREEN)")]
    CapsuleShooterGreen = 17,

    [EnumMember(Value = "Capsule Shooter(WHITE)")]
    CapsuleShooterWhite = 18,

    [EnumMember(Value = "Mine Detector")]
    MineDetector = 19,

    [EnumMember(Value = "Rocket Launcher")]
    RocketLauncher = 20,

    [EnumMember(Value = "UnknownItem21")]
    UnknownItem21 = 21,

    [EnumMember(Value = "Unused22(no name)")]
    Unused22 = 22,

    [EnumMember(Value = "Grenade Launcher (BURST)")]
    GrenadeLauncherBurst = 23,

    [EnumMember(Value = "Grenade Launcher (FLAME)")]
    GrenadeLauncherFlame = 24,

    [EnumMember(Value = "Grenade Launcher (ACID)")]
    GrenadeLauncherAcid = 25,

    [EnumMember(Value = "Grenade Launcher (Fire Extinguishing)")]
    GrenadeLauncherFireExtinguishing = 26,

    [EnumMember(Value = "Grenade Launcher (BOW GAS)")]
    GrenadeLauncherBowGas = 27,

    [EnumMember(Value = "Pesticide Spray")]
    PesticideSpray = 28,

    [EnumMember(Value = "Flame Spray")]
    FlameSpray = 29,

    [EnumMember(Value = "Stun Gun")]
    StunGun = 30,

    [EnumMember(Value = "Nail Gun")]
    NailGun = 31,

    [EnumMember(Value = "Ampoule Shooter")]
    AmpouleShooter = 32,

    // 100-119: Melee Weapons
    [EnumMember(Value = "Survival Knife")]
    SurvivalKnife = 100,

    [EnumMember(Value = "Folding Knife")]
    FoldingKnife = 101,

    [EnumMember(Value = "Butcher Knife")]
    ButcherKnife = 102,

    [EnumMember(Value = "Iron Pipe")]
    IronPipe = 103,

    [EnumMember(Value = "Curved Iron Pipe")]
    CurvedIronPipe = 104,

    [EnumMember(Value = "Bent Iron Pipe")]
    BentIronPipe = 105,

    [EnumMember(Value = "Long Pole")]
    LongPole = 106,

    [EnumMember(Value = "Square Timber")]
    SquareTimber = 107,

    [EnumMember(Value = "Bomb Switch")]
    BombSwitch = 108,

    [EnumMember(Value = "Axe")]
    Axe = 109,

    [EnumMember(Value = "Scrub Brush")]
    ScrubBrush = 110,

    [EnumMember(Value = "Wooden Pole")]
    WoodenPole = 111,

    [EnumMember(Value = "Throwable Stick")]
    ThrowableStick = 112,

    [EnumMember(Value = "Spear")]
    Spear = 113,

    [EnumMember(Value = "Molotov Cocktail")]
    MolotovCocktail = 114,

    [EnumMember(Value = "Hammer")]
    Hammer = 115,

    [EnumMember(Value = "Crutch")]
    Crutch = 116,

    [EnumMember(Value = "Stun Rod")]
    StunRod = 117,

    [EnumMember(Value = "Concrete Piece")]
    ConcretePiece = 118,

    [EnumMember(Value = "Broken Crutch")]
    BrokenCrutch = 119,

    // 155-161: Bottles/Bombs
    [EnumMember(Value = "Empty Bottle")]
    EmptyBottle = 155,

    [EnumMember(Value = "Red Chemical Bottle")]
    RedChemicalBottle = 156,

    [EnumMember(Value = "Green Chemical Bottle")]
    GreenChemicalBottle = 157,

    [EnumMember(Value = "Yellow Chemical Bottle")]
    YellowChemicalBottle = 158,

    [EnumMember(Value = "Gray Chemical Bottle")]
    GrayChemicalBottle = 159,

    [EnumMember(Value = "Time Bomb(before ignition)")]
    TimeBombBeforeIgnition = 160,

    [EnumMember(Value = "Time Bomb(after ignition)")]
    TimeBombAfterIgnition = 161,

    // 200-211: Magazines
    [EnumMember(Value = "Handgun Magazine")]
    HandgunMagazine = 200,

    [EnumMember(Value = "Handgun SG Magazine")]
    HandgunSgMagazine = 201,

    [EnumMember(Value = "Handgun GL Magazine")]
    HandgunGlMagazine = 202,

    [EnumMember(Value = "Magnum Clip")]
    MagnumClip = 203,

    [EnumMember(Value = "Magnum Revolver S.Loader")]
    MagnumRevolverSLoader = 204,

    [EnumMember(Value = "Revolver S.Loader")]
    RevolverSLoader = 205,

    [EnumMember(Value = "Sub Machine Gun Clip")]
    SubMachineGunClip = 206,

    [EnumMember(Value = "Assault Rifle Clip")]
    AssaultRifleClip = 207,

    [EnumMember(Value = "Unused")]
    UnusedAmmo = 208,

    [EnumMember(Value = "Burst Hg Magazine")]
    BurstHgMagazine = 209,

    [EnumMember(Value = "45 Auto Magazine")]
    AutoMagazine45 = 210,

    [EnumMember(Value = "Handgun HP Magazine")]
    HandgunHPMagazine = 211,

    // 250-263: Ammo Types
    [EnumMember(Value = "Hand Gun Rounds 9mm")]
    HandgunRounds9mm = 250,

    [EnumMember(Value = "Magnum Hg rounds")]
    MagnumHgRounds = 251,

    [EnumMember(Value = "Magnum Revolver Rounds")]
    MagnumRevolverRounds = 252,

    [EnumMember(Value = "Revolver Rounds")]
    RevolverRounds = 253,

    [EnumMember(Value = "Shotgun Rounds")]
    ShotgunRounds = 254,

    [EnumMember(Value = "Rifle Rounds")]
    RifleRounds = 255,

    [EnumMember(Value = "Rocket Rounds")]
    RocketRounds = 256,

    [EnumMember(Value = "Burst Rounds")]
    BurstRounds = 257,

    [EnumMember(Value = "Flame Rounds")]
    FlameRounds = 258,

    [EnumMember(Value = "Acid Rounds")]
    AcidRounds = 259,

    [EnumMember(Value = "Fire Extinguisher Rounds")]
    FireExtinguisherRounds = 260,

    [EnumMember(Value = "BOW Gas Rounds")]
    BowGasRounds = 261,

    [EnumMember(Value = "45 Auto Rounds")]
    AutoRounds45 = 262,

    [EnumMember(Value = "High Power Revolver Rounds")]
    HighPowerRevolverRounds = 263,

    // 300-318: Healing/Medicine
    [EnumMember(Value = "Green Herb")]
    GreenHerb = 300,

    [EnumMember(Value = "Blue Herb")]
    BlueHerb = 301,

    [EnumMember(Value = "Red Herb")]
    RedHerb = 302,

    [EnumMember(Value = "Mixed Herb (G+G)")]
    MixedHerbGG = 303,

    [EnumMember(Value = "Mixed Herb (G+G+G)")]
    MixedHerbGGG = 304,

    [EnumMember(Value = "Mixed Herb (G+R)")]
    MixedHerbGR = 305,

    [EnumMember(Value = "Mixed Herb (G+B)")]
    MixedHerbGB = 306,

    [EnumMember(Value = "Mixed Herb (G+G+B)")]
    MixedHerbGGB = 307,

    [EnumMember(Value = "Mixed Herb (G+R+B)")]
    MixedHerbGRB = 308,

    [EnumMember(Value = "First Aid Spray")]
    FirstAidSpray = 309,

    [EnumMember(Value = "Recovery Medicine")]
    RecoveryMedicine = 310,

    [EnumMember(Value = "Hemostat")]
    Hemostat = 311,

    [EnumMember(Value = "Daylight")]
    Daylight = 312,

    [EnumMember(Value = "Mixed Herb(R+B)")]
    MixedHerbRB = 313,

    [EnumMember(Value = "Antidote")]
    Antidote = 314,

    [EnumMember(Value = "Recovery Medicine (L)")]
    RecoveryMedicineL = 315,

    [EnumMember(Value = "Anti Virus")]
    AntiVirus = 316,

    [EnumMember(Value = "Anti Virus (L)")]
    AntiVirusL = 317,

    [EnumMember(Value = "Recovery Medicine Base")]
    RecoveryMedicineBase = 318,

    // 400-420: Personal Items/Tools
    [EnumMember(Value = "Kevin's 45 Auto")]
    Kevins45Auto = 400,

    [EnumMember(Value = "Mark's Handgun")]
    MarksHandgun = 401,

    [EnumMember(Value = "Coin holder(Jim's)(unused)")]
    CoinHolderJimsUnused = 402,

    [EnumMember(Value = "Jim's Coin")]
    JimsCoin = 403,

    [EnumMember(Value = "Lock Pick (unused)")]
    LockPickUnused = 404,

    [EnumMember(Value = "Medical Set")]
    MedicalSet = 405,

    [EnumMember(Value = "Tool Box")]
    ToolBox = 406,

    [EnumMember(Value = "Folding Knife(David's)")]
    FoldingKnifeDavids = 407,

    [EnumMember(Value = "Monkey Wrench")]
    MonkeyWrench = 408,

    [EnumMember(Value = "Vinyl Tape")]
    VinylTape = 409,

    [EnumMember(Value = "Junk Parts")]
    JunkParts = 410,

    [EnumMember(Value = "Picking Tool")]
    PickingTool = 411,

    [EnumMember(Value = "Knapsack")]
    Knapsack = 412,

    [EnumMember(Value = "Herb Case")]
    HerbCase = 413,

    [EnumMember(Value = "I-Shaped Pick")]
    IShapedPick = 414,

    [EnumMember(Value = "S-Shaped Pick")]
    SShapedPick = 415,

    [EnumMember(Value = "W-Shaped Pick")]
    WShapedPick = 416,

    [EnumMember(Value = "P-Shaped Pick")]
    PShapedPick = 417,

    [EnumMember(Value = "Bandage")]
    Bandage = 418,

    [EnumMember(Value = "Lucky Coin")]
    LuckyCoin = 419,

    [EnumMember(Value = "Charm")]
    Charm = 420,

    // 450-456: Miscellaneous Items
    [EnumMember(Value = "Lighter")]
    Lighter = 450,

    [EnumMember(Value = "Alcohol Bottle")]
    AlcoholBottle = 451,

    [EnumMember(Value = "Bottle + Newspaper")]
    BottleNewspaper = 452,

    [EnumMember(Value = "Broken Handgun")]
    BrokenHandgun = 453,

    [EnumMember(Value = "Broken Shotgun")]
    BrokenShotgun = 454,

    [EnumMember(Value = "Battery")]
    Battery = 455,

    [EnumMember(Value = "Broken Handgun SG")]
    BrokenHandgunSG = 456,

    // 10100-10108: Keys/Story Items
    [EnumMember(Value = "Staff Room Key")]
    StaffRoomKey = 10100,

    [EnumMember(Value = "Key with a Red Tag (?? OB)")]
    KeyWithRedTagOB = 10101,

    [EnumMember(Value = "Key with a Blue Tag (?? OB)")]
    KeyWithBlueTagOB = 10102,

    [EnumMember(Value = "Forklift Key")]
    ForkliftKey = 10103,

    [EnumMember(Value = "Unknown10104")]
    Unknown10104 = 10104,

    [EnumMember(Value = "Storage Key")]
    StorageKey = 10105,

    [EnumMember(Value = "Detonator Handle")]
    DetonatorHandle = 10106,

    [EnumMember(Value = "Detonator Main Unit")]
    DetonatorMainUnit = 10107,

    [EnumMember(Value = "Detonator")]
    Detonator = 10108,

    // 10150: Notes
    [EnumMember(Value = "Newspaper (?? OB)")]
    NewspaperOB = 10150,

    // 10206-10211: Keys/Story Items
    [EnumMember(Value = "Charlie's ID tag")]
    CharliesIDTag = 10206,

    [EnumMember(Value = "Len's ID tag")]
    LensIDTag = 10207,

    [EnumMember(Value = "Gold Key")]
    GoldKey = 10208,

    [EnumMember(Value = "Silver Key")]
    SilverKey = 10209,

    [EnumMember(Value = "Security Room Card Key")]
    SecurityRoomCardKey = 10210,

    [EnumMember(Value = "Red Jewel (?? HF)")]
    RedJewelHF = 10211,

    // 10600-10614: Keys/Story Items
    [EnumMember(Value = "Examination Room Key")]
    ExaminationRoomKey = 10600,

    [EnumMember(Value = "ID Card Lv1")]
    IDCardLv1 = 10601,

    [EnumMember(Value = "ID Card Lv2")]
    IDCardLv2 = 10602,

    [EnumMember(Value = "MO Disk")]
    MODisk = 10603,

    [EnumMember(Value = "MO Disk(Code A)")]
    MODiskCodeA = 10604,

    [EnumMember(Value = "MO Disk(Code G)")]
    MODiskCodeG = 10605,

    [EnumMember(Value = "Unknown10606")]
    Unknown10606 = 10606,

    [EnumMember(Value = "Unknown10607")]
    Unknown10607 = 10607,

    [EnumMember(Value = "Unknown10608")]
    Unknown10608 = 10608,

    [EnumMember(Value = "Valve Handle (6 Sided)")]
    ValveHandle6Sided = 10609,

    [EnumMember(Value = "Valve Handle (4 Sided)")]
    ValveHandle4Sided = 10610,

    [EnumMember(Value = "Crowbar")]
    Crowbar = 10611,

    [EnumMember(Value = "Model Grenade Launcher")]
    ModelGrenadeLauncher = 10612,

    [EnumMember(Value = "Unknown10613")]
    Unknown10613 = 10613,

    [EnumMember(Value = "??")]
    UnknownItem10614 = 10614,

    // 10650-10652: Notes
    [EnumMember(Value = "Newspaper 1 (?? EOTR)")]
    Newspaper1EOTR = 10650,

    [EnumMember(Value = "Newspaper 2 (?? EOTR)")]
    Newspaper2EOTR = 10651,

    [EnumMember(Value = "Newspaper 3 (?? EOTR)")]
    Newspaper3EOTR = 10652,

    // 11000-11010: Keys/Story Items
    [EnumMember(Value = "Employee Area Key")]
    EmployeeAreaKey = 11000,

    [EnumMember(Value = "B2F Key")]
    B2FKey = 11001,

    [EnumMember(Value = "Ventilation Tower Key")]
    VentilationTowerKey = 11002,

    [EnumMember(Value = "Valve Handle (?? UB)")]
    ValveHandleUB = 11003,

    [EnumMember(Value = "Repair Tape")]
    RepairTape = 11004,

    [EnumMember(Value = "Rubber Sheet(unused)")]
    RubberSheetUnused = 11005,

    [EnumMember(Value = "Founder's Emblem(Werner/Gold)")]
    FoundersEmblemWernerGold = 11006,

    [EnumMember(Value = "Founder's Emblem(Oral/Blue)")]
    FoundersEmblemOralBlue = 11007,

    [EnumMember(Value = "Model Train Wheel")]
    ModelTrainWheel = 11008,

    [EnumMember(Value = "Blood Pack (?? UB)(already used)")]
    BloodPackUBAlreadyUsed = 11009,

    [EnumMember(Value = "Blood Pack (?? UB)")]
    BloodPackUB = 11010,

    // 11050-11052: Notes
    [EnumMember(Value = "Newspaper 1 (?? UB)")]
    Newspaper1UB = 11050,

    [EnumMember(Value = "Newspaper 2 (?? UB)")]
    Newspaper2UB = 11051,

    [EnumMember(Value = "Newspaper 3 (?? UB)")]
    Newspaper3UB = 11052,

    // 11500-11515: Keys/Story Items
    [EnumMember(Value = "Joker Key")]
    JokerKey = 11500,

    [EnumMember(Value = "Onyx Plate")]
    OnyxPlate = 11501,

    [EnumMember(Value = "Sapphire Plate")]
    SapphirePlate = 11502,

    [EnumMember(Value = "Ruby Plate")]
    RubyPlate = 11503,

    [EnumMember(Value = "Emerald Plate")]
    EmeraldPlate = 11504,

    [EnumMember(Value = "Amethyst Plate")]
    AmethystPlate = 11505,

    [EnumMember(Value = "Padlock Key")]
    PadlockKey = 11506,

    [EnumMember(Value = "Ace Key")]
    AceKey = 11507,

    [EnumMember(Value = "Gas Canister")]
    GasCanister = 11508,

    [EnumMember(Value = "Plywood Board")]
    PlywoodBoard = 11509,

    [EnumMember(Value = "Film A")]
    FilmA = 11510,

    [EnumMember(Value = "Used Gas Canister")]
    UsedGasCanister = 11511,

    [EnumMember(Value = "Unicorn Medal")]
    UnicornMedal = 11512,

    [EnumMember(Value = "Film B")]
    FilmB = 11513,

    [EnumMember(Value = "Film C")]
    FilmC = 11514,

    [EnumMember(Value = "Film D")]
    FilmD = 11515,

    // 11550: Notes
    [EnumMember(Value = "Secret File")]
    SecretFile = 11550,

    // 12600-12605: Keys/Story Items
    [EnumMember(Value = "Auxiliary Building Key")]
    AuxiliaryBuildingKey = 12600,

    [EnumMember(Value = "Administrator's Office Key")]
    AdministratorsOfficeKey = 12601,

    [EnumMember(Value = "Rusty Key")]
    RustyKey = 12602,

    [EnumMember(Value = "Syringe (empty)")]
    SyringeEmpty = 12603,

    [EnumMember(Value = "Syringe (solvent)")]
    SyringeSolvent = 12604,

    [EnumMember(Value = "Pendant")]
    Pendant = 12605,

    // 12800-12807: Keys/Story Items
    [EnumMember(Value = "Blood pack (?? TH)(already used)")]
    BloodPackTHAlreadyUsed = 12800,

    [EnumMember(Value = "Blood pack (?? TH)")]
    BloodPackTH = 12801,

    [EnumMember(Value = "Unknown12802")]
    Unknown12802 = 12802,

    [EnumMember(Value = "Unknown12803")]
    Unknown12803 = 12803,

    [EnumMember(Value = "Unknown12804")]
    Unknown12804 = 12804,

    [EnumMember(Value = "Level 1 Card Key (?? TH)")]
    Level1CardKeyTH = 12805,

    [EnumMember(Value = "Level 2 Card Key (?? TH)")]
    Level2CardKeyTH = 12806,

    [EnumMember(Value = "Chain Key")]
    ChainKey = 12807,

    // 12850-12852: Notes
    [EnumMember(Value = "Male Nurse's Diary")]
    MaleNursesDiary = 12850,

    [EnumMember(Value = "Researcher's Diary")]
    ResearchersDiary = 12851,

    [EnumMember(Value = "Research Request")]
    ResearchRequest = 12852,

    // 13500-13508: Keys/Story Items
    [EnumMember(Value = "UMB No.3")]
    UMBNo3 = 13500,

    [EnumMember(Value = "VP-017")]
    VP017 = 13501,

    [EnumMember(Value = "V-JOLT")]
    VJOLT = 13502,

    [EnumMember(Value = "Wrench")]
    Wrench = 13503,

    [EnumMember(Value = "Frozen Wrench")]
    FrozenWrench = 13504,

    [EnumMember(Value = "Lab Cardkey")]
    LabCardkey = 13505,

    [EnumMember(Value = "Turntable Key")]
    TurntableKey = 13506,

    [EnumMember(Value = "Valve Handle (?? BFP)")]
    ValveHandleBFP = 13507,

    [EnumMember(Value = "Hand Burner")]
    HandBurner = 13508,

    // 13550: Notes
    [EnumMember(Value = "no data(unused)")]
    NoDataUnused = 13550,

    // 14000-14009: Keys/Story Items
    [EnumMember(Value = "Bolt Cutters")]
    BoltCutters = 14000,

    [EnumMember(Value = "Elephant Key")]
    ElephantKey = 14001,

    [EnumMember(Value = "Alligator Key")]
    AlligatorKey = 14002,

    [EnumMember(Value = "Lion Key")]
    LionKey = 14003,

    [EnumMember(Value = "Office Key")]
    OfficeKey = 14004,

    [EnumMember(Value = "Mr. Racoon Medal")]
    MrRacoonMedal = 14005,

    [EnumMember(Value = "Lion Emblem (RED)")]
    LionEmblemRED = 14006,

    [EnumMember(Value = "Lion Emblem (BLUE)")]
    LionEmblemBLUE = 14007,

    [EnumMember(Value = "Blank Tape")]
    BlankTape = 14008,

    [EnumMember(Value = "Parade BGM Tape")]
    ParadeBGMTape = 14009,

    // 14100-14111: Keys/Story Items
    [EnumMember(Value = "Brass Spectacles")]
    BrassSpectacles = 14100,

    [EnumMember(Value = "Card Key (?? DD)")]
    CardKeyDD = 14101,

    [EnumMember(Value = "V-Poison")]
    VPoison = 14102,

    [EnumMember(Value = "P-Base")]
    PBase = 14103,

    [EnumMember(Value = "Reagent Case")]
    ReagentCase = 14104,

    [EnumMember(Value = "Sealed Reagent Case")]
    SealedReagentCase = 14105,

    [EnumMember(Value = "P-Base(sealed)")]
    PBaseSealed = 14106,

    [EnumMember(Value = "T-Blood")]
    TBlood = 14107,

    [EnumMember(Value = "Red Jewel (?? DD)")]
    RedJewelDD = 14108,

    [EnumMember(Value = "Blue Jewel (?? DD)")]
    BlueJewelDD = 14109,

    [EnumMember(Value = "P-Base(depleted)")]
    PBaseDepleted = 14110,

    [EnumMember(Value = "Key with a Red Tag (?? DD)")]
    KeyWithRedTagDD = 14111
}