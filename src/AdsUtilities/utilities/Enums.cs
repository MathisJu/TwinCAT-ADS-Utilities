namespace AdsUtilities.Enums
{
    //Type code determines the type of a value to be added to the registry
    public enum RegEditTypeCode
    {
        REG_NONE,
        REG_SZ,
        REG_EXPAND_SZ,
        REG_BINARY,
        REG_DWORD,
        REG_DWORD_BIG_ENDIAN,
        REG_LINK,
        REG_MULTI_SZ,
        REG_RESOURCE_LIST,
        REG_FULL_RESOURCE_DESCRIPTOR,
        REG_RESOURCE_REQUIREMENTS_LIST,
        REG_QWORD
    }

    internal enum MdpModule
    {
        NIC = 0x0002,
        TIME = 0x0003,
        USER_MANAGEMENT = 0x0004,
        RAS = 0x0005,
        SMB_SERVER = 0x0007,
        TWINCAT = 0x0008,
        SOFTWARE_VERSIONS = 0x000A,
        CPU = 0x000B,
        MEMORY = 0x000C,
        FIREWALL = 0x000E,
        FILE_SYSTEM_OBJECT = 0x0010,
        DISPLAY_DEVICE = 0x0013,
        ENHANCED_WRITE_FILTER = 0x0014,
        FILE_BASED_WRITE_FILTER = 0x0015,
        OS = 0x0016,
        RAID = 0x0019,
        FAN = 0x001B,
        MAINBOARD = 0x001C,
        DISK = 0x001D,
        UPS = 0x001E,
        PHYSICAL_DRIVE = 0x001F,
        MASS_STORAGE_MONITOR = 0x0020,
        IO = 0x0022,
        MISCELLANEOUS = 0x0100
    }
}
