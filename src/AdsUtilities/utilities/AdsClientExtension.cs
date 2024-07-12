﻿using System;
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


        internal static void Write(this AdsClient adsClient, uint indexGroup, uint indexOffset, WriteRequestHelper readRequest)
        {
            adsClient.Write(indexGroup, indexOffset, readRequest.GetBytes());
        }

        internal static AdsErrorCode TryRead(this AdsClient adsClient, uint indexGroup, uint indexOffset, ReadRequestHelper readRequest, out int readBytes)
        {
            return adsClient.TryRead(indexGroup, indexOffset, readRequest.data, out readBytes);
        } 
    }
}
