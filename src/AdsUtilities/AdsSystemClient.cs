using System;
using System.Security;
using System.Text;
using System.Xml;
using TwinCAT.Ads;

namespace AdsUtilities
{
    public class AdsSystemClient : IDisposable
    {
        public string NetId { get { return _netId.ToString(); } }

        private readonly AdsClient _adsClient = new();

        private AmsNetId? _netId;

        public AdsSystemClient()
        {
            
        }

        public bool Connect(string netId)
        {
            _netId = new AmsNetId(netId);
            _adsClient.Connect(_netId, AmsPort.SystemService);
            AdsErrorCode readStateError = _adsClient.TryReadState(out _);
            _adsClient.Disconnect();
            return readStateError == AdsErrorCode.NoError;
        }

        public bool Connect()
        {
            return Connect(AmsNetId.Local.ToString());
        }

        public async Task RebootAsync(uint delaySec = 0, CancellationToken cancel = default) 
        {
            _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            var res = await _adsClient.WriteControlAsync(AdsState.Shutdown, 1, BitConverter.GetBytes(delaySec), cancel);
            _adsClient.Disconnect();
        }

        // ToDo: Redo this. There already is an ads command to enable remote control on CE. Using file access in not necessary
        /*public static void EnableCeRemoteDisplay()
        {
            FileHandler.RenameFile(netId, @"\Hard Disk\RegFiles\CeRemoteDisplay_Disable.reg", @"\Hard Disk\RegFiles\CeRemoteDisplay_Enable.reg");
            Reboot(netId, 0);
        }*/

        public async Task SetRegEntryAsync(string subKey, string valueName, RegEditTypeCode registryTypeCode, byte[] value, CancellationToken cancel)
        {
            WriteRequestHelper setRegRequest = new WriteRequestHelper()
                .AddStringUTF8(subKey)
                .AddStringUTF8(valueName)
                .Add(new byte[] { 0, (byte)registryTypeCode, 0, 0, 0 })
                .Add(value);

            _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await _adsClient.WriteAsync(Constants.AdsIGrpSysServRegHklm, 0, setRegRequest.GetBytes(), cancel);
            _adsClient.Disconnect();
        }

        public async Task<byte[]> QueryRegEntryAsync(string subKey, string valueName, uint byteSize, CancellationToken cancel = default)
        {
            WriteRequestHelper readRegRequest = new WriteRequestHelper()
                .AddStringUTF8(subKey)
                .AddStringUTF8(valueName);

            byte[] readBuffer = new byte[byteSize];

            _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await _adsClient.ReadWriteAsync(Constants.AdsIGrpSysServRegHklm, 0, readBuffer, readRegRequest.GetBytes(), cancel);
            _adsClient.Disconnect();

            return readBuffer;
        }

        public async Task<SystemInfo> GetSystemInfoAsync(CancellationToken cancel = default)
        {
            byte[] rdBfr = new byte[2048];

            _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await _adsClient.ReadAsync(Constants.AdsIGrpSysServTcSystemInfo, 1, rdBfr, cancel);
            _adsClient.Disconnect();

            string sysInfo = Encoding.UTF8.GetString(rdBfr);
            if (string.IsNullOrEmpty(sysInfo)) return new SystemInfo();

            XmlDocument xmlDoc = new();
            xmlDoc.LoadXml(sysInfo);
            SystemInfo devInfo = new()
            {
                TargetType = TryGetValueFromXml(xmlDoc, "//TargetType"),
                TargetVersion = $"{TryGetValueFromXml(xmlDoc, "//TargetVersion/Version")}.{TryGetValueFromXml(xmlDoc, "//TargetVersion/Revision")}.{TryGetValueFromXml(xmlDoc, "//TargetVersion/Build")}",
                TargetLevel = TryGetValueFromXml(xmlDoc, "//TargetFeatures/Level"),
                TargetNetId = TryGetValueFromXml(xmlDoc, "//TargetFeatures/NetId"),
                HardwareModel = TryGetValueFromXml(xmlDoc, "//Hardware/Model"),
                HardwareSerialNumber = TryGetValueFromXml(xmlDoc, "//Hardware/SerialNo"),
                HardwareCpuVersion = TryGetValueFromXml(xmlDoc, "//Hardware/CPUVersion"),
                HardwareDate = TryGetValueFromXml(xmlDoc, "//Hardware/Date"),
                HardwareCpuArchitecture = TryGetValueFromXml(xmlDoc, "//Hardware/CPUArchitecture"),
                OsImageDevice = TryGetValueFromXml(xmlDoc, "//OsImage/ImageDevice"),
                OsImageVersion = TryGetValueFromXml(xmlDoc, "//OsImage/ImageVersion"),
                OsImageLevel = TryGetValueFromXml(xmlDoc, "//OsImage/ImageLevel"),
                OsName = TryGetValueFromXml(xmlDoc, "//OsImage/OsName"),
                OsVersion = TryGetValueFromXml(xmlDoc, "//OsImage/OsVersion")
            };
            return devInfo;

            string TryGetValueFromXml(XmlDocument xmlDoc, string xpath)
            {
                try
                {
                    XmlNode? node = xmlDoc.SelectSingleNode(xpath);
                    return node?.InnerText ?? string.Empty;
                }
                catch (XmlException)
                {
                    //_logger?.LogWarning("Could not read property {xpath} from netId {netId}", xpath, _netId);
                    return string.Empty;
                }
            }
        }

        public async Task<DateTime> GetSystemTimeAsync(CancellationToken cancel = default)
        {
            byte[] rdBfr = new byte[16]; 

            _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            var readResult = await _adsClient.ReadAsync(Constants.AdsIGrpSysServTimeServices, 1, rdBfr, cancel);
            _adsClient.Disconnect();
            return ConvertByteArrayToDateTime(rdBfr);
        }

        private DateTime ConvertByteArrayToDateTime(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length < 16)
                throw new ArgumentException("byte array has to contain 16 elements");

            int year = byteArray[0] + (byteArray[1] << 8);

            int month = byteArray[2];
            int day = byteArray[4];

            int hour = byteArray[8];
            int minute = byteArray[10];
            int second = byteArray[12];

            return new DateTime(year, month, day, hour, minute, second);
        }

        public async Task<List<CpuUsage>> GetTcCpuUsageAsync(CancellationToken cancel = default)
        {
            byte[] rdBfr = new byte[2400]; //Read buffer is sufficient for up to 100 CPU Cores (Increase size if needed)

            _adsClient.Connect(_netId, (int)Constants.AdsPortR0RTime);
            var readResult = await _adsClient.ReadAsync(1, 15, rdBfr, cancel); //Retrieve new Data       ToDo: add idxGrp and idxOffs to constants
            _adsClient.Disconnect();

            List<CpuUsage> cpuInfo = new();
            for (int i = 0; i < readResult.ReadBytes / 24; i++)
            {
                int baseIdx = i * 24;
                int latencyWarning = (rdBfr[13 + baseIdx] << 8) + rdBfr[baseIdx + 12];
                int coreLatency = (rdBfr[baseIdx + 9] << 8) + rdBfr[baseIdx + 8];
                cpuInfo.Add(new CpuUsage { cpuNo = rdBfr[baseIdx], latencyWarning = (uint)latencyWarning, systemLatency = (uint)coreLatency, utilization = rdBfr[baseIdx + 16] });
            }
            return cpuInfo;
        }

        public async Task<RouterStatusInfo> GetRouterStatusInfoAsync(CancellationToken cancel = default)
        {
            ReadRequestHelper readRequest = new(32);
            _adsClient.Connect(_netId, (int)Constants.AdsPortRouter);
            await _adsClient.ReadAsync(1, 1, readRequest, cancel);

            RouterStatusInfo routerInfo = new()
            {
                RouterMemoryBytesReserved = readRequest.ExtractUint32(),
                RouterMemoryBytesAvailable = readRequest.ExtractUint32(),
                registeredPorts = readRequest.ExtractUint32(),
                registeredDrivers = readRequest.ExtractUint32()
            };
            return routerInfo;
        }

        public async Task<short> GetPlatformLevelAsync(CancellationToken cancel = default)
        {
            _adsClient.Connect(_netId, (int)Constants.AdsPortLicenseServer);
            short platformLevel = (await _adsClient.ReadAnyAsync<short>(Constants.AdsIGrpLicenseInfo, 0x2, cancel)).Value;
            _adsClient.Disconnect();

            return platformLevel;
        }

        private async Task<byte[]> GetSystemIdBytesAsync(CancellationToken cancel = default)
        {
            byte[] rdBfr = new byte[16];

            _adsClient.Connect(_netId, (int)Constants.AdsPortLicenseServer);
            await _adsClient.ReadAsync(Constants.AdsIGrpLicenseInfo, 0x1, rdBfr, cancel);
            _adsClient.Disconnect();

            return rdBfr;
        }

        public async Task<Guid> GetSystemIdGuidAsync(CancellationToken cancel = default)
        {
            byte[] sysId = await GetSystemIdBytesAsync(cancel);
            return new Guid(sysId);
        }

        public async Task<string> GetSystemIdStringAsync(CancellationToken cancel = default)
        {
            byte[] sysId = await GetSystemIdBytesAsync(cancel);
            return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}-{4:X2}{5:X2}-{6:X2}{7:X2}-{8:X2}{9:X2}-{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}",
                sysId[3], sysId[2], sysId[1], sysId[0],
                sysId[5], sysId[4], sysId[7], sysId[6],
                sysId[8], sysId[9], sysId[10], sysId[11],
                sysId[12], sysId[13], sysId[14], sysId[15]
            );
        }

        public async Task<uint> GetVolumeNumberAsync(CancellationToken cancel = default)
        {
            _adsClient.Connect(_netId, (int)Constants.AdsPortLicenseServer);
            uint volumeNo = (await _adsClient.ReadAnyAsync<uint>(Constants.AdsIGrpLicenseInfo, 0x5, cancel)).Value;
            _adsClient.Disconnect();

            return volumeNo;
        }

        public void Dispose()
        {
            if (_adsClient.IsConnected)
                _adsClient.Disconnect();
            if (!_adsClient.IsDisposed)
            {
                _adsClient.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }

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
}
