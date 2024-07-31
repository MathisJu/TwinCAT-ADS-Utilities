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

        private readonly AdsClient _adsClient = new();

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

        public async Task<Structs.IoDevice> GetIoDeviceInfoAsync(uint deviceId, CancellationToken cancel = default)
        {
            _adsClient.Connect(_netId, (int)Constants.AdsPortR0Io);
            uint readLen = (await _adsClient.ReadAnyAsync<uint>(Constants.AdsIGrpIoDeviceStateBase + deviceId, Constants.AdsIOffsReadDeviceFullInfo, cancel)).Value;

            ReadRequestHelper readRequest = new((int)readLen);

            await _adsClient.ReadAsync(Constants.AdsIGrpIoDeviceStateBase + deviceId, Constants.AdsIOffsReadDeviceFullInfo, readRequest, cancel);
            _adsClient.Disconnect();

            // Get Master info
            uint dataLen = readRequest.ExtractUint32();
            byte unknown1 = readRequest.ExtractByte();
            readRequest.Skip();
            byte[] unknown2 = readRequest.ExtractBytes(4);
            uint slaveCnt = readRequest.ExtractByte();
            readRequest.Skip();
            string masterNetId = readRequest.ExtractNetId();
            byte[] unknown3 = readRequest.ExtractBytes(2);
            string masterName = readRequest.ExtractStringWithLength();

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
                string slaveName = readRequest.ExtractStringWithLength();
                Structs.IoBox box = new() { name = slaveName, port = port, boxId = id };
                boxes.Add(box);
            }

            Structs.IoDevice ecMaster = new() { deviceId = deviceId, netId = masterNetId, deviceName = masterName, boxes = boxes, boxCount = slaveCnt };
            return ecMaster;
        }

        public async Task<List<Structs.IoDevice>> GetIoDevicesAsync(CancellationToken cancel = default)
        {
            ReadRequestHelper readRequest = new(402);

            _adsClient.Connect(_netId, (int)Constants.AdsPortR0Io);
            await _adsClient.ReadAsync(Constants.AdsIGrpIoDeviceStateBase, Constants.AdsIOffsReadDeviceId, readRequest, cancel);
            _adsClient.Disconnect();

            uint numberOfIoDevices = readRequest.ExtractUint16();
            List<Structs.IoDevice> ioDevices = new();

            for (int i = 0; i < numberOfIoDevices; i++)
            {
                uint id = readRequest.ExtractUint16();
                ioDevices.Add(await GetIoDeviceInfoAsync(id, cancel));
            }               

            return ioDevices;
        }

        public T ReadCoeData<T>(string netId, int ecSlaveAddress, ushort index, ushort subIndex)
        {
            _adsClient.Connect(netId, ecSlaveAddress);
            T value = (T)_adsClient.ReadAny(Constants.AdsIGrpCoe, ((uint)index << 16) | subIndex, typeof(T));
            _adsClient.Disconnect();
            return value;
        }

        public void WriteCoeData(string netId, int ecSlaveAddress, ushort index, ushort subIndex, object value)
        {
            _adsClient.Connect(netId, ecSlaveAddress);
            _adsClient.WriteAny(Constants.AdsIGrpCoe, ((uint)index << 16) | subIndex, value);
            _adsClient.Disconnect();
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
}
