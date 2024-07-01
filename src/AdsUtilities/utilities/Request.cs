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
    internal interface IReadRequest
    {
        byte[] data { set; get; }
        void Skip(int length = 1);
        int GetIndex();
        void SetIndex(int index);
        bool IsFullyProcessed();
        byte ExtractByte();
        T ExtractStruct<T>() where T : struct;
        uint ExtractUint32();
        byte[] ExtractBytes(int length);
        string ExtractNetId();
        string ExtractString();
        ushort ExtractUint16();
    }

    internal class ReadRequest : IReadRequest
    {
        public byte[] data { set; get; }
        private int _currentIndex;

        public ReadRequest(byte[] data)
        {
            this.data = data;
            _currentIndex = 0;
        }

        public ReadRequest(int length)
        {
            this.data = new byte[length];
            _currentIndex = 0;
        }

        public void Skip(int length)
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

    /// <summary>
    /// Simple interface to create ADS write requests. Acts as a wrapper for either an array or a list of bytes
    /// </summary>
    internal interface IWriteRequest
    {
        void Add(byte data);
        void Add(byte[] data);
        void AddStringUTF8(string data);
        void AddStringAscii(string data);
        void AddInt(int data);
        void AddStruct<T>(T data) where T:struct;
        byte[] GetBytes();
        void TrimEnd(int terminationLength);
    }

    internal class DynamicWriteRequest : IWriteRequest
    {
        private List<byte> _requestBytes { get; set; }

        public DynamicWriteRequest()
        {
            _requestBytes = new List<byte>();
        }

        public void Add(byte data)
        {
            _requestBytes.Add(data);
        }

        public void Add(byte[] data)
        {
            _requestBytes.AddRange(data);
        }

        public void AddStringUTF8(string str)
        {
            _requestBytes.AddRange(Encoding.UTF8.GetBytes(str));
            _requestBytes.Add(0);   // add string termination
        }
        public void AddStringAscii(string str)
        {
            _requestBytes.AddRange(Encoding.ASCII.GetBytes(str));
            _requestBytes.Add(0);   // add string termination
        }

        public void AddInt(int data)
        {
            _requestBytes.AddRange(BitConverter.GetBytes(data));
        }

        public void AddStruct<T>(T data) where T : struct
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
        }

        public byte[] GetBytes()
        {
            return _requestBytes.ToArray();
        }

        public void TrimEnd(int terminationLength)
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

        }
    }

    internal class FixedSizeWriteRequest : IWriteRequest
    {
        private byte[] _requestBytes;
        private int _currentIndex;

        public FixedSizeWriteRequest(int size)
        {
            _requestBytes = new byte[size];
            _currentIndex = 0;
        }

        public void Add(byte data)
        {
            if (_currentIndex >= _requestBytes.Length)
            {
                throw new InvalidOperationException("Not enough space in the request that was instanced with a fixed size.");
            }
            _requestBytes[_currentIndex++] = data;
        }

        public void Add(byte[] data)
        {
            if (_currentIndex + data.Length > _requestBytes.Length)
            {
                throw new InvalidOperationException("Not enough space in the request that was instanced with a fixed size.");
            }
            Array.Copy(data, 0, _requestBytes, _currentIndex, data.Length);
            _currentIndex += data.Length;
        }

        public void AddStringUTF8(string str)
        {
            if (_currentIndex + str.Length + 1 > _requestBytes.Length)
            {
                throw new InvalidOperationException("Not enough space in the request that was instanced with a fixed size.");
            }
            Array.Copy(Encoding.UTF8.GetBytes(str), 0, _requestBytes, _currentIndex, str.Length);
            _currentIndex += str.Length;
            _requestBytes[_currentIndex++] = 0;     // add string termination
        }
        public void AddStringAscii(string str)
        {
            if (_currentIndex + str.Length + 1 > _requestBytes.Length)
            {
                throw new InvalidOperationException("Not enough space in the request that was instanced with a fixed size.");
            }
            Array.Copy(Encoding.ASCII.GetBytes(str), 0, _requestBytes, _currentIndex, str.Length);
            _currentIndex += str.Length;
            _requestBytes[_currentIndex++] = 0;     // add string termination
        }

        public void AddInt(int data)
        {
            if (_currentIndex + sizeof(int) > _requestBytes.Length)
            {
                throw new InvalidOperationException("Not enough space in the request that was instanced with a fixed size.");
            }
            BitConverter.GetBytes(data).CopyTo(_requestBytes, _currentIndex);
            _currentIndex += sizeof(int);
        }

        public void AddStruct<T>(T data) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            if (_currentIndex + size > _requestBytes.Length)
            {
                throw new InvalidOperationException("Not enough space in the request that was instanced with a fixed size.");
            }
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
            structAsBytes.CopyTo(_requestBytes, _currentIndex);
            _currentIndex += size;
        }

        public void TrimEnd(int terminationLength = 0)
        {
            int lastIndex = -1;
            for (int i = _requestBytes.Length - 1; i >= 0; i--)
            {
                if (_requestBytes[i] != 0)
                {
                    lastIndex = i;
                    break;
                }
            }

            int newLength = (lastIndex + 1) + terminationLength;
            byte[] newArray = new byte[newLength];

            for (int i = 0; i <= lastIndex; i++)
            {
                newArray[i] = _requestBytes[i];
            }

            for (int i = lastIndex + 1; i < newLength; i++)
            {
                newArray[i] = 0;
            }

            _currentIndex = newLength;
            _requestBytes = newArray;
        }

        public byte[] GetBytes()
        {
            /*if (_currentIndex < _requestBytes.Length)
            {
                // Optionally, resize the array to remove unused space
                byte[] result = new byte[_currentIndex];
                Array.Copy(_requestBytes, result, _currentIndex);
                return result;
            }*/
            return _requestBytes;
        }
    }

    internal static class RequestFactory
    {
        /// <summary>
        /// Create instance of IWriteRequest - wrapper for easier creation of ADS write requests
        /// </summary>
        /// <param name="size">If given a size, the IWriteRequest will use an array - otherwise a list. For better performance, use this param whenever possible</param>
        /// <returns></returns>
        public static IWriteRequest CreateWriteRequest(int? size = null)
        {
            if (size.HasValue)
            {
                return new FixedSizeWriteRequest(size.Value);
            }
            else
            {
                return new DynamicWriteRequest();
            }
        }


        public static IReadRequest CreateReadRequest(int length)
        {
            return new ReadRequest(length);
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
