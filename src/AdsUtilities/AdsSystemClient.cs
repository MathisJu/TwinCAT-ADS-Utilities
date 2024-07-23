using System.Security;
using System.Text;
using TwinCAT.Ads;

namespace AdsUtilities
{
    public class AdsSystemClient : IDisposable
    {
        public string NetId { get { return _netId.ToString(); } }

        private static readonly AdsClient adsClient = new();

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
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.WriteControlAsync(AdsState.Shutdown, 1, BitConverter.GetBytes(delaySec), cancel);
            adsClient.Disconnect();
        }

        // ToDo: Redo this. There already is an ads command to enable remote control on CE. Using file access in not necessary
        /*public static void EnableCeRemoteDisplay()
        {
            FileHandler.RenameFile(netId, @"\Hard Disk\RegFiles\CeRemoteDisplay_Disable.reg", @"\Hard Disk\RegFiles\CeRemoteDisplay_Enable.reg");
            Reboot(netId, 0);
        }*/

        public async Task SetRegEntryAsync(string subKey, string valueName, Enums.RegEditTypeCode registryTypeCode, byte[] value, CancellationToken cancel)
        {
            WriteRequestHelper setRegRequest = new WriteRequestHelper()
                .AddStringUTF8(subKey)
                .AddStringUTF8(valueName)
                .Add(new byte[] { 0, (byte)registryTypeCode, 0, 0, 0 })
                .Add(value);

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.WriteAsync(Constants.AdsIGrpSysServRegHklm, 0, setRegRequest.GetBytes(), cancel);
            adsClient.Disconnect();
        }

        public async Task<byte[]> QueryRegEntryAsync(string subKey, string valueName, uint byteSize, CancellationToken cancel = default)
        {
            WriteRequestHelper readRegRequest = new WriteRequestHelper()
                .AddStringUTF8(subKey)
                .AddStringUTF8(valueName);

            byte[] readBuffer = new byte[byteSize];

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            await adsClient.ReadWriteAsync(Constants.AdsIGrpSysServRegHklm, 0, readBuffer, readRegRequest.GetBytes(), cancel);
            adsClient.Disconnect();

            return readBuffer;
        }

        public void Dispose()
        {
            if (adsClient.IsConnected)
                adsClient.Disconnect();
            if (!adsClient.IsDisposed)
            {
                adsClient.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
