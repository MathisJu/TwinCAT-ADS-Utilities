﻿using AdsUtilities;
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
        await adsRoutingClient.Connect(netId);
        List<StaticRoutesInfo> routes = await adsRoutingClient.GetRoutesListAsync();
        ConcurrentBag<StaticRouteStatus> routesStatus = new();

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
            routesStatus.Add(routeStatus);
        }));
        return routesStatus.ToList();
    }

    public async Task<List<StaticRoutesInfo>> LoadOnlineRoutesAsync(string netId)
    {
        using AdsRoutingClient adsRoutingClient = new();
        await adsRoutingClient.Connect(netId);
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
