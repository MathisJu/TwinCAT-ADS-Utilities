using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.Ads;
using TwinCAT.PlcOpen;

namespace AdsUtilities
{
    internal class ReadRequestHelper
    {
        public byte[] Bytes { set; get; }
        private int _currentIndex;

        public ReadRequestHelper(byte[] data)
        {
            Bytes = data;
            _currentIndex = 0;
        }

        public ReadRequestHelper(int length)
        {
            Bytes = new byte[length];
            _currentIndex = 0;
        }

        public void Clear()
        {
            Array.Clear(Bytes, 0, Bytes.Length);
        }

        public void Skip(int length=1)
        {
            _currentIndex += length;
        }

        public int SkipNullBytes()
        {
            if (Bytes[_currentIndex] != 0)
                return 0;
            int bytesSkipped = 0;
            while (Bytes.Length > _currentIndex && Bytes[_currentIndex] == 0)
            {
                _currentIndex++;
                bytesSkipped++;
            }
            return bytesSkipped;
        }

        public int GetIndex()
        {
            return _currentIndex;
        }

        public void SetIndex(int index)
        {
            _currentIndex = index;
        }

        public bool IsFullyProcessed()
        {
            if (_currentIndex < Bytes.Length)
                return false;
            return true;
        }

        public byte ExtractByte()
        {
            if (_currentIndex + sizeof(byte) > Bytes.Length)
            {
                throw new InvalidOperationException("Tried to extract more bytes from array than it contains.");
            }
            return Bytes[_currentIndex++];
        }
        public T ExtractStruct<T>() where T : struct
        {
            int structSize = Marshal.SizeOf(typeof(FileInfoByteMapped));
            if (_currentIndex + structSize > Bytes.Length)
            {
                throw new InvalidOperationException("Tried to extract more bytes from array than it contains.");
            }
            byte[] structBuffer = Bytes.Skip(_currentIndex).Take(structSize).ToArray();
            var handle = GCHandle.Alloc(structBuffer, GCHandleType.Pinned);
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
            _currentIndex += structSize;
            return result;
        }
        public uint ExtractUint32()
        {
            uint value = BitConverter.ToUInt32(Bytes, _currentIndex);
            _currentIndex += sizeof(uint);
            return value;
        }

        public byte[] ExtractBytes(int length)
        {
            byte[] result = new byte[length];
            Array.Copy(Bytes, _currentIndex, result, 0, length);
            _currentIndex += length;
            return result;
        }

        public string ExtractNetId()
        {
            byte[] netIdBytes = ExtractBytes(6);
            return $"{netIdBytes[0]}.{netIdBytes[1]}.{netIdBytes[2]}.{netIdBytes[3]}.{netIdBytes[4]}.{netIdBytes[5]}";
        }

        public string ExtractStringWithLength()
        {
            int length = Bytes[_currentIndex++];
            _currentIndex += 3; // Skip Null-Bytes
            string result = Encoding.UTF8.GetString(Bytes, _currentIndex, length);
            _currentIndex += length + 2; // Skip double string termination
            return result;
        }

        public string ExtractString()
        {
            if (Bytes[_currentIndex]==0)
                return string.Empty;
            int terminationPosition = Array.IndexOf(Bytes, (byte)0, _currentIndex);
            byte[] stringBytes = new byte[terminationPosition - _currentIndex];
            Array.Copy(Bytes, _currentIndex, stringBytes, 0, terminationPosition - _currentIndex);
            _currentIndex = terminationPosition + 1;
            return Encoding.UTF8.GetString(stringBytes);
        }

        public ushort ExtractUint16()
        {
            ushort value = BitConverter.ToUInt16(Bytes, _currentIndex);
            _currentIndex += sizeof(ushort);
            return value;
        }
    }

    internal class WriteRequestHelper 
    {
        private List<byte> RequestBytes { get; set; }

        public WriteRequestHelper()
        {
            RequestBytes = new List<byte>();
        }

        public WriteRequestHelper Add(byte data)
        {
            RequestBytes.Add(data);
            return this;
        }

        public void Clear()
        {
            RequestBytes.Clear();
            RequestBytes = new();
        }

        public WriteRequestHelper Add(byte[] data)
        {
            RequestBytes.AddRange(data);
            return this;
        }

        public WriteRequestHelper AddStringUTF8(string str)
        {
            RequestBytes.AddRange(Encoding.UTF8.GetBytes(str));
            RequestBytes.Add(0);   // add string termination
            return this;
        }
        public WriteRequestHelper AddStringAscii(string str)    // ToDo: Don't remember why I didn't use UTF8 for some parameters. It should work just the same
        {
            RequestBytes.AddRange(Encoding.ASCII.GetBytes(str));
            RequestBytes.Add(0);   // add string termination
            return this;
        }

        public WriteRequestHelper AddInt(int data)
        {
            RequestBytes.AddRange(BitConverter.GetBytes(data));
            return this;
        }

        public WriteRequestHelper AddStruct<T>(T data) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] structAsBytes = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(data, ptr, false);
                Marshal.Copy(ptr, structAsBytes, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            RequestBytes.AddRange(structAsBytes);
            return this;
        }

        public WriteRequestHelper TrimEnd(int terminationLength)
        {
            while (RequestBytes.Count > 0 && RequestBytes[^1] == 0)
            {
                RequestBytes.RemoveAt(RequestBytes.Count - 1);
            }
            if (terminationLength > 0)
            {
                for (int i = 0; i < terminationLength; i++)
                {
                    RequestBytes.Add(0);
                }
            }
            return this;
        }

        public byte[] GetBytes()
        {
            return RequestBytes.ToArray();
        }
    }
        
    internal static class Segments
    {
        public static readonly byte[] HEADER = { 0x03, 0x66, 0x14, 0x71 };
        public static readonly byte[] END = { 0, 0, 0, 0 };
        public static readonly byte[] AMSNETID = { 0, 0, 0, 0, 1, 1 };
        public static readonly byte[] PORT = { 0x10, 0x27 };

        public static readonly byte[] REQUEST_ADDROUTE = { 6, 0, 0, 0 };
        public static readonly byte[] REQUEST_DISCOVER = { 1, 0, 0, 0 };
        public static readonly byte[] ROUTETYPE_TEMP = { 6, 0, 0, 0 };
        public static readonly byte[] ROUTETYPE_STATIC = { 5, 0, 0, 0 };
        public static readonly byte[] TEMPROUTE_TAIL = { 9, 0, 4, 0, 1, 0, 0, 0 };
        public static readonly byte[] ROUTENAME_L = { 0x0c, 0, 0, 0 };
        public static readonly byte[] IPADDRESS_L = { 2, 0, 191, 03 };
        public static readonly byte[] USERNAME_L = { 0x0d, 0, 0, 0 };
        public static readonly byte[] PASSWORD_L = { 2, 0, 0, 0 };
        public static readonly byte[] LOCALHOST_L = { 5, 0, 0, 0 };
        public static readonly byte[] AMSNETID_L = { 7, 0, 6, 0 };

        public static readonly byte[] RESPONSE_ADDROUTE = { 6, 0, 0, 0x80 };
        public static readonly byte[] RESPONSE_DISCOVER = { 1, 0, 0, 0x80 };
        public static readonly byte[] TCATTYPE_ENGINEERING = { 4, 0, 0x94, 0, 0x94, 0, 0, 0 };
        public static readonly byte[] TCATTYPE_RUNTIME = { 4, 0, 0x14, 1, 0x14, 1, 0, 0 };

        public static readonly int L_NAMELENGTH = 4;
        public static readonly int L_OSVERSION = 12;
        public static readonly int L_DESCRIPTIONMARKER = 4;
        public static readonly int L_ROUTEACK = 4;
    }
}
