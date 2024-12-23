using AdsUtilities;
using AdsUtilitiesUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;

namespace AdsUtilitiesUI;

public class AdsRoutingViewModel : ViewModelTargetAccessPage
{
    public AdsRoutingViewModel(TargetService targetService, ILoggerService loggerService)
    {
        AddRouteSelection = new();

        _TargetService = targetService;
        InitTargetAccess(_TargetService);
        _TargetService.OnTargetChanged += LoadNetworkAdapters;
        _TargetService.OnTargetChanged += UpdateRemoteName;

        _LoggerService = (LoggerService)loggerService;

        BroadcastCommand = new(Broadcast);
        AddRouteCommand = new(AddRoute);
        SearchByIpOrNameCommand = new(SearchByIpOrName);

        NetworkAdapterPairs = new ObservableCollection<NetworkAdapterPair>();
        TargetInfoList = new ObservableCollection<TargetInfo>();
    }


    public List<NetworkAdapterItem> NetworkAdapters;
    public ObservableCollection<NetworkAdapterPair> NetworkAdapterPairs { get; set; }


    public ObservableCollection<TargetInfo> TargetInfoList { get; set; }

    private TargetInfo _TargetListSelection;
    public TargetInfo TargetListSelection
    {
        get => _TargetListSelection;
        set
        {
            if (_TargetListSelection.Name != value.Name || _TargetListSelection.NetId != value.NetId || _TargetListSelection.IpAddress != value.IpAddress)
            {
                _TargetListSelection = value;
                OnPropertyChanged();
                AddRouteSelection.HostName = value.Name;
                AddRouteSelection.Name = value.Name;
                AddRouteSelection.NetId = value.NetId;
                AddRouteSelection.IpAddress = value.IpAddress;

                OnPropertyChanged(nameof(AddRouteSelection));
            }
        }
    }

    public void UpdateRemoteName(object sender, StaticRoutesInfo newTarget)
    {
        if (Target.NetId == AmsNetId.Local.ToString())
            AddRouteSelection.RemoteName = Environment.MachineName;
        else
            AddRouteSelection.RemoteName = newTarget.Name;

        OnPropertyChanged(nameof(AddRouteSelection));
    }

    private AddRouteInfo _AddRouteSelection;
    public AddRouteInfo AddRouteSelection
    {
        get => _AddRouteSelection;
        set
        {
            _AddRouteSelection = value;
            OnPropertyChanged();
        }
    }

    private string _IpOrHostnameInput;
    public string IpOrHostnameInput { get => _IpOrHostnameInput; set { _IpOrHostnameInput = value; OnPropertyChanged(); } }

  

    public void LoadNetworkAdapters(object sender, StaticRoutesInfo newTarget) 
    {
        if (Target is not null)
            _ = LoadNetworkAdaptersAsync();
    }
    public async Task LoadNetworkAdaptersAsync()
    {
        if (Target is null) return;

        using AdsRoutingClient client = new ();
        await client.Connect(Target?.NetId);
        var adapters = await client.GetNetworkInterfacesAsync();
        var adapterItems = adapters.Select(adapter => new NetworkAdapterItem { AdapterInfo = adapter, IsSelected = true }).ToList();
        NetworkAdapters = adapterItems;
        NetworkAdapterPairs.Clear();
        for (int i = 0; i < adapterItems.Count; i += 2)
        {
            var pair = new NetworkAdapterPair
            {
                Adapter1 = adapterItems[i],
                Adapter2 = (i + 1 < adapterItems.Count) ? adapterItems[i + 1] : null
            };
            NetworkAdapterPairs.Add(pair);
        }
    }


    public AsyncRelayCommand BroadcastCommand { get; }

    public async Task Broadcast()
    {
        if (Target is null) return;

        if(NetworkAdapters != null)
        {
            List<NetworkInterfaceInfo> nicsToBroadcastOn = new();
            foreach (var nic in NetworkAdapters)
            {
                if (nic.IsSelected)
                {
                    nicsToBroadcastOn.Add(nic.AdapterInfo);
                }
            }
            if (nicsToBroadcastOn.Count == 0)
                return;

            using AdsRoutingClient client = new();
            await client.Connect(Target?.NetId);
            TargetInfoList.Clear();
            await foreach (var target in client.AdsBroadcastSearchStreamAsync(nicsToBroadcastOn))
            {
                TargetInfoList.Add(target);
            }
        }
    }

    public AsyncRelayCommand SearchByIpOrNameCommand { get; }

    private async Task SearchByIpOrName()
    {
        if (string.IsNullOrEmpty(IpOrHostnameInput))
        {
            return;
        }
        if (IPAddress.TryParse(IpOrHostnameInput, out _))
        {
            await SearchByIp(IpOrHostnameInput);
        }
        else
        {
            await SearchByName();
        }
    }

    public async Task SearchByIp(string ipAddress)
    {
        if (Target is null) return;

        using AdsRoutingClient client = new();
        await client.Connect(Target?.NetId);
        TargetInfoList.Clear();
        await foreach (var target in client.AdsSearchByIpAsync(ipAddress))
        {
            TargetInfoList.Add(target);
        }
    }

    public async Task SearchByName()
    {
        using AdsRoutingClient routingClient = new();
        await routingClient.Connect(Target?.NetId);

        string? ip = await routingClient.GetIpFromHostname(IpOrHostnameInput);
            
        if (ip is not null)
        {
            await SearchByIp(ip);  
        }
        else
        {
            return;
        }
    }

    public AsyncRelayCommand AddRouteCommand { get; }
    public async Task AddRoute()
    {
        if (Target is null) return;

        if (!AddRouteSelection.AllParametersProvided()) return;

        // Check if adding route via hostname makes sense if that option is selected
        if (AddRouteSelection.AddByHostname)
        {
            try
            {
                var hostIPs = Dns.GetHostAddresses(AddRouteSelection.Name);
            }
            catch
            {
                var result = MessageBox.Show("Could not resolve Hostname. This might be due to the network structure. You might want to add by IP instead.\nProceed anyway?",
                                     "Hostname Resolution Failed",
                                     MessageBoxButton.YesNo,
                                     MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    _LoggerService.LogInfo("Action canceled");
                    return;
                }
            }
        }

        using AdsRoutingClient routingClient = new();
        await routingClient.Connect(Target?.NetId);

        if (AddRouteSelection.TypeStaticLocal)
        {
            if (AddRouteSelection.AddByIpAddress)
            {
                await routingClient.AddLocalRouteEntryByIpAsync(AddRouteSelection.NetId, AddRouteSelection.IpAddress, AddRouteSelection.Name); 
            }
            else
            {
                await routingClient.AddLocalRouteEntryByNameAsync(AddRouteSelection.NetId, AddRouteSelection.HostName, AddRouteSelection.Name);
            }
        }
        if (AddRouteSelection.TypeTempLocal)
        {
            if (AddRouteSelection.AddByIpAddress)
            {
                await routingClient.AddLocalRouteEntryByIpAsync(AddRouteSelection.NetId, AddRouteSelection.IpAddress, AddRouteSelection.Name, temporary: true);
            }
            else
            {
                await routingClient.AddLocalRouteEntryByNameAsync(AddRouteSelection.NetId, AddRouteSelection.HostName, AddRouteSelection.Name, temporary: true);
            }
        }

        if (AddRouteSelection.TypeStaticRemote)
        {
            if (AddRouteSelection.AddByIpAddress)
            {
                await routingClient.AddRemoteRouteEntryByIpAsync(AddRouteSelection.IpAddress, AddRouteSelection.Username, AddRouteSelection.Password, AddRouteSelection.RemoteName, false);
            }
            else
            {
                await routingClient.AddRemoteRouteEntryByNameAsync(AddRouteSelection.HostName, AddRouteSelection.Username, AddRouteSelection.Password, AddRouteSelection.RemoteName, false);
            }
        }
        if (AddRouteSelection.TypeTempRemote)
        {
            if (AddRouteSelection.AddByIpAddress)
            {
                await routingClient.AddRemoteRouteEntryByIpAsync(AddRouteSelection.IpAddress, AddRouteSelection.Username, AddRouteSelection.Password, AddRouteSelection.RemoteName, true);
            }
            else
            {
                await routingClient.AddRemoteRouteEntryByIpAsync(AddRouteSelection.HostName, AddRouteSelection.Username, AddRouteSelection.Password, AddRouteSelection.RemoteName, true);
            }
        }
    }

}

public class NetworkAdapterItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public NetworkInterfaceInfo AdapterInfo { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class NetworkAdapterPair
{
    public NetworkAdapterItem Adapter1 { get; set; }
    public NetworkAdapterItem Adapter2 { get; set; } // Is null if number of nics is uneven
}

public class AddRouteInfo : INotifyPropertyChanged
{
    public string Name { get; set; }
    public string NetId { get; set; }
    public string IpAddress { get; set; }
    public string HostName { get; set; }
    public string RemoteName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    private bool _addByIpAddress = true;
    private bool _addByHostname;

    public bool AddByIpAddress
    {
        get { return _addByIpAddress; }
        set
        {
            if (_addByIpAddress != value)
            {
                _addByIpAddress = value;
                OnPropertyChanged(nameof(AddByIpAddress));
            }
        }
    }
    public bool AddByHostname
    {
        get { return _addByHostname; }
        set
        {
            if (_addByHostname != value)
            {
                _addByHostname = value;
                OnPropertyChanged(nameof(_addByHostname));
            }
        }
    }

    private bool _typeNoneRemote;
    private bool _typeStaticRemote = true;
    private bool _typeTempRemote;

    public bool TypeNoneRemote
    {
        get { return _typeNoneRemote; }
        set
        {
            if (_typeNoneRemote != value)
            {
                _typeNoneRemote = value;
                OnPropertyChanged(nameof(TypeNoneRemote));
            }
        }
    }

    public bool TypeStaticRemote
    {
        get { return _typeStaticRemote; }
        set
        {
            if (_typeStaticRemote != value)
            {
                _typeStaticRemote = value;
                OnPropertyChanged(nameof(TypeStaticRemote));
            }
        }
    }

    public bool TypeTempRemote
    {
        get { return _typeTempRemote; }
        set
        {
            if (_typeTempRemote != value)
            {
                _typeTempRemote = value;
                OnPropertyChanged(nameof(TypeTempRemote));
            }
        }
    }
    private bool _typeNoneLocal;
    private bool _typeStaticLocal = true;
    private bool _typeTempLocal;

    public bool TypeNoneLocal
    {
        get { return _typeNoneLocal; }
        set
        {
            if (_typeNoneLocal != value)
            {
                _typeNoneLocal = value;
                OnPropertyChanged(nameof(_typeNoneLocal));
            }
        }
    }

    public bool TypeStaticLocal
    {
        get { return _typeStaticLocal; }
        set
        {
            if (_typeStaticLocal != value)
            {
                _typeStaticLocal = value;
                OnPropertyChanged(nameof(_typeStaticLocal));
            }
        }
    }

    public bool TypeTempLocal
    {
        get { return _typeTempLocal; }
        set
        {
            if (_typeTempLocal != value)
            {
                _typeTempLocal = value;
                OnPropertyChanged(nameof(TypeTempLocal));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool AllParametersProvided()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;
        if (string.IsNullOrWhiteSpace(NetId))
            return false;
        if(string.IsNullOrWhiteSpace(IpAddress) && AddByIpAddress)
            return false;
        if(string.IsNullOrWhiteSpace(HostName) && AddByHostname)
            return false;
        if(string.IsNullOrWhiteSpace(RemoteName))
            return false;
        if (string.IsNullOrWhiteSpace(Username)) 
            return false;
        if (string.IsNullOrWhiteSpace(Password)) 
            return false;
        return true;
    }
}
