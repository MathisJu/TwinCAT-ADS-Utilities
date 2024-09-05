using AdsUtilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AdsUtilitiesUI
{
    public class AdsRoutingViewModel : INotifyPropertyChanged
    {
        public List<NetworkAdapterItem> NetworkAdapters;
        public ObservableCollection<NetworkAdapterPair> NetworkAdapterPairs { get; set; }

        public ObservableCollection<TargetInfo> TargetInfoList { get; set; }

        public AdsRoutingViewModel()
        {
            NetworkAdapterPairs = new ObservableCollection<NetworkAdapterPair>();
            TargetInfoList = new ObservableCollection<TargetInfo>();
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

                AdsRoutingClient client = new();
                client.Connect(Target.NetId);
                TargetInfoList.Clear();
                await foreach (var target in client.AdsBroadcastSearchStreamAsync(nicsToBroadcastOn))
                {
                    TargetInfoList.Add(target);
                }
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
        public NetworkAdapterItem Adapter2 { get; set; } // Kann null sein, wenn die Anzahl ungerade ist
    }

}
