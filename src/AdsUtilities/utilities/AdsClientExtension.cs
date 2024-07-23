using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace AdsUtilities
{
    public static class AdsClientExtension
    {
        internal static int Read(this AdsClient adsClient, uint indexGroup, uint indexOffset, ReadRequestHelper readRequest)
        {
            return adsClient.Read(indexGroup, indexOffset, readRequest.data);
        }

        internal static async Task<ResultRead> ReadAsync(this AdsClient adsClient, uint indexGroup, uint indexOffset, ReadRequestHelper readRequest, CancellationToken cancel = default)
        {
            return await adsClient.ReadAsync(indexGroup, indexOffset, readRequest.data, cancel);
        }

        internal static AdsErrorCode TryRead(this AdsClient adsClient, uint indexGroup, uint indexOffset, ReadRequestHelper readRequest, out int readBytes)
        {
            return adsClient.TryRead(indexGroup, indexOffset, readRequest.data, out readBytes);
        }

        internal static async Task<AdsErrorCode> TryReadAsync(this AdsClient adsClient, uint indexGroup, uint indexOffset, ReadRequestHelper readRequest, CancellationToken cancel = default)
        {
            return await Task.Run(() => adsClient.TryRead(indexGroup, indexOffset, readRequest.data, out _), cancel);
        }

        internal static async Task<AdsErrorCode> TryWriteAsync(this AdsClient adsClient, uint indexGroup, uint indexOffset, WriteRequestHelper writeRequest, CancellationToken cancel = default)
        {
            return await Task.Run(() => adsClient.TryWrite(indexGroup, indexOffset, writeRequest.GetBytes()), cancel);
        }

        internal static async Task<AdsErrorCode> TryReadWriteAsync(this AdsClient adsClient, uint indexGroup, uint indexOffset, byte[] readBuffer, byte[] writeBuffer, CancellationToken cancel = default)
        {
            return await Task.Run(() => adsClient.TryReadWrite(indexGroup, indexOffset, readBuffer, writeBuffer, out _), cancel);
        }
      
        internal static async Task<ResultWrite> WriteAsync(this AdsClient adsClient, uint indexGroup, uint indexOffset, WriteRequestHelper writeRequest, CancellationToken cancel = default)
        {
            return await adsClient.WriteAsync(indexGroup, indexOffset, writeRequest.GetBytes(), cancel);
        }

        internal static void Write(this AdsClient adsClient, uint indexGroup, uint indexOffset, WriteRequestHelper writeRequest)
        {
            adsClient.Write(indexGroup, indexOffset, writeRequest.GetBytes());
        }
    }
}
