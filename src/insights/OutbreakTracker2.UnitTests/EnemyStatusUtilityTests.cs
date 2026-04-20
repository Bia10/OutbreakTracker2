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
}
