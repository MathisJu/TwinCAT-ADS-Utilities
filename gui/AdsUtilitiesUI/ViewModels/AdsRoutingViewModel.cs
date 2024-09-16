using AdsUtilities;
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
using System.Windows.Data;

namespace AdsUtilitiesUI
{
    public class AdsRoutingViewModel : INotifyPropertyChanged
    {
        public List<NetworkAdapterItem> NetworkAdapters;
        public ObservableCollection<NetworkAdapterPair> NetworkAdapterPairs { get; set; }

        public ObservableCollection<TargetInfo> TargetInfoList { get; set; }

        public StatusViewModel? StatusLogger;

        private string _IpOrHostnameInput;
        public string IpOrHostnameInput { get => _IpOrHostnameInput; set { _IpOrHostnameInput = value; OnPropertyChanged(); } }

        public AdsRoutingViewModel()
        {
            NetworkAdapterPairs = new ObservableCollection<NetworkAdapterPair>();
            TargetInfoList = new ObservableCollection<TargetInfo>();

            AddRouteSelection = new()
            {
                RemoteName = Environment.MachineName
            };
        }

        private StaticRoutesInfo _Target;

        public StaticRoutesInfo Target
        {
            get => _Target;
            set
            {
                if (_Target.Name != value.Name)
                {
                    _Target = value;
                    OnPropertyChanged();
                    _ = LoadNetworkAdaptersAsync();
                }
            }
        }

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

        public async Task Broadcast()
        {
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
                client.Connect(Target.NetId);
                TargetInfoList.Clear();
                await foreach (var target in client.AdsBroadcastSearchStreamAsync(nicsToBroadcastOn))
                {
                    TargetInfoList.Add(target);
                }
            }
        }

        public async Task SearchByIp()
        {
            await SearchByIp(IpOrHostnameInput);
        }

        public async Task SearchByIp(string ipAddress)
        {
            using AdsRoutingClient client = new();
            client.Connect(Target.NetId);
            TargetInfoList.Clear();
            await foreach (var target in client.AdsSearchByIpAsync(ipAddress))
            {
                TargetInfoList.Add(target);
            }
        }

        public async Task SearchByName()
        {
            try
            {
                // The Search by Hostname function in the default route dialog sends a search command that contains the corresponding ip address. There might be an ADS function to get the ip for a known name but for now it is done using dns directly
                IPHostEntry hostEntry = Dns.GetHostEntry(IpOrHostnameInput);

                if (hostEntry.AddressList.Length > 0)
                {
                    await SearchByIp(hostEntry.AddressList[0].ToString());  
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public async Task LoadNetworkAdaptersAsync()
        {
            using AdsRoutingClient client = new AdsRoutingClient();
            client.Connect(Target.NetId);
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

        public async Task AddRoute()
        {
            using AdsRoutingClient routingClient = new();
            routingClient.Connect(Target.NetId);

            if (AddRouteSelection.TypeStaticLocal)
            {
                await routingClient.AddLocalRouteEntryAsync(AddRouteSelection.NetId, AddRouteSelection.IpAddress, AddRouteSelection.Name); // ToDo: Add option for route via Hostname
            }
            if (AddRouteSelection.TypeTempLocal)
            {
                StatusLogger?.ShowError("Temporary routes not implemented yet."); // ToDo: Add temporary route option
            }


            if (AddRouteSelection.TypeStaticRemote)
            {
                await routingClient.AddRemoteRouteEntryAsync(AddRouteSelection.IpAddress, AddRouteSelection.Username, AddRouteSelection.Password, AddRouteSelection.RemoteName);
                StatusLogger?.ShowSuccess("Route added.");
            }
            if (AddRouteSelection.TypeTempRemote)
            {
                StatusLogger?.ShowError("Temporary routes not implemented yet.");// ToDo: Add temporary route option
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
}
