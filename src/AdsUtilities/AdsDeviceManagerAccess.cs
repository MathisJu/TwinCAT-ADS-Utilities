using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;

namespace AdsUtilities
{
    public class AdsDeviceManagerAccess
    {
        public string NetId { get { return _netId.ToString(); } }

        private readonly Dictionary<ushort, uint> Areas;

        private readonly AdsClient adsClient = new();

        private readonly AmsNetId _netId;

        public AdsDeviceManagerAccess(AmsNetId netId)
        {
            _netId = netId;
            Areas = ReadAvailableAreas();
        }

        public AdsDeviceManagerAccess(string netId) 
        {
            _netId = AmsNetId.Parse(netId);
            Areas = ReadAvailableAreas();
        }

        private Dictionary<ushort, uint> ReadAvailableAreas()
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            ushort mdpModuleCount = (ushort)adsClient.ReadAny(Constants.AdsIGrpCoe,     // CoE = CAN over EtherCAT (profile definition)
                                                        0xF0200000, // index to get module ID List - Flag and Subindex 0
                                                        typeof(ushort) // first short in list is the count of items 
                                                        );

            Dictionary<ushort, uint> mdp = new();

            for (int i = 0; i < mdpModuleCount + 1; i++)
            {
                // Composition of the MDPModule number and read the numbers of modules
                uint mdpModule = (uint)adsClient.ReadAny(Constants.AdsIGrpCoe, (uint)(0xF0200000 + i), typeof(uint)); // get module ID List at subindex i

                // Composition of the Type and ID
                // do &-Operation with 0xFFFF0000 and shift 16 bit to get the type from the high word
                ushort mdpType = (ushort)((mdpModule & 0xFFFF0000) >> 16);

                if (!mdp.ContainsKey(mdpType))
                {
                    mdp.Add(mdpType, mdpModule);
                }
            }

            adsClient.Disconnect();

            return mdp;
        }

        public void SetAutoGenCert(bool value)      // ToDo: Add wrappers for more device manager functions
        {
            SetDeviceManValue((ushort)Enums.MdpModule.MISCELLANEOUS, 0x8001, 6, value);
        }

        public bool GetAutoGenCert()
        {
            return GetDeviceManValue<bool>((ushort)Enums.MdpModule.MISCELLANEOUS, 0x8001, 6);
        }

        private void SetDeviceManValue(ushort areaNumber, uint table, uint subidx, object value)
        {
            if (Areas.ContainsKey(areaNumber))
            {
                uint mdpAddress = GetMdpAddress(areaNumber, table, subidx); // MDP address of parameter -> ADS idx offset
                adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
                adsClient.WriteAny(Constants.AdsIGrpCoe, mdpAddress, value);
                adsClient.Disconnect();
            }
        }

        private T GetDeviceManValue<T>(ushort areaNumber, uint table, uint subidx)
        {
            if(Areas.ContainsKey(areaNumber))
            {
                uint mdpAddress = GetMdpAddress(areaNumber, table, subidx); // MDP address of parameter -> ADS idx offset
                adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
                T res = (T)adsClient.ReadAny(Constants.AdsIGrpCoe, mdpAddress, typeof(T));
                adsClient.Disconnect();
                return res;
            }
            return default;
        }

        private uint GetMdpAddress(ushort areaNumber, uint table, uint subidx)
        {
            ushort mdpId = (ushort)(Areas[areaNumber] & 0x0000FFFF);    // get the dynamically set MDP-ID for the MDP table containing the parameter to read/write. The ID is the low word part of the area number
            uint mdpAddr = (uint)(mdpId << 20) | (table << 16) | subidx;    // combine table number, MDP-ID and sub-index of the parameter. The result is used as the index offset for ADS access
            return mdpAddr;
        }

    }
}
