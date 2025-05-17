using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.UnitTests;

[TestFixture]
public class LobbyAddressTests
{
    private const nint F2Slot1 = 0x628DA0;
    private const nint F2Slot2 = 0x628EFC;
    private const nint F2Slot3 = 0x629058;
    private const nint F2Slot4 = 0x6291B4;
    private const nint F2Slot5 = 0x629310;
    private const nint F2Slot6 = 0x62946C;
    private const nint F2Slot7 = 0x6295C8;
    private const nint F2Slot8 = 0x629724;
    private const nint F2Slot9 = 0x629880;
    private const nint F2Slot10 = 0x6299DC;
    private const nint F2Slot11 = 0x629B38;
    private const nint F2Slot12 = 0x629C94;
    private const nint F2Slot13 = 0x629DF0;
    private const nint F2Slot14 = 0x629F4C;
    private const nint F2Slot15 = 0x62A0A8;
    private const nint F2Slot16 = 0x62A204;
    private const nint F2Slot17 = 0x62A360;
    private const nint F2Slot18 = 0x62A4BC;
    private const nint F2Slot19 = 0x62A618;
    private const nint F2Slot20 = 0x62A774;

    private static nint F2_GetLobbyAddress(nint slotNum)
    {
        return slotNum switch
        {
            0 => F2Slot1,
            1 => F2Slot2,
            2 => F2Slot3,
            3 => F2Slot4,
            4 => F2Slot5,
            5 => F2Slot6,
            6 => F2Slot7,
            7 => F2Slot8,
            8 => F2Slot9,
            9 => F2Slot10,
            10 => F2Slot11,
            11 => F2Slot12,
            12 => F2Slot13,
            13 => F2Slot14,
            14 => F2Slot15,
            15 => F2Slot16,
            16 => F2Slot17,
            17 => F2Slot18,
            18 => F2Slot19,
            19 => F2Slot20,
            _ => -1
        };
    }

    [Test]
    public void GetLobbyAddress_ValidIndices_MatchesHardcodedValues()
    {
        for (int i = 0; i < 20; i++)
        {
            nint expected = F2_GetLobbyAddress(i);
            nint actual = FileTwoPtrs.GetLobbyAddress(i);
            Assert.That(actual, Is.EqualTo(expected), $"Slot index {i} mismatch.");
        }
    }

    [TestCase(-1)]
    [TestCase(20)]
    [TestCase(100)]
    public void GetLobbyAddress_InvalidIndices_ThrowsInvalidOperationException(int invalidIndex)
    {
        Assert.Throws<InvalidOperationException>(() => FileTwoPtrs.GetLobbyAddress(invalidIndex));
    }
}
