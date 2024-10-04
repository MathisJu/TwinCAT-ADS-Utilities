using AdsUtilities;
using AdsUtilitiesUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using TwinCAT.Ads;

namespace AdsUtilitiesUI
{
    public class StaticRouteStatus : StaticRoutesInfo
    {
        public string DisplayName { get; set; }
        public bool IsOnline { get; set; }
    }

    public class TargetService : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TargetService()
        {
            Task.Run(async () => await Reload_Routes());
        }

        private StaticRouteStatus _currentTarget;

        public StaticRouteStatus CurrentTarget
        {
            get => _currentTarget;
            set
            {
                if (value is null)
                    return;
                if (_currentTarget is null || _currentTarget.NetId != value.NetId)
                {
                    _currentTarget = value;
                    OnTargetChanged?.Invoke(this, _currentTarget);
                    OnPropertyChanged(nameof(CurrentTarget));
                }
            }
        }

        // Verfügbare Targets als ObservableCollection, damit die UI Änderungen automatisch bemerkt
        public ObservableCollection<StaticRouteStatus> AvailableTargets { get; private set; } = new ObservableCollection<StaticRouteStatus>();

        // Event, das ausgelöst wird, wenn sich das Target ändert
        public event EventHandler<StaticRouteStatus> OnTargetChanged;


        // Methode zum Neuladen aller lokal verfügbaren Targets
        public async Task Reload_Routes()
        {
            List<StaticRouteStatus> routes = new();
            StaticRouteStatus localSystem = new()
            {
                NetId = AmsNetId.Local.ToString(),
                Name = "<Local>",
                DisplayName = "<Local>",
                IpAddress = "0.0.0.0",
                IsOnline = true
                
            };
            routes.Add(localSystem);


            routes.AddRange( await LoadAllRoutesAsync(AmsNetId.Local.ToString()));

            Application.Current.Dispatcher.Invoke(() =>
            {
                AvailableTargets.Clear();
                
                foreach (var route in routes)
                {
                    AvailableTargets.Add(route);
                }

                if (AvailableTargets.Count > 0)
                {
                    CurrentTarget = AvailableTargets[0];
                }
            });
            
        }

        public async Task<List<StaticRouteStatus>> LoadAllRoutesAsync(string netId)
        {
            using AdsRoutingClient adsRoutingClient = new();
            adsRoutingClient.Connect(netId);
            List<StaticRoutesInfo> routes = await adsRoutingClient.GetRoutesListAsync();
            ConcurrentBag<StaticRouteStatus> routesStatus = new(); // threadsichere Collection

            // Parallel ausgeführte Schleife
            await Task.WhenAll(routes.Select(async route =>
            {
                bool isOnline = await IsTargetOnline(route.NetId);
                StaticRouteStatus routeStatus = new()
                {
                    NetId = route.NetId,
                    Name = route.Name,
                    IpAddress = route.IpAddress,
                    IsOnline = isOnline,
                    DisplayName = isOnline ? route.Name : route.Name + " (offline)"
                };

                routesStatus.Add(routeStatus); // Thread-sicheres Hinzufügen
            }));

            return routesStatus.ToList();
        }

        public async Task<List<StaticRoutesInfo>> LoadOnlineRoutesAsync(string netId)
        {
            using AdsRoutingClient adsRoutingClient = new();
            adsRoutingClient.Connect(netId);
            List<StaticRoutesInfo> routes = await adsRoutingClient.GetRoutesListAsync();
            List<StaticRoutesInfo> routesOnline = new();
            if (netId == AmsNetId.Local.ToString())
            {
                StaticRoutesInfo localRoute = new()
                {
                    NetId = netId,
                    Name = "<local>",
                    IpAddress = "0.0.0.0"
                };
                routesOnline.Add(localRoute);
            }
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
            client.Timeout = 50;
            client.Connect(netId, 10000);
            bool available = (await client.ReadStateAsync(new CancellationToken())).ErrorCode is AdsErrorCode.NoError;
            client.Disconnect();
            client.Dispose();
            return available;
        }
    }

}
