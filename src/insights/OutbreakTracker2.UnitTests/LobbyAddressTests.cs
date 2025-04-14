using OutbreakTracker2.Outbreak;

namespace OutbreakTracker2.UnitTests;

[TestFixture]
public class LobbyAddressTests
{
    private const nint F2_Slot1 = 0x628DA0;
    private const nint F2_Slot2 = 0x628EFC;
    private const nint F2_Slot3 = 0x629058;
    private const nint F2_Slot4 = 0x6291B4;
    private const nint F2_Slot5 = 0x629310;
    private const nint F2_Slot6 = 0x62946C;
    private const nint F2_Slot7 = 0x6295C8;
    private const nint F2_Slot8 = 0x629724;
    private const nint F2_Slot9 = 0x629880;
    private const nint F2_Slot10 = 0x6299DC;
    private const nint F2_Slot11 = 0x629B38;
    private const nint F2_Slot12 = 0x629C94;
    private const nint F2_Slot13 = 0x629DF0;
    private const nint F2_Slot14 = 0x629F4C;
    private const nint F2_Slot15 = 0x62A0A8;
    private const nint F2_Slot16 = 0x62A204;
    private const nint F2_Slot17 = 0x62A360;
    private const nint F2_Slot18 = 0x62A4BC;
    private const nint F2_Slot19 = 0x62A618;
    private const nint F2_Slot20 = 0x62A774;

    private static nint F2_GetLobbyAddress(nint slotNum)
    {
        return slotNum switch
        {
            0 => F2_Slot1,
            1 => F2_Slot2,
            2 => F2_Slot3,
            3 => F2_Slot4,
            4 => F2_Slot5,
            5 => F2_Slot6,
            6 => F2_Slot7,
            7 => F2_Slot8,
            8 => F2_Slot9,
            9 => F2_Slot10,
            10 => F2_Slot11,
            11 => F2_Slot12,
            12 => F2_Slot13,
            13 => F2_Slot14,
            14 => F2_Slot15,
            15 => F2_Slot16,
            16 => F2_Slot17,
            17 => F2_Slot18,
            18 => F2_Slot19,
            19 => F2_Slot20,
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
