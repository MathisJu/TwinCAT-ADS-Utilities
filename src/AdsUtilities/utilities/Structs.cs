using System.Runtime.InteropServices;
using System.Text;

namespace AdsUtilities.Structs
{
    internal static class Converter
    {
        internal static T MarshalToStructure<T>(Memory<byte> memory)
        {
            var handle = GCHandle.Alloc(memory.ToArray(), GCHandleType.Pinned);
            T result;
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                result = (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            catch
            {
                result = Activator.CreateInstance<T>();
            }
            finally
            {
                handle.Free();
            }
            return result;
        }

        internal static byte[] StructureToByteArray<T>(T structure) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] byteArray = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure, ptr, false);
                Marshal.Copy(ptr, byteArray, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return byteArray;
        }
    }

    // Struct that resembles the byte stream returned from a file info request to the system service
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct FileInfoByteMapped
    {
        public ushort hFile;
        public ushort reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] dwFileAttributes;
        public uint creationTimeLow;
        public uint creationTimeHigh;
        public uint lastAccessTimeLow;
        public uint lastAccessTimeHigh;
        public uint lastWriteTimeLow;
        public uint lastWriteTimeHigh;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint nReserved0;
        public uint nReserved1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
        public byte[] sFileName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] sAlternativeFileName;

        public static explicit operator FileInfoDetails(FileInfoByteMapped fileEntry)
        {
            int fileNameLength = Array.IndexOf(fileEntry.sFileName, (byte)0);
            int altFileNameLength = Array.IndexOf(fileEntry.sAlternativeFileName, (byte)0);
            FileInfoDetails info = new()
            {
                creationTime = DateTime.FromFileTime((long)fileEntry.creationTimeHigh << 32 | fileEntry.creationTimeLow),
                lastAccessTime = DateTime.FromFileTime((long)fileEntry.lastAccessTimeHigh << 32 | fileEntry.lastAccessTimeLow),
                lastWriteTime = DateTime.FromFileTime((long)fileEntry.lastWriteTimeHigh << 32 | fileEntry.lastWriteTimeLow),
                fileSize = (long)fileEntry.nFileSizeHigh << 32 | fileEntry.nFileSizeLow,
                fileName = Encoding.UTF8.GetString(fileEntry.sFileName.Take(fileNameLength).ToArray()),
                alternativeFileName = Encoding.UTF8.GetString(fileEntry.sAlternativeFileName.Take(altFileNameLength).ToArray()),
                isReadOnly = (fileEntry.dwFileAttributes[0] & (1 << 0)) != 0,
                isHidden = (fileEntry.dwFileAttributes[0] & (1 << 1)) != 0,
                isSystemFile = (fileEntry.dwFileAttributes[0] & (1 << 2)) != 0,
                isDirectory = (fileEntry.dwFileAttributes[0] & (1 << 4)) != 0,     // the byte is 0000X000´, so I originally used & 1<<3. This did not work (maybe MSB and LSB swapped). This may need to be done for other parameters as well - ToDo: URGENT
                isCompressed = (fileEntry.dwFileAttributes[1] & (1 << 3)) != 0,
                isEncrypted = (fileEntry.dwFileAttributes[1] & (1 << 6)) != 0,
            };
            return info;
        }
    }

    // Struct that contains file info in a user friendly format
    public struct FileInfoDetails
    {
        // ToDo: Offset of some dwFileAttributes might be off. Others are not implemented yet - should take care of that at some point
        public DateTime creationTime;
        public DateTime lastAccessTime;
        public DateTime lastWriteTime;
        public long fileSize;
        public string fileName;
        public string alternativeFileName;
        public bool isReadOnly;
        public bool isHidden;
        public bool isSystemFile;
        public bool isDirectory;
        public bool isCompressed;
        public bool isEncrypted;
    }

    public struct CpuUsage
    {
        public uint cpuNo;
        public uint utilization;
        public uint systemLatency;
        public uint latencyWarning;
    }

    public struct SystemInfo
    {
        public string TargetType { get; set; }
        public string TargetVersion { get; set; }
        public string TargetLevel { get; set; }
        public string TargetNetId { get; set; }
        public string HardwareModel { get; set; }
        public string HardwareSerialNumber { get; set; }
        public string HardwareCpuVersion { get; set; }
        public string HardwareDate { get; set; }
        public string HardwareCpuArchitecture { get; set; }
        public string OsImageDevice { get; set; }
        public string OsImageVersion { get; set; }
        public string OsImageLevel { get; set; }
        public string OsName { get; set; }
        public string OsVersion { get; set; }
    }

     public struct TargetInfo
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string NetId { get; set; }
        public string OsVersion { get; set; }
        public string Fingerprint { get; set; }
        //public string Comment;
        //public string tcVersion;
        //public bool isRuntime;
        //public string HostName;
    }

    public struct StaticRoutesInfo
    {
        public string NetId { get; set; }
        public string Name { get; set; }
        public string IpAddress { get; set; }
    }

    public struct NetworkInterfaceInfo
    {
        public string guid;
        public string name;
        public string ipAddress;
        public string subnetMask;
        public string defaultGateway;
        public string dhcpServer;
    }

    // this struct resembles the content of the 40 byte payload of the ads write packet that triggers a broadcast ads search
    // reserved / null bytes might be used for flags, ToDo: should test that later on
    internal readonly struct TriggerBroadcastPacket
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private readonly byte[] reserved1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private readonly byte[] broadcastIp;            
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private readonly byte[] nullBytes1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private readonly byte[] reserved2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private readonly byte[] nullBytes2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        private readonly byte[] reserved3;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private readonly byte[] nullBytes3;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private readonly    byte[] localNetId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        private readonly byte[] reserved4;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private readonly byte[] nullBytes4;

        // ToDo: Restructure this
        public TriggerBroadcastPacket(byte[] broadcastAddress, byte[] netId)
        {
            reserved1 = new byte[]{ 2, 0, 191, 3};
            broadcastIp = broadcastAddress;
            nullBytes1 = new byte[8];
            reserved2 = new byte[] { 3, 102, 20, 113 };
            nullBytes2 = new byte[4];
            reserved3 = new byte[] { 1 };
            nullBytes3 = new byte[3];
            localNetId = netId;
            reserved4 = new byte[]{ 16, 39 };   
            nullBytes4 = new byte[4];
        }
    }
    public struct IoDevice
    {
        public string deviceName;
        public uint deviceId;
        public string netId;
        public uint boxCount;
        public List<IoBox> boxes;
    }

    public struct IoBox
    {
        public string name;
        public uint boxId;
        public uint port;
    }

    public struct RouterStatusInfo
    {
        public uint RouterMemoryBytesReserved;
        public uint RouterMemoryBytesAvailable;
        public uint registeredPorts;
        public uint registeredDrivers;
    }
}
