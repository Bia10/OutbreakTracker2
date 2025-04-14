namespace OutbreakTracker2.WinInterop.Enums;

[Flags]
public enum ProcessAccessFlags : uint
{
    Terminate               = 0x0001,
    CreateThread            = 0x0002,
    SetSessionId            = 0x0004,
    VmOperation             = 0x0008,
    VmRead                  = 0x0010,
    VmWrite                 = 0x0020,
    DuplicateHandle         = 0x0040,
    CreateProcess           = 0x0080,
    SetQuota                = 0x0100,
    SetInformation          = 0x0200,
    QueryInformation        = 0x0400,
    SuspendResume           = 0x0800,
    QueryLimitedInformation = 0x1000,
    Synchronize             = 0x00100000,
    Delete                  = 0x00010000,
    ReadControl             = 0x00020000,
    WriteDac                = 0x00040000,
    WriteOwner              = 0x00080000,
    StandardRightsRequired  = 0x000F0000,

    AllAccess = StandardRightsRequired | Synchronize | 0xFFFF
}