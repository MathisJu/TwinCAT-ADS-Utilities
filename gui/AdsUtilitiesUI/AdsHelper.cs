using AdsUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace AdsUtilitiesUI
{
    class AdsHelper
    {
        public static async Task<bool> IsTargetOnline(string netId)
        {
            AdsClient client = new AdsClient();
            client.Timeout = 25;
            client.Connect(netId, 10000);
            bool available = (await client.ReadStateAsync(new CancellationToken())).ErrorCode is AdsErrorCode.NoError;
            client.Disconnect();
            client.Dispose();
            return available;
        }

        public static async Task<List<StaticRoutesInfo>> LoadOnlineRoutesAsync(string netId)
        {
            using AdsRoutingClient adsRoutingClient = new();
            adsRoutingClient.Connect(netId);
            List<StaticRoutesInfo> routes = await adsRoutingClient.GetRoutesListAsync();
            List<StaticRoutesInfo> routesOnline = new();
            StaticRoutesInfo localSystem = new()
            {
                NetId = AmsNetId.Local.ToString(),
                Name = "<Local>",
                IpAddress = "0.0.0.0"
            };
            routesOnline.Add(localSystem);
            foreach (var route in routes)
            {
                if (await IsTargetOnline(route.NetId))
                {
                    routesOnline.Add(route);
                }
            }

            return routesOnline;
        }
    }
}
