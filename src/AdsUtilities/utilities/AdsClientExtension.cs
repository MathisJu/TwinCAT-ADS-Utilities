using TwinCAT.Ads;

namespace AdsUtilities;

public static class AdsClientExtension
{
    internal static int Read(this AdsClient _adsClient, uint indexGroup, uint indexOffset, ReadRequestHelper readRequest)
    {
        return _adsClient.Read(indexGroup, indexOffset, readRequest.Bytes);
    }

    internal static async Task<ResultRead> ReadAsync(this AdsClient _adsClient, uint indexGroup, uint indexOffset, ReadRequestHelper readRequest, CancellationToken cancel = default)
    {
        return await _adsClient.ReadAsync(indexGroup, indexOffset, readRequest.Bytes, cancel);
    }

    internal static AdsErrorCode TryRead(this AdsClient _adsClient, uint indexGroup, uint indexOffset, ReadRequestHelper readRequest, out int readBytes)
    {
        return _adsClient.TryRead(indexGroup, indexOffset, readRequest.Bytes, out readBytes);
    }
  
    internal static async Task<ResultWrite> WriteAsync(this AdsClient _adsClient, uint indexGroup, uint indexOffset, WriteRequestHelper writeRequest, CancellationToken cancel = default)
    {
        return await _adsClient.WriteAsync(indexGroup, indexOffset, writeRequest.GetBytes(), cancel);
    }

    internal static void Write(this AdsClient _adsClient, uint indexGroup, uint indexOffset, WriteRequestHelper writeRequest)
    {
        _adsClient.Write(indexGroup, indexOffset, writeRequest.GetBytes());
    }
}
