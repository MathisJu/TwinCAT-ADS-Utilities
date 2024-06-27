using System.Security;
using System.Text;
using TwinCAT.Ads;

namespace AdsUtilities
{
    public class AdsSystemAccess
    {
        private static readonly AdsClient adsClient = new();

        private readonly AmsNetId _netId;

        public AdsSystemAccess(AmsNetId netId)
        {
            _netId = netId;
        }

        public AdsSystemAccess(string netId)
        {
            _netId = AmsNetId.Parse(netId);
        }

        public void Reboot(uint delaySec = 0) 
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.WriteControl(new StateInfo(AdsState.Shutdown, 1), BitConverter.GetBytes(delaySec));
            adsClient.Disconnect();
        }

        // ToDo: Redo this. There already is an ads command to enable remote control on CE. Using file access in not necessary
        /*public static void EnableCeRemoteDisplay()
        {
            FileHandler.RenameFile(netId, @"\Hard Disk\RegFiles\CeRemoteDisplay_Disable.reg", @"\Hard Disk\RegFiles\CeRemoteDisplay_Enable.reg");
            Reboot(netId, 0);
        }*/

        /// <summary>
        /// Modifies an existing Reg entry or creates a new one
        /// </summary>
        /// <param name="subKey">Sub-Key under HKLM</param>
        /// <param name="valueName">The entry to edit/ create</param>
        /// <param name="registryTypeCode">Type of the entry</param>
        /// <param name="value"></param>
        /// /// <returns>Returns true if Reg entry was set successfully</returns>
        public void SetRegEntry(string subKey, string valueName, Enums.RegEditTypeCode registryTypeCode, byte[] value)
        {
            IWriteRequest setRegRequest = RequestFactory.CreateWriteRequest(subKey.Length + valueName.Length + value.Length + 6);
            setRegRequest.AddStringUTF8(subKey);
            setRegRequest.AddStringUTF8(valueName);
            setRegRequest.Add(new byte[] { 0, (byte)registryTypeCode, 0, 0, 0 });
            setRegRequest.Add(value);

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.Write(Constants.SystemServiceRegHkeyLocalMachine, 0, setRegRequest.GetBytes());
            adsClient.Disconnect();
        }

        /// <summary>
        /// Reads the value of a Reg entry
        /// </summary>
        /// <param name="subKey">Sub-Key under HKLM</param>
        /// <param name="valueName">The entry whose value to read</param>
        /// <param name="netId"></param>
        /// <param name="data">Ref to a buffer for the value to read</param>
        /// <returns>Returns true if Reg entry was read successfully</returns>
        public void QueryRegEntry(string subKey, string valueName, ref byte[] data)
        {
            IWriteRequest readRegRequest = RequestFactory.CreateWriteRequest(subKey.Length + valueName.Length + 2);
            readRegRequest.AddStringUTF8(subKey);
            readRegRequest.AddStringUTF8(valueName);

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.ReadWrite(Constants.SystemServiceRegHkeyLocalMachine, 0, data, readRegRequest.GetBytes());
            adsClient.Disconnect();
        }
    }
}
