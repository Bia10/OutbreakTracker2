using OutbreakTracker2.Outbreak.Enums.Enemy;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.UnitTests;

public sealed class EnemyStatusUtilityTests
{
    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsDead_ForStandardEnemyZeroHp()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(slotId: 1, nameId: 49, curHp: 0, maxHp: 1550);

        await Assert.That(status).IsEqualTo("Dead");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsTrue();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsInvincible_ForMegabytesProjectile()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: (byte)EnemyType.Megabytes,
            curHp: 1,
            maxHp: 1
        );

        await Assert.That(status).IsEqualTo("Invincible");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsFalse();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsCurrentHp_ForLiveLicker()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(slotId: 49, nameId: 0, curHp: 1138, maxHp: 1250);

        await Assert.That(status).IsEqualTo("1138");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsFalse();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsEmpty_ForFileTwoEmptySlot()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(slotId: 0, nameId: 0, curHp: 0, maxHp: 0);

        await Assert.That(status).IsEqualTo("Empty");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsFalse();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsInvincible_ForFireWithMaxHpOne()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: (byte)EnemyType.Fire,
            curHp: 0,
            maxHp: 1
        );

        await Assert.That(status).IsEqualTo("Invincible");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsFalse();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsExploded_ForEntityWithMineNameAtZeroHp()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: 49,
            curHp: 0,
            maxHp: 1500,
            entityName: "Test Mine"
        );

        await Assert.That(status).IsEqualTo("Exploded");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsTrue();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsExploded_ForEntityWithExplosiveNameAtZeroHp()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: 49,
            curHp: 0,
            maxHp: 1500,
            entityName: "Explosive Device"
        );

        await Assert.That(status).IsEqualTo("Exploded");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsTrue();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsExploded_ForEntityWithFuelNameAtZeroHp()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: 49,
            curHp: 0,
            maxHp: 1500,
            entityName: "Fuel Tank"
        );

        await Assert.That(status).IsEqualTo("Exploded");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsTrue();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsExploded_ForEntityWithCanisterNameAtZeroHp()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: 49,
            curHp: 0,
            maxHp: 1500,
            entityName: "Canister 01"
        );

        await Assert.That(status).IsEqualTo("Exploded");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsTrue();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsExploded_ForEntityWithLowercaseMineNameAtZeroHp()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: 49,
            curHp: 0,
            maxHp: 1500,
            entityName: "MINE_SPAWN"
        );

        await Assert.That(status).IsEqualTo("Exploded");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsTrue();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsDead_ForNonExplosiveEntityAtZeroHp()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: 49,
            curHp: 0,
            maxHp: 1500,
            entityName: "Standard Enemy"
        );

        await Assert.That(status).IsEqualTo("Dead");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsTrue();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsExploded_ForMineEnumAtZeroHp()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: (byte)EnemyType.Mine,
            curHp: 0,
            maxHp: 1500
        );

        await Assert.That(status).IsEqualTo("Exploded");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsTrue();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsExploded_ForGasolineTankEnumAtZeroHp()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: (byte)EnemyType.GasolineTank,
            curHp: 0,
            maxHp: 1500
        );

        await Assert.That(status).IsEqualTo("Exploded");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsTrue();
    }

    [Test]
    public async Task GetHealthStatusForFileTwo_ReturnsDestroyed_ForMineEnumAtMaxHpOneAndCurHpFfff()
    {
        string status = EnemyStatusUtility.GetHealthStatusForFileTwo(
            slotId: 5,
            nameId: (byte)EnemyType.Mine,
            curHp: 0xffff,
            maxHp: 1
        );

        await Assert.That(status).IsEqualTo("Destroyed");
        await Assert.That(EnemyStatusUtility.IsDeadStatus(status)).IsTrue();
    }
}
