using AdsUtilities;
using AdsUtilitiesUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TwinCAT.Ads;

namespace AdsUtilitiesUI
{
    public class TargetService
    {
        public TargetService()
        {
            Task.Run(async () => await Reload_Routes());
        }

        private StaticRoutesInfo _currentTarget;

        public StaticRoutesInfo CurrentTarget
        {
            get => _currentTarget;
            set
            {
                if (_currentTarget.NetId == null || _currentTarget.NetId != value.NetId)
                {
                    _currentTarget = value;
                    OnTargetChanged?.Invoke(this, _currentTarget);
                }
            }
        }

        // Verfügbare Targets als ObservableCollection, damit die UI Änderungen automatisch bemerkt
        public ObservableCollection<StaticRoutesInfo> AvailableTargets { get; private set; } = new ObservableCollection<StaticRoutesInfo>();

        // Event, das ausgelöst wird, wenn sich das Target ändert
        public event EventHandler<StaticRoutesInfo> OnTargetChanged;


        // Methode zum Neuladen aller lokal verfügbaren Targets
        public async Task Reload_Routes()
        {
            var routes = await LoadOnlineRoutesAsync(AmsNetId.Local.ToString());

            Application.Current.Dispatcher.Invoke(() =>
            {
                AvailableTargets.Clear();
                
                foreach (var route in routes)
                {
                    AvailableTargets.Add(route);
                }

                if (AvailableTargets.Count > 0 && CurrentTarget.NetId == null)
                {
                    CurrentTarget = AvailableTargets[0];
                }
            });
        }

        public async Task<List<StaticRoutesInfo>> LoadOnlineRoutesAsync(string netId)
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
    }

}
