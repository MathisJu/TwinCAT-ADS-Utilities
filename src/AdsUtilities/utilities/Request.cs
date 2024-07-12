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
        public byte[] data { set; get; }
        private int _currentIndex;

        public ReadRequestHelper(byte[] data)
        {
            this.data = data;
            _currentIndex = 0;
        }

        public ReadRequestHelper(int length)
        {
            this.data = new byte[length];
            _currentIndex = 0;
        }

        public void Skip(int length=1)
        {
            _currentIndex += length;
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
            if (_currentIndex < data.Length)
                return false;
            return true;
        }

        public byte ExtractByte()
        {
            if (_currentIndex + sizeof(byte) > data.Length)
            {
                throw new InvalidOperationException("Tried to extract more bytes from array than it contains.");
            }
            return data[_currentIndex++];
        }
        public T ExtractStruct<T>() where T : struct
        {
            int structSize = Marshal.SizeOf(typeof(Structs.FileInfoByteMapped));
            if (_currentIndex + structSize > data.Length)
            {
                throw new InvalidOperationException("Tried to extract more bytes from array than it contains.");
            }
            byte[] structBuffer = data.Skip(_currentIndex).Take(structSize).ToArray();
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
            uint value = BitConverter.ToUInt32(data, _currentIndex);
            _currentIndex += sizeof(uint);
            return value;
        }

        public byte[] ExtractBytes(int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, _currentIndex, result, 0, length);
            _currentIndex += length;
            return result;
        }

        public string ExtractNetId()
        {
            byte[] netIdBytes = ExtractBytes(6);
            return $"{netIdBytes[0]}.{netIdBytes[1]}.{netIdBytes[2]}.{netIdBytes[3]}.{netIdBytes[4]}.{netIdBytes[5]}";
        }

        public string ExtractString()
        {
            int length = data[_currentIndex++];
            _currentIndex += 3; // Skip Null-Bytes
            string result = Encoding.UTF8.GetString(data, _currentIndex, length);
            _currentIndex += length + 2; // Skip double string termination
            return result;
        }

        public ushort ExtractUint16()
        {
            ushort value = BitConverter.ToUInt16(data, _currentIndex);
            _currentIndex += sizeof(ushort);
            return value;
        }
    }

    internal class WriteRequestHelper 
    {
        private List<byte> _requestBytes { get; set; }

        public WriteRequestHelper()
        {
            _requestBytes = new List<byte>();
        }

        public WriteRequestHelper Add(byte data)
        {
            _requestBytes.Add(data);
            return this;
        }

        public WriteRequestHelper Add(byte[] data)
        {
            _requestBytes.AddRange(data);
            return this;
        }

        public WriteRequestHelper AddStringUTF8(string str)
        {
            _requestBytes.AddRange(Encoding.UTF8.GetBytes(str));
            _requestBytes.Add(0);   // add string termination
            return this;
        }
        public WriteRequestHelper AddStringAscii(string str)
        {
            _requestBytes.AddRange(Encoding.ASCII.GetBytes(str));
            _requestBytes.Add(0);   // add string termination
            return this;
        }

        public WriteRequestHelper AddInt(int data)
        {
            _requestBytes.AddRange(BitConverter.GetBytes(data));
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
            _requestBytes.AddRange(structAsBytes);
            return this;
        }

        public WriteRequestHelper TrimEnd(int terminationLength)
        {
            while (_requestBytes.Count > 0 && _requestBytes[_requestBytes.Count - 1] == 0)
            {
                _requestBytes.RemoveAt(_requestBytes.Count - 1);
            }
            if (terminationLength > 0)
            {
                for (int i = 0; i < terminationLength; i++)
                {
                    _requestBytes.Add(0);
                }
            }
            return this;
        }

        public byte[] GetBytes()
        {
            return _requestBytes.ToArray();
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
