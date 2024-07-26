using TwinCAT.Ads;

namespace AdsUtilities
{
    internal static class Constants
    {
        // ADS Ports
        public const uint AdsPortRouter = 1;
        public const uint AdsPortLicenseServer = 30;
        public const uint AdsPortLogger = 100;
        public const uint AdsPortEventLog = 110;
        public const uint AdsPortR0RTime = 200;
        public const uint AdsPortR0Io = 300;
        public const uint AdsPortR0Plc = 800;
        public const uint AdsPortSystemService = 10000;

        // Index groups and offsets for file operations
        public const uint AdsIGrpSysServOpenCreate = 100;
        public const uint AdsIGrpSysServOpenRead = 101;
        public const uint AdsIGrpSysServOpenWrite = 102;
        public const uint AdsIGrpSysServCreateFile = 110;
        public const uint AdsIGrpSysServCloseHandle = 111;
        public const uint AdsIGrpSysServFOpen = 120;
        public const uint AdsIGrpSysServFClose = 121;
        public const uint AdsIGrpSysServFRead = 122;
        public const uint AdsIGrpSysServFWrite = 123;
        public const uint AdsIGrpSysServFSeek = 124;
        public const uint AdsIGrpSysServFTell = 125;
        public const uint AdsIGrpSysServFGets = 126;
        public const uint AdsIGrpSysServFPuts = 127;
        public const uint AdsIGrpSysServFScanF = 128;
        public const uint AdsIGrpSysServFPrintF = 129;
        public const uint AdsIGrpSysServFEof = 130;
        public const uint AdsIGrpSysServFDelete = 131;
        public const uint AdsIGrpSysServFRename = 132;
        public const uint AdsIGrpSysServFFind = 133;
        public const uint AdsIGrpSysServMkDir = 138;
        public const uint AdsIGrpSysServRmDir = 139;

        // Routing related functions
        public const uint AdsIGrpSysServBroadcast = 141;
        public const uint AdsIGrpSysServTcSystemInfo = 700;
        public const uint AdsIGrpSysServAddRemote = 801;
        public const uint AdsIGrpSysServDelRemote = 802;
        public const uint AdsIGrpSysServEnumRemote = 803;
        public const uint AdsIGrpSysServIpHelperApi = 701;
        public const uint AdsIGrpSysServIpHostName = 702;
        public const uint AdsIOffsIpHelperApiAdaptersInfo = 1;
        public const uint AdsIOffsIpHelperApiIpAddrByHostName = 4;

        // NT functions
        public const uint AdsIGrpSysServRegHklm = 200;
        public const uint AdsIGrpSysServSendEmail = 300;
        public const uint AdsIGrpSysServTimeServices = 400;
        public const uint AdsIGrpSysServStartProcess = 500;
        public const uint AdsIGrpSysServChangeNetId = 600;

        // Device info
        public const uint AdsIGrpDeviceData = 0xF100;
        public const uint AdsIOffsDevDataAdsState = 0;
        public const uint AdsIOffsDevDataDevState = 2;

        // Symbol info
        public const uint AdsIGrpSymbolTab = 0;
        public const uint AdsIGrpSymbolName = 1;
        public const uint AdsIGrpSymbolVal = 2;
        public const uint AdsIGrpSymbolHandleByName = 3;
        public const uint AdsIGrpSymbolValByName = 4;
        public const uint AdsIGrpSymbolValByHandle = 5;
        public const uint AdsIGrpSymbolReleaseHandle = 6;
        public const uint AdsIGrpSymbolInfoByName = 7;
        public const uint AdsIGrpSymbolVersion = 8;
        public const uint AdsIGrpSymbolInfoByNameEx = 9;
        public const uint AdsIGrpSymbolDownload = 10;
        public const uint AdsIGrpSymbolUpload = 11;
        public const uint AdsIGrpSymbolUploadInfo = 12;
        public const uint AdsIGrpSymbolNote = 16;

        // IO Operations
        public const uint AdsIGrpIoDeviceStateBase = 0x5000;
        public const uint AdsIOffsReadDeviceId = 1;           
        public const uint AdsIOffsReadDeviceName = 1;         
        public const uint AdsIOffsReadDeviceCount = 2;          
        public const uint AdsIOffsReadDeviceNetId = 5;        
        public const uint AdsIOffsReadDeviceType = 7;
        public const uint AdsIOffsReadDeviceFullInfo = 8;

        // Device Manager Access
        public const uint AdsIGrpCoe = 0xF302;

        // License info
        public const uint AdsIGrpLicenseInfo = 0x01010004;


        // File operation constants for file open mode flags 
        public const uint FOpenModeRead = 1;    // Open for reading. If the file does not exist or cannot be found, the call fails.
        public const uint FOpenModeWrite = 2;   // Opens an empty file for writing. If the file exists, its contents are destroyed.
        public const uint FOpenModeAppend = 4;  // Opens for writing at the end of the file without removing the EOF marker; creates the file if it doesn't exist.
        public const uint FOpenModePlus = 8;    // Opens for reading and writing.
        public const uint FOpenModeBinary = 16; // Open in binary (untranslated) mode.
        public const uint FOpenModeText = 32;   // Open in text (translated) mode.

        // Path constants
        public const uint PathGeneric = 1;      // Search/open/create files in selected/generic folder.
        public const uint PathBootProject = 2;      // Search/open/create files in TwinCAT boot project folder with .wbp extension.
        public const uint PathBootData = 3;     // Reserved for future use.
        public const uint PathBootPath = 4;     // Refers to the TwinCAT/Boot directory without adding an extension.
        public const uint PathUserPath1 = 11;   // Reserved for future use.
    }
}