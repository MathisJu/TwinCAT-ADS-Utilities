using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace AdsUtilities
{
    public class AdsIoClient : IDisposable
    {
        public string NetId { get { return _netId.ToString(); } }

        private readonly AdsClient adsClient = new();

        private readonly AmsNetId _netId;

        private ILogger? _logger;

        public void ConfigureLogger(ILogger logger)
        {
            _logger = logger;
        }

        public AdsIoClient(string netId)
        {
            _netId = AmsNetId.Parse(netId);
        }

        public AdsIoClient(AmsNetId netId)
        {
            _netId = netId;
        }

        public Structs.IoDevice GetIoDeviceInfo(uint deviceId)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortR0Io);
            uint readLen = adsClient.ReadAny<uint>(Constants.AdsIGrpIoDeviceStateBase + deviceId, Constants.AdsIOffsReadDeviceFullInfo);

            IReadRequest readRequest = RequestFactory.CreateReadRequest((int)readLen);

            adsClient.Read(Constants.AdsIGrpIoDeviceStateBase + deviceId, Constants.AdsIOffsReadDeviceFullInfo, readRequest);

            // Get Master info
            uint dataLen = readRequest.ExtractUint32();
            byte unknown1 = readRequest.ExtractByte();
            readRequest.Skip();
            byte[] unknown2 = readRequest.ExtractBytes(4);
            uint slaveCnt = readRequest.ExtractByte();
            readRequest.Skip();
            string masterNetId = readRequest.ExtractNetId();
            byte[] unknown3 = readRequest.ExtractBytes(2);
            string masterName = readRequest.ExtractString();

            // Get slaves info
            List<Structs.IoBox> boxes = new();
            while (!readRequest.IsFullyProcessed())
            {
                byte[] unknown4 = readRequest.ExtractBytes(2);
                uint id = readRequest.ExtractByte();
                readRequest.Skip();
                byte[] unknown5 = readRequest.ExtractBytes(2);
                uint port = readRequest.ExtractUint16();
                string netIdMaster = readRequest.ExtractNetId();
                uint unknown6 = readRequest.ExtractUint16();   // In some cases this is the same as port, in some it is null
                string slaveName = readRequest.ExtractString();
                Structs.IoBox box = new() { name = slaveName, port = port, boxId = id };
                boxes.Add(box);
            }

            Structs.IoDevice ecMaster = new() { deviceId = deviceId, netId = masterNetId, deviceName = masterName, boxes = boxes, boxCount = slaveCnt };
            return ecMaster;
        }

        public List<Structs.IoDevice> GetIoDevices()
        {
            IReadRequest readRequest = RequestFactory.CreateReadRequest(402);

            adsClient.Connect(_netId, (int)Constants.AdsPortR0Io);
            adsClient.Read(Constants.AdsIGrpIoDeviceStateBase, Constants.AdsIOffsReadDeviceId, readRequest);
            adsClient.Disconnect();

            uint numberOfIoDevices = readRequest.ExtractUint16();
            List<Structs.IoDevice> ioDevices = new();

            for (int i = 0; i < numberOfIoDevices; i++)
            {
                uint id = readRequest.ExtractUint16();
                ioDevices.Add(GetIoDeviceInfo(id));
            }               

            return ioDevices;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="netId">The EtherCAT Master's AMS Net ID</param>
        /// <param name="ecSlaveAddress"></param>
        /// <param name="index"></param>
        /// <param name="subIndex"></param>
        /// <returns></returns>
        public T ReadCoeData<T>(string netId, int ecSlaveAddress, ushort index, ushort subIndex)
        {
            adsClient.Connect(netId, ecSlaveAddress);
            T value = (T)adsClient.ReadAny(0xF302, ((uint)index << 16) | subIndex, typeof(T));
            adsClient.Disconnect();
            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="netId">The EtherCAT Master's AMS Net ID</param>
        /// <param name="ecSlaveAddress"></param>
        /// <param name="index"></param>
        /// <param name="subIndex"></param>
        /// <param name="value"></param>
        public void WriteCoeData(string netId, int ecSlaveAddress, ushort index, ushort subIndex, object value)
        {
            adsClient.Connect(netId, ecSlaveAddress);
            adsClient.WriteAny(0xF302, ((uint)index << 16) | subIndex, value);
            adsClient.Disconnect();
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
