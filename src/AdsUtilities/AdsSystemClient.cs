using System.Security;
using System.Text;
using TwinCAT.Ads;

namespace AdsUtilities
{
    public class AdsSystemClient : IDisposable
    {
        public string NetId { get { return _netId.ToString(); } }

        private static readonly AdsClient _adsClient = new();

        private readonly AmsNetId _netId;

        public AdsSystemClient(AmsNetId netId)
        {
            _netId = netId;
        }

        public AdsSystemClient(string netId)
        {
            _netId = AmsNetId.Parse(netId);
        }

        public async Task RebootAsync(uint delaySec = 0, CancellationToken cancel = default) 
        {
            _adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await _adsClient.WriteControlAsync(AdsState.Shutdown, 1, BitConverter.GetBytes(delaySec), cancel);
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
