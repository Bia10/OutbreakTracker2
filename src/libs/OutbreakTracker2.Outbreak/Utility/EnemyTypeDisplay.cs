using OutbreakTracker2.Outbreak.Enums.Enemy;

namespace OutbreakTracker2.Outbreak.Utility;

/// <summary>
/// Display-name and boss-type resolution helpers for enemy entities.
/// Separated from <c>EnemiesReader</c> so that the reader remains a pure data-extraction class.
/// </summary>
public static class EnemyTypeDisplay
{
    /// <summary>
    /// Returns the boss-tier value for a given enemy:
    /// 0 = regular, 1 = mini-boss, 2 = final boss.
    /// </summary>
    public static byte GetBossType(byte nameId, ushort maxHp)
    {
        if (!EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType))
            return 0;

        return enemyType switch
        {
            EnemyType.Thanatos or EnemyType.Nyx or EnemyType.NyxTyrant or EnemyType.Tyrant => 2,
            EnemyType.GLeech
            or EnemyType.Leechman
            or EnemyType.GMutant
            or EnemyType.Titan
            or EnemyType.Lion
            or EnemyType.TyrantQuestion
            or EnemyType.NyxCore
            or EnemyType.Axeman
            or EnemyType.Gigabyte => 1,
            _ => 0,
        };
    }

    /// <summary>
    /// Resolves the display name for an enemy using its <paramref name="nameId"/> and
    /// optional subtype <paramref name="typeId"/> (e.g. zombie variant, dog breed).
    /// </summary>
    public static string ResolveDisplayName(byte nameId, byte typeId)
    {
        if (typeId <= 1)
            return GetEnemyName(nameId);

        if (!EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType))
            return GetEnemyName(nameId);

        string enemyName = GetEnemyName(nameId);
        static string Prefer(string variant, string fallback) => string.IsNullOrEmpty(variant) ? fallback : variant;

        return enemyType switch
        {
            EnemyType.Zombie => Prefer(GetZombieName(typeId), enemyName),
            EnemyType.Dog => Prefer(GetDogName(typeId), enemyName),
            EnemyType.ScissorTail => Prefer(GetScissorTailName(typeId), enemyName),
            EnemyType.Lion => Prefer(GetLionName(typeId), enemyName),
            EnemyType.Tyrant => Prefer(GetTyrantName(typeId), enemyName),
            EnemyType.Thanatos => Prefer(GetThanatosName(typeId), enemyName),
            _ => enemyName,
        };
    }

    private static string GetEnemyName(byte nameId) => EnumUtility.GetEnumString(nameId, EnemyType.Unknown);

    private static string GetZombieName(byte typeId) => EnumUtility.GetEnumString(typeId, ZombieType.Unknown0);

    private static string GetDogName(byte typeId) => EnumUtility.GetEnumString(typeId, DogType.Unknown0);

    private static string GetScissorTailName(byte typeId) =>
        EnumUtility.GetEnumString(typeId, ScissorTailType.ScissorTail);

    private static string GetLionName(byte typeId) => EnumUtility.GetEnumString(typeId, LionType.Stalker);

    private static string GetThanatosName(byte typeId) => EnumUtility.GetEnumString(typeId, ThanatosType.Thanatos);

    private static string GetTyrantName(byte typeId) => EnumUtility.GetEnumString(typeId, TyrantType.Tyrant);
}
