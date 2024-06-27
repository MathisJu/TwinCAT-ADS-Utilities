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
        internal static void Read(this AdsClient adsClient, uint indexGroup, uint indexOffset, IReadRequest readRequest)
        {
            adsClient.Read(indexGroup, indexOffset, readRequest.data);
        }
        internal static void Write(this AdsClient adsClient, uint indexGroup, uint indexOffset, IWriteRequest readRequest)
        {
            adsClient.Write(indexGroup, indexOffset, readRequest.GetBytes());
        }
    }
}
