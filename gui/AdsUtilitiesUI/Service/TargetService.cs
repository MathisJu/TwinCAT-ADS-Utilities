using AdsUtilities;
using AdsUtilitiesUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using TwinCAT.Ads;

namespace AdsUtilitiesUI;

public class StaticRouteStatus : StaticRoutesInfo
{
    public string DisplayName { get; set; }
    public bool IsOnline { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var other = (StaticRouteStatus)obj;
        return NetId == other.NetId && Name == other.Name;
    }

    public override int GetHashCode()
    {
        return NetId?.GetHashCode() ?? 0;
    }
}

public class TargetService : INotifyPropertyChanged
{
    public TargetService()
    {
        Task.Run(Reload_Routes);
    }

    private StaticRouteStatus _currentTarget;

    public StaticRouteStatus CurrentTarget
    {
        get => _currentTarget;
        set
        {
            if (value is null)
                return;

            _currentTarget = value;
            OnPropertyChanged(nameof(CurrentTarget));
            OnTargetChanged?.Invoke(this, _currentTarget);
            
        }
    }

    public ObservableCollection<StaticRouteStatus> AvailableTargets { get; private set; } = new ObservableCollection<StaticRouteStatus>();

    public event EventHandler<StaticRouteStatus> OnTargetChanged;

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async Task Reload_Routes()
    {
        StaticRouteStatus? previousTarget = CurrentTarget;

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

            int idx = routes.IndexOf(previousTarget);
            if (idx >= 0)
            {
                CurrentTarget = AvailableTargets[idx];
            }
            else if (AvailableTargets.Count > 0)
            {
                CurrentTarget = AvailableTargets[0];
            }
        });
    }

    public async Task<List<StaticRouteStatus>> LoadAllRoutesAsync(string netId)
    {
        using AdsRoutingClient adsRoutingClient = new();
        bool connected = await adsRoutingClient.Connect(netId);
        if (!connected)
        {
            return [];  // ToDo: Handle connection loss
        }
            
        List<StaticRoutesInfo> routes = await adsRoutingClient.GetRoutesListAsync();
        ConcurrentBag<StaticRouteStatus> routesStatus = new();

        var semaphore = new SemaphoreSlim(10);

        await Task.WhenAll(routes.Select(async route =>
        {
            await semaphore.WaitAsync();
            try
            {
                bool isOnline = await IsTargetOnline(route.NetId);
                string deviceType = string.Empty;
                if (isOnline)
                {
                    using AdsSystemClient systemClient = new();
                    await systemClient.Connect(route.NetId);
                    var deviceInfo = await systemClient.GetSystemInfoAsync();
                    deviceType = deviceInfo.HardwareModel;
                }
                StaticRouteStatus routeStatus = new()
                {
                    NetId = route.NetId,
                    Name = route.Name,
                    IpAddress = route.IpAddress,
                    IsOnline = isOnline,
                    DisplayName = isOnline ? ((deviceType != string.Empty) ? route.Name + $" ({deviceType})" : route.Name) : route.Name + " (offline)"
                };
                routesStatus.Add(routeStatus);
            }
            finally
            {
                semaphore.Release();
            }
        }));

        var routesStatusList = routesStatus.ToList();
        routesStatusList.Sort((x, y) => string.Compare(x.Name, y.Name));
        return routesStatusList;
    }

    public async Task<bool> IsTargetOnline(string netId)
    {
        using AdsClient client = new ();
        client.Timeout = 100;
        client.Connect(netId, 10000);
        bool available = (await client.ReadStateAsync(default)).ErrorCode is AdsErrorCode.NoError;
        return available;
    }
}
