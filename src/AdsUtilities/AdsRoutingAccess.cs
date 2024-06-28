// Ignore Spelling: ip

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using TwinCAT.Ads;

namespace AdsUtilities
{
    public class AdsRoutingAccess
    {
        private ILogger? _logger;

        private readonly AdsClient adsClient = new();

        private readonly AmsNetId _netId;

        public void ConfigureLogger(ILogger logger)
        {
            _logger = logger;
        } 

        public AdsRoutingAccess(string netId)
        {
            _netId = AmsNetId.Parse(netId);
        }

        public AdsRoutingAccess(AmsNetId netId)
        {
            _netId = netId;
        }

        public void AddRoute(string netIdTarget, string ipAddressTarget, string routeName, string usernameTarget, string passwordTarget, string remoteRouteName)
        {
            AddLocalRouteEntry(netIdTarget, ipAddressTarget, routeName);
            AddRemoteRouteEntry(ipAddressTarget, usernameTarget, passwordTarget, remoteRouteName);
        }

        public void RemoveLocalRouteEntry(string routeName)
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            IWriteRequest deleteRouteReq = RequestFactory.CreateWriteRequest(routeName.Length + 1);
            deleteRouteReq.AddStringUTF8(routeName);

            adsClient.Write(Constants.SystemServiceDelRemote, 0, deleteRouteReq);
            adsClient.Disconnect();
        }

        /// <summary>
        /// This method adds an entry to the local routes list with the specified parameters. For a communication to be established, a corresponding route entry must also be added to the remote routes list
        /// </summary>
        /// <param name="netIdEntry"></param>
        /// <param name="ipAddressEntry"></param>
        /// <param name="routeNameEntry"></param>
        /// <returns>Returns true if ADS call succeeded</returns>
        public void AddLocalRouteEntry(string netIdEntry, string ipAddressEntry, string routeNameEntry) 
        {
            IWriteRequest addRouteRequest = RequestFactory.CreateWriteRequest();
            addRouteRequest.Add(netIdEntry.Split('.').Select(byte.Parse).ToArray());
            addRouteRequest.Add(new byte[] { 1, 0, 32 });   // ToDo: Add to Segments List
            addRouteRequest.Add(new byte[23]);
            addRouteRequest.Add((byte)(ipAddressEntry.Length + 1));
            addRouteRequest.Add(new byte[3]);
            addRouteRequest.Add((byte)(routeNameEntry.Length + 1));
            addRouteRequest.Add(new byte[7]);
            addRouteRequest.AddStringUTF8(ipAddressEntry);
            addRouteRequest.AddStringUTF8(routeNameEntry);

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.Write(Constants.SystemServiceAddRemote, 0, addRouteRequest.GetBytes());
            adsClient.Disconnect();
        }

        /// <summary>
        /// This method adds an entry with parameters of the local system to the routes list of a remote system
        /// </summary>
        /// <param name="ipAddressRemote">IP address of the remote system. The address needs to be in the same address range as a network adapter of the local system</param>
        /// <param name="usernameRemote">Credentials of the remote system</param>
        /// <param name="passwordRemote">Credentials of the remote system</param>
        /// <param name="remoteRouteName">Name of the route on the remote system (usually the device name this method is called on)</param>
        /// <returns>Returns true if ADS call succeeded</returns>
        public void AddRemoteRouteEntry(string ipAddressRemote, string usernameRemote, string passwordRemote, string remoteRouteName) 
        {
            static bool IsIpAddressInRange(string ipAddressStr, string subnetMaskStr, string defaultGatewayStr)
            {
                static long IPAddressToLong(IPAddress ip)
                {
                    byte[] bytes = ip.GetAddressBytes();
                    long value = 0;
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        value |= (long)bytes[i] << (8 * (3 - i));
                    }
                    return value;
                }

                // convert inputs to ip address objects
                IPAddress ipAddress = IPAddress.Parse(ipAddressStr);
                IPAddress subnetMask = IPAddress.Parse(subnetMaskStr);
                IPAddress defaultGateway = IPAddress.Parse(defaultGatewayStr);

                // Get network address
                byte[] ipAdressBytes = ipAddress.GetAddressBytes();
                byte[] subnetMaskBytes = subnetMask.GetAddressBytes();
                byte[] networkAddressBytes = new byte[ipAdressBytes.Length];
                for (int i = 0; i < networkAddressBytes.Length; i++)
                {
                    networkAddressBytes[i] = (byte)(ipAdressBytes[i] & subnetMaskBytes[i]);
                }
                IPAddress networkAddress = new (networkAddressBytes);

                // Get broadcast address
                byte[] broadcastAddressBytes = new byte[ipAdressBytes.Length];
                for (int i = 0; i < broadcastAddressBytes.Length; i++)
                {
                    broadcastAddressBytes[i] = (byte)(networkAddressBytes[i] | ~subnetMaskBytes[i]);
                }
                IPAddress broadcastAddress = new(broadcastAddressBytes);

                // Check if default gateway is in range
                long ipAddrNumeric = IPAddressToLong(ipAddress);
                long networkAddrNumeric = IPAddressToLong(networkAddress);
                long broadcastAddrNumeric = IPAddressToLong(broadcastAddress);
                return ipAddrNumeric >= networkAddrNumeric && ipAddrNumeric <= broadcastAddrNumeric;
            }

            
            if (!IPAddress.TryParse(ipAddressRemote, out IPAddress? ipBytes))
            {
                _logger?.LogError("Could not add a route entry on remote system because the provided IP address is invalid");
                return;
            }

            IWriteRequest addRouteRequest = RequestFactory.CreateWriteRequest();
            addRouteRequest.Add(new byte[]{ 2, 0, 191, 03 });   // ToDo: Check these write entries, constants might be wrong, ToDo: Add to Segments list
            addRouteRequest.Add(ipBytes.GetAddressBytes());
            addRouteRequest.Add(new byte[8]);
            addRouteRequest.Add(Segments.HEADER);
            addRouteRequest.Add(new byte[4]);
            addRouteRequest.Add(Segments.REQUEST_ADDROUTE);
            addRouteRequest.Add(_netId.ToBytes());
            addRouteRequest.Add(Segments.PORT);
            addRouteRequest.Add(Segments.ROUTETYPE_STATIC);
            byte[] Segment_ROUTENAME_LENGTH = Segments.ROUTENAME_L;
            Segment_ROUTENAME_LENGTH[2] = (byte)(remoteRouteName.Length + 1);
            addRouteRequest.Add(Segment_ROUTENAME_LENGTH);
            addRouteRequest.AddStringUTF8(remoteRouteName);
            addRouteRequest.Add(Segments.AMSNETID_L);
            addRouteRequest.Add(_netId.ToBytes());
            byte[] Segment_USERNAME_LENGTH = Segments.USERNAME_L;
            Segment_USERNAME_LENGTH[2] = (byte)(usernameRemote.Length + 1);
            addRouteRequest.Add(Segment_USERNAME_LENGTH);
            addRouteRequest.AddStringUTF8(usernameRemote);
            byte[] Segment_PASSWORD_LENGTH = Segments.PASSWORD_L;
            Segment_PASSWORD_LENGTH[2] = (byte)(passwordRemote.Length + 1);
            addRouteRequest.Add(Segment_PASSWORD_LENGTH);
            addRouteRequest.AddStringUTF8(passwordRemote);


            List<Structs.NetworkInterfaceInfo> nicsInfo = GetNetworkInterfaces();
            bool foundNwAdapterInRange = false;
            bool rwSuccessAny = false;
            foreach (var nic in nicsInfo)
            {
                if(IsIpAddressInRange(nic.ipAddress, nic.subnetMask, nic.defaultGateway))   // look for a NIC with an IP that's in the same address range as the remote system and use this IP for the remote route entry
                {
                    foundNwAdapterInRange = true;

                    byte[] Segment_IPADDRESS_LENGTH = Segments.LOCALHOST_L;
                    Segment_IPADDRESS_LENGTH[2] = (byte)(nic.ipAddress.Length + 1);
                    addRouteRequest.Add(Segment_IPADDRESS_LENGTH);
                    addRouteRequest.AddStringUTF8(nic.ipAddress);

                    byte[] rdBfr = new byte[2048];

                    adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
                    AdsErrorCode rwError = adsClient.TryReadWrite(Constants.SystemServiceBroadcast, 0, rdBfr, addRouteRequest.GetBytes(), out _);    
                    adsClient.Disconnect();

                    if (rwError == AdsErrorCode.NoError)
                        rwSuccessAny = true;

                    break;
                }
            }
            if (!foundNwAdapterInRange)
                _logger?.LogError("Error occurred when trying to add a remote route entry. No network adapter on the local system matched address range of the provided IP address. Please check the provided IP or the DHCP settings / the static IP on the remote system");
            if (!rwSuccessAny)
                _logger?.LogError("ADS call to add remote route entry failed on all network adapters");
        }

        /// <summary>
        /// Adds an ADS route to a remote system using a third system as a gateway. An existing route between gateway-system and sub-route-system is required.
        /// </summary>
        /// <param name="netIdGateway"></param>
        /// <param name="netIdSubRoute"></param>
        /// <param name="nameSubRoute"></param>
        public void AddSubRoute(string netIdGateway, string netIdSubRoute, string nameSubRoute)
        {
            var routesGateway = new AdsRoutingAccess(netIdGateway).GetRoutesList();     // Check if there is a route between gateway and sub-route-system - mandatory for sub-route to work
            if (!routesGateway.Where(r => r.NetId == netIdSubRoute).Any())
            {
                _logger?.LogCritical("Sub-Route to {netIdSub} with gateway {netIdGate} could not be added because there is no existing route entry to the sub-route on the gateway. Make sure to add the remote route first.", netIdSubRoute, netIdGateway);
                return;
            }
            string staticRoutesPath = GetTwinCatDirectory() + "/3.1/Target/StaticRoutes.xml";
            AdsFileAccess routesEditor = new(_netId);
            byte[] staticRoutesContent = routesEditor.FileRead(staticRoutesPath, false);
            string staticRoutesString = Encoding.UTF8.GetString(staticRoutesContent.Where(c => c is not 0).ToArray());
            XDocument routesXml = XDocument.Parse(staticRoutesString);

            var gatewayEntry = routesXml.Descendants("Route")
                                 .FirstOrDefault(route => route.Element("NetId")?.Value == netIdGateway);

            if (gatewayEntry != null)
            {
                XElement subRoute = new XElement("SubRoute",
                                    new XElement("Name", nameSubRoute),
                                    new XElement("NetId", netIdSubRoute));

                gatewayEntry.Add(subRoute);
                
                routesEditor.FileWrite(staticRoutesPath, Encoding.UTF8.GetBytes(routesXml.ToString()), false);
                _logger?.LogInformation("Sub-Route to {netIdSub} with gateway {netIdGate} was successfully added.", netIdSubRoute, netIdGateway);
            }
            else
            {
                _logger?.LogError("Sub-Route to {netIdSub} with gateway {netIdGate} could not be added due to a parsing error with the StaticRoutesXml of {netIdLocal}", netIdSubRoute, netIdGateway, _netId);
            }
        }

        public void AddAdsMqttRoute(string brokerAddress, uint brokerPort, bool unidirectional = false, string topic = default, uint qualityOfService = default, string user = default, string password = default)
        {
            string staticRoutesPath = GetTwinCatDirectory() + "/3.1/Target/StaticRoutes.xml";
            AdsFileAccess routesEditor = new(_netId);
            byte[] staticRoutesContent = routesEditor.FileRead(staticRoutesPath, false);
            string staticRoutesString = Encoding.UTF8.GetString(staticRoutesContent.Where(c => c is not 0).ToArray());
            XDocument routesXml = XDocument.Parse(staticRoutesString);

            var connectionsEntry = routesXml.Element("RemoteConnections");

            XElement mqttRoute = new XElement("Mqtt",
            new XElement("Address", brokerAddress, new XAttribute("Port", $"{brokerPort}")));

            if (unidirectional)
                mqttRoute.Add(new XAttribute("Unidirectional", "true"));
            if (!string.IsNullOrEmpty(topic))
                mqttRoute.Add(new XElement("Topic", topic));
            if (qualityOfService > 0)
                mqttRoute.Add(new XElement("QoS", qualityOfService));
            if (!string.IsNullOrEmpty(user))
                mqttRoute.Add(new XElement("User", user));
            if (!string.IsNullOrEmpty(password))
                mqttRoute.Add(new XElement("Pwd", password));

            connectionsEntry.Add(mqttRoute);

            routesEditor.FileWrite(staticRoutesPath, Encoding.UTF8.GetBytes(routesXml.ToString()), false);
        }

        public void AddAdsMqttRoute(string brokerAddress, uint brokerPort, AdsMqttTlsParameters tlsParameters, bool unidirectional = false, string topic = default, uint qualityOfService = default, string user = default, string password = default)
        {

        }

        public struct AdsMqttTlsParameters
        {
            public string CertificateAuthority;
            public string? ClientCert = default;
            public string? Key = default;
            public string? KeyPassword = default;
            public string? Version = default;
            public List<string>? Cipher = default;
            public string? RevocationList = default;

            public AdsMqttTlsParameters(string CertificateAuthority) {
                this.CertificateAuthority = CertificateAuthority;
            }
        }

        private string GetTwinCatDirectory()
        {
            return "C:/TwinCAT";    // ToDo: Return installation path dynamically. There may be an ADS command to read this. Otherwise use AdsSystemAccess to read OS and implement logic for each OS (e.g. registry / env variables for windows)
        }

        public Structs.SystemInfo GetSystemInfo()
        {
            byte[] rdBfr = new byte[2048];

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.Read(Constants.SystemServiceTcSystemInfo, 1, rdBfr);
            adsClient.Disconnect();

            string sysInfo = Encoding.UTF8.GetString(rdBfr);
            if (string.IsNullOrEmpty(sysInfo)) return new Structs.SystemInfo();

            XmlDocument xmlDoc = new();
            xmlDoc.LoadXml(sysInfo);
            Structs.SystemInfo devInfo = new()
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
                    _logger?.LogWarning("Encountered an error trying to access the property {xpath} from the system with netId {netId}", xpath, _netId);
                    return string.Empty;
                }
            }
        }

        public List<Structs.CpuUsage> GetTcCpuUsage()
        {
            byte[] rdBfr = new byte[2400]; //Read buffer is sufficient for up to 100 CPU Cores (Increase size if needed)

            adsClient.Connect(_netId, (int)Constants.AdsPortR0RTime);
            var NumberofBytes = adsClient.Read(1, 15, rdBfr); //Retrieve new Data       ToDo: add idxGrp and idxOffs to constants
            adsClient.Disconnect();

            List<Structs.CpuUsage> cpuInfo = new();
            for (int i = 0; i < NumberofBytes / 24; i++)
            {
                int baseIdx = i * 24;
                int latencyWarning = (rdBfr[13 + baseIdx] << 8) + rdBfr[baseIdx + 12];
                int coreLatency = (rdBfr[baseIdx + 9] << 8) + rdBfr[baseIdx + 8];
                cpuInfo.Add(new Structs.CpuUsage { cpuNo = rdBfr[baseIdx], latencyWarning = (uint)latencyWarning, systemLatency = (uint)coreLatency, utilization = rdBfr[baseIdx + 16] });
            }
            return cpuInfo;
        }

        public List<Structs.StaticRoutesInfo> GetRoutesList()
        {
            // Read ADS routes from target system 
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            List<Structs.StaticRoutesInfo> routesList = new();
            for (uint i = 0; i < 100; i++)
            {
                byte[] rdBfr = new byte[235];
                try
                {
                    adsClient.Read((int)Constants.SystemServiceEnumRemote, i, rdBfr); 
                }
                catch (Exception)   // ToDo: Read how many route entries there are beforehand instead of trying to read until an exception is thrown or specify the handled exception
                {
                    break;
                }

                string netIdRd = String.Join(".", rdBfr.Take(6).Where(b => b != 0).Select(b => b.ToString()));

                int flags = rdBfr[8];       // ToDo: Add interpretation for remaining infos from data stream
                int unknown1 = rdBfr[12];
                int unknown2 = rdBfr[13];
                int unknown3 = rdBfr[32];
                int unknown4 = rdBfr[36];

                int startPosName = Array.IndexOf(rdBfr, (byte)0, 44) + 1;
                startPosName = startPosName == 0 ? 44 : startPosName; // Adjust if no null byte was found

                string ip = Encoding.ASCII.GetString(rdBfr, 44, startPosName - 44 - 1).TrimEnd('\0');

                int nameLength = Array.IndexOf(rdBfr, (byte)0, startPosName) - startPosName;
                nameLength = nameLength < 0 ? 0 : nameLength; // Adjust if no null byte was found

                string name = Encoding.ASCII.GetString(rdBfr, startPosName, nameLength).TrimEnd('\0');

                Structs.StaticRoutesInfo entry = new()
                {
                    NetId = netIdRd,
                    Name = name,
                    IpAddress = ip
                };
                routesList.Add(entry);
            }
            adsClient.Disconnect();

            return routesList;
        }

        /// <summary>
        /// Reads the fingerprint of a system to which a route already exists
        /// </summary>
        /// <returns>Fingerprint in a string format</returns>
        public  string GetFingerprint()
        {
            byte[] rdBfr = new byte[129];

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.Read(Constants.SystemServiceTcSystemInfo, 9, rdBfr);
            adsClient.Disconnect();

            return Encoding.UTF8.GetString(rdBfr);
        }

        public List<Structs.NetworkInterfaceInfo> GetNetworkInterfaces()
        {
            byte[] readBfr = new byte[4];
            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            adsClient.Read(Constants.SystemServiceIpHelperApi, Constants.IpHelperApiAdaptersInfo, readBfr);
            uint nicBfrSize = BitConverter.ToUInt32(readBfr, 0);  
            byte[] nicBfr = new byte[nicBfrSize];
            adsClient.Read(Constants.SystemServiceIpHelperApi, Constants.IpHelperApiAdaptersInfo, nicBfr);
            adsClient.Disconnect();

            const uint bytesPerNic = 640;             // Info on every NIC takes 640 bytes. There might be a data field in the byte array that contains that size. For now it's statically defined however
            uint numOfNics = nicBfrSize / bytesPerNic;  
            List<Structs.NetworkInterfaceInfo> nicList = new();
            for (int i = 0; i < numOfNics; i++)
            {
                Structs.NetworkInterfaceInfo newNic = new()
                {
                    guid = Encoding.UTF8.GetString(nicBfr[(Index)(i * bytesPerNic + 8)..(Index)(i * bytesPerNic + 267)]).Replace("\0", string.Empty),
                    name = Encoding.UTF8.GetString(nicBfr[(Index)(i * bytesPerNic + 268)..(Index)(i * bytesPerNic + 380)]).Replace("\0", string.Empty),
                    ipAddress = Encoding.UTF8.GetString(nicBfr[(Index)(i * bytesPerNic + 432)..(Index)(i * bytesPerNic + 447)]).Replace("\0", string.Empty),
                    subnetMask = Encoding.UTF8.GetString(nicBfr[(Index)(i * bytesPerNic + 448)..(Index)(i * bytesPerNic + 463)]).Replace("\0", string.Empty),
                    defaultGateway = Encoding.UTF8.GetString(nicBfr[(Index)(i * bytesPerNic + 472)..(Index)(i * bytesPerNic + 487)]).Replace("\0", string.Empty),
                    dhcpServer = Encoding.UTF8.GetString(nicBfr[(Index)(i * bytesPerNic + 512)..(Index)(i * bytesPerNic + 527)]).Replace("\0", string.Empty)
                };
                nicList.Add(newNic);
            }
            return nicList;            
        }

        /// <summary>
        /// Performs a broadcast search on all available network adapters
        /// </summary>
        /// <param name="secondsTimeout">Broadcast timeout</param>
        /// <param name="cancellationToken">Broadcast cancellation</param>
        /// <returns></returns>
        public async Task<List<Structs.TargetInfo>> AdsBroadcastSearchAsync(ushort secondsTimeout = 5, CancellationToken cancellationToken = default)
        {
            List<Structs.NetworkInterfaceInfo> nicsInfo = GetNetworkInterfaces();
            Task<List<Structs.TargetInfo>> taskPerformBroadcast = AdsBroadcastSearchAsync(nicsInfo, secondsTimeout, cancellationToken);
            return await taskPerformBroadcast;
        }

        /// <summary>
        /// Performs a broadcast search on a selected group of network adapters
        /// </summary>
        /// <param name="interfacesToBroadcastOn">List of network adapters to broadcast on</param>
        /// <param name="secondsTimeout">Broadcast timeout</param>
        /// <param name="cancellationToken">Broadcast cancellation</param>
        /// <returns></returns>
        public async Task<List<Structs.TargetInfo>> AdsBroadcastSearchAsync(List<Structs.NetworkInterfaceInfo> interfacesToBroadcastOn, ushort secondsTimeout = 5, CancellationToken cancellationToken = default)
        {
            List<Structs.TargetInfo> broadcastResults = new();

            void RecievedBroadcastResponse(object sender, AdsNotificationEventArgs e)
            {
                broadcastResults.Add(ParseBroadcastReturn(e.Data.ToArray()));
            }

            // Register a notification. The results of the broadcast search will be sent as a response on this notification (one notification per found device)
            adsClient.AdsNotification += RecievedBroadcastResponse;
            NotificationSettings sttngs = new(AdsTransMode.OnChange, 100, 0);

            adsClient.Connect(_netId, (int)Constants.AdsPortSystemService);
            
            uint notiHdl  = adsClient.AddDeviceNotification(Constants.SystemServiceBroadcast, 0, 2048, sttngs, null);   

            foreach (Structs.NetworkInterfaceInfo nic in  interfacesToBroadcastOn)
            {
                if (nic.ipAddress is "0.0.0.0" or null)
                {
                    _logger?.LogInformation("The NIC '{nicName}' has no assigned IP address. The request for an ADS broadcast search was aborted.", nic.name);
                    continue;
                }
                    
                IPAddress gatewayAddress = IPAddress.Parse(nic.defaultGateway);
                IPAddress subnetAddress = IPAddress.Parse(nic.subnetMask);
                byte[] gatewayBytes = gatewayAddress.GetAddressBytes();
                byte[] subnetBytes = subnetAddress.GetAddressBytes();
                byte[] broadcastBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    broadcastBytes[i] = (byte)(gatewayBytes[i] | ~subnetBytes[i]);
                }
                Structs.TriggerBroadcastPacket brPacket = new(broadcastBytes,AmsNetId.Local.ToBytes());
                byte[] wrBfr = Structs.Converter.StructureToByteArray(brPacket);
                try
                {
                    adsClient.Write(Constants.SystemServiceBroadcast, 1, wrBfr);     
                }
                catch (AdsErrorException ex)
                {
                    _logger?.LogInformation("Could not perform an ADS broadcast search on adapter '{nicName}'. The request was aborted due to error: '{error}'.", nic.name, ex.Message);
                }
            }
            try
            {
                await Task.Delay(secondsTimeout * 1000, cancellationToken);
            }           
            catch (TaskCanceledException)
            {
                _logger?.LogInformation("ADS broadcast search was canceled by caller. The List of available TwiCAT systems may be incomplete");
            }
            finally
            {
                // Unregister the Event / Handle
                adsClient.DeleteDeviceNotification(notiHdl);
                adsClient.AdsNotification -= RecievedBroadcastResponse;
                adsClient.Disconnect();
            }
            return broadcastResults;

            Structs.TargetInfo ParseBroadcastReturn(byte[] broadcastReturn)
            {
                const int startingOffset = 4;

                const int startIp = startingOffset;
                const int lenIp = 4;
                byte[] ip = broadcastReturn.Skip(startIp).Take(lenIp).ToArray();
                string ipAddr = $"{ip[0]}.{ip[1]}.{ip[2]}.{ip[3]}";

                // ToDo: Add route for remaining info returned from broadcast including Port, Route Type

                const int startNetId = 28;
                const int lenNetId = 6;
                byte[] netId = broadcastReturn.Skip(startNetId).Take(lenNetId).ToArray();
                string netIdStr = $"{netId[0]}.{netId[1]}.{netId[2]}.{netId[3]}.{netId[4]}.{netId[5]}";

                const int startName = 44;
                int lenName = broadcastReturn[42] - 1;
                byte[] name = broadcastReturn.Skip(startName).Take(lenName).ToArray();
                string deviceName = Encoding.UTF8.GetString(name);

                int startTcType = startName + lenName;
                const int lenTcType = 8;
                byte[] tcType = broadcastReturn.Skip(startTcType).Take(lenTcType).ToArray();    // {4,0,148,0,148,0,0,0} for engineering and {4,0,20,1,20,1,00} for runtime

                int startOsVer = startTcType + lenTcType + 1;
                const int lenOsVer = 12;
                byte[] osVer = broadcastReturn.Skip(startOsVer).Take(lenOsVer).ToArray();
                ushort osKey = (ushort)(osVer[0] * 256 + osVer[4]);
                ushort osBuildKey = (ushort)(osVer[8] * 256 + osVer[9]);
                string os = OS_IDS.ContainsKey(osKey) ? OS_IDS[osKey] : osKey.ToString("X2");
                string osVersionString;
                if (osKey > 0x0C00) //TCBSD has no BuildKey
                    osVersionString = "TwinCAT/BSD" + " (" + osVer[0] + "." + osVer[4] + ")";
                else if (os.Contains("Windows")) //Windows 10 has BulidKey
                    osVersionString = os + " " + (OS_BUILDIDS.ContainsKey(osBuildKey) ? OS_BUILDIDS[osBuildKey] : osBuildKey.ToString("X2"));
                else if (osKey < 0x0500) //TCRTOS
                    osVersionString = "TC/RTOS" + " (" + osVer[0] + "." + osVer[4] + ")";
                else
                    osVersionString = os;

                // TC Type

                // ???? currently unknown parameters

                // ToDo: There should be indicators as to where in the byte array the fingerprint starts. for now we look for the sequence 0-65-0 that indicates the start of the 65 bytes field of the fingerprint
                int startFingerprint = startOsVer + lenOsVer; // + something else
                for (int i = startFingerprint; i < broadcastReturn.Length - 2; i++)
                {
                    if (broadcastReturn[i] == 0 && broadcastReturn[i + 1] == 65 && broadcastReturn[i + 2] == 0)
                    {
                        startFingerprint = i + 3;
                        break;
                    }
                }
                int lenFingerprint = 64;
                byte[] fingerprint = broadcastReturn.Skip(startFingerprint).Take(lenFingerprint).ToArray();
                string fingerprintStr = Encoding.UTF8.GetString(fingerprint);

                return new Structs.TargetInfo { IpAddress = ipAddr, Name = deviceName, NetId = netIdStr, OsVersion = osVersionString, Fingerprint = fingerprintStr };
            }
        }

        /// <summary>
        /// Requests a change of the NetId at next system start. Only works with full Windows OS
        /// </summary>
        /// <param name="netIdNew"></param>
        public void ChangeNetId(string netIdNew)
        {
            string[] partsNetId = netIdNew.Split('.');
            byte[] bytesNetId = new byte[partsNetId.Length];
            for (int i = 0; i < partsNetId.Length; i++)
            {
                if (!byte.TryParse(partsNetId[i], out bytesNetId[i]))
                {
                    _logger?.LogError("One of the segments in the netId is not a valid byte.");
                    return;
                }
            }
            new AdsSystemAccess(_netId).SetRegEntry(@"Software\Beckhoff\TwinCAT3\System", "RequestedAmsNetId", Enums.RegEditTypeCode.REG_BINARY, bytesNetId);
        }

        public Structs.RouterStatusInfo GetRouterStatusInfo()
        {
            IReadRequest readRequest = RequestFactory.CreateReadRequest(32);
            adsClient.Connect(_netId, (int)Constants.AdsPortRouter);
            adsClient.Read(1, 1, readRequest);  // ToDo: Add idx to constants

            Structs.RouterStatusInfo routerInfo = new()
            {
                RouterMemoryBytesReserved = readRequest.ExtractUint32(),
                RouterMemoryBytesAvailable = readRequest.ExtractUint32(),
                registeredPorts = readRequest.ExtractUint32(),
                registeredDrivers = readRequest.ExtractUint32()
            };
            return routerInfo;
        }

        public short GetPlatformLevel()
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortLicenseServer);
            short platformLevel = adsClient.ReadAny<short>(0x01010004, 0x2);  // ToDo: Add idx to constants
            adsClient.Disconnect();

            return platformLevel;
        }

        public string GetSystemId()
        {
            byte[] rdBfr = new byte[16];

            adsClient.Connect(_netId, (int)Constants.AdsPortLicenseServer);           
            adsClient.Read(0x01010004, 0x1, rdBfr);  // ToDo: Add idx to constants
            adsClient.Disconnect();

            return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}-{4:X2}{5:X2}-{6:X2}{7:X2}-{8:X2}{9:X2}-{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}",
                rdBfr[3], rdBfr[2], rdBfr[1], rdBfr[0],
                rdBfr[5], rdBfr[4], rdBfr[7], rdBfr[6],
                rdBfr[8], rdBfr[9], rdBfr[10], rdBfr[11], 
                rdBfr[12], rdBfr[13], rdBfr[14], rdBfr[15]
            );
        }

        public uint GetVolumeNumber()
        {
            adsClient.Connect(_netId, (int)Constants.AdsPortLicenseServer);        
            uint volumeNo = adsClient.ReadAny<uint>(0x01010004, 0x5);  // ToDo: Add idx to constants
            adsClient.Disconnect();

            return volumeNo;
        }

        private static readonly Dictionary<ushort, string> OS_IDS =
            new()
            {
                {0x0700, "Win CE (7.0)"},
                {0x0602, "Win 8/8.1/10"}, //TC2 images and TC3 images up to 4020
                {0x0A00, "Windows"},
                {0x0601, "Win 7"},
                {0x0600, "Win CE (6.0)"},
                {0x0500, "Win CE (5.0)"},
                {0x0501, "Win XP"},
                {0x0009, "RTOS"}
                //{0x0C02, "TwinCAT/BSD (12.2)"}, // 0C = 12, 02 = 2  // Wireshark line 0060 - 9 / 13
                //{0x0D01, "TwinCAT/BSD (13.1)"}, // 0D = 13, 01 = 1 
                //{0x0D02, "TwinCAT/BSD (13.2)"}
            };

        // Windows build versions
        private static readonly Dictionary<ushort, string> OS_BUILDIDS =
            new()
            {
                // all tested with 4022 !
                {0x5D58, "11 (22621) 22H2"},
                {0x654A, "10 (19045) 22H2"},
                {0x644A, "10 (19044) 21H2"},
                {0x634A, "10 (19043) 21H1"},
                {0x624A, "10 (19042) 20H2"},// 4.8.1
                {0x614A, "10 (19041) 2004"},// only up to .NET Framework 4.8
                {0x4447, "10 (18363) 1909"},// only up to .NET Framework 4.8
                {0xBA47, "10 (18362) 1903"},// only up to .NET Framework 4.8
                {0x6345, "10 (17763) 1809"},// only up to .NET Framework 4.8
                {0xEE42, "10 (17134) 1803"},// only up to .NET Framework 4.8
                {0xAB3F, "10 (16299) 1709"},// only up to .NET Framework 4.8
                {0xD73A, "10 (15063) 1703"},// only up to .NET Framework 4.8
                {0x3938, "10 (14393) 1607"},
                {0x5A29, "10 (10586) 1511"},// only up to .NET Framework 4.6.2
                {0x0028, "10 (10240) 1507"} // only up to .NET Framework 4.6.2
            };
    }
}
