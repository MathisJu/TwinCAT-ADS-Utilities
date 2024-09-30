using AdsUtilities;
using AdsUtilitiesUI.Model;
using AdsUtilitiesUI.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Xml.Linq;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;

namespace AdsUtilitiesUI
{
    class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<TabViewModel> Tabs { get; set; }
        private TabViewModel _selectedTab;

        private readonly TargetService _targetService;

        private readonly LoggerService _loggerService;

        public ObservableCollection<StaticRoutesInfo> AvailableTargets => _targetService.AvailableTargets;

        public ObservableCollection<LogMessage> LogMessages { get; set; }

        private StaticRoutesInfo _selectedTarget;
        public StaticRoutesInfo SelectedTarget
        {
            get => _selectedTarget;
            set
            {
                if (_selectedTarget.NetId != value.NetId)
                {
                    _selectedTarget = value;
                    _targetService.CurrentTarget = value; // TargetService aktualisieren
                    OnPropertyChanged(nameof(SelectedTarget));
                }
            }
        }

        public TabViewModel SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged();
            }
        }

        public MainWindowViewModel() 
        {
            _targetService = new TargetService();
            _targetService.OnTargetChanged += TargetService_OnTargetChanged;

            RemoteConnectCommand = new(SetupRemoteConnection);

            LogMessages = new();
            _loggerService = new LoggerService(LogMessages, System.Windows.Threading.Dispatcher.CurrentDispatcher);
            _loggerService.OnNewLogMessage += OnNewLogMessageReceived;

            Tabs = new ObservableCollection<TabViewModel>
            {
                new TabViewModel("Ads Routing", new AdsRoutingViewModel(_targetService, _loggerService)),
                new TabViewModel("File Access", new FileHandlingViewModel(_targetService, _loggerService)),
                new TabViewModel("Device Info", new DeviceInfoViewModel(_targetService, _loggerService)),
            };
            SelectedTab = Tabs[0];
        }

        private void TargetService_OnTargetChanged(object sender, StaticRoutesInfo newTarget)
        {
            SelectedTarget = newTarget;
        }

        private string _logMessage;
        public string LogMessage
        {
            get => _logMessage;
            set
            {
                _logMessage = value;
                OnPropertyChanged();
            }
        }

        private string _icon;
        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        private string _timestamp;
        public string Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                OnPropertyChanged();
            }
        }
        private async void OnNewLogMessageReceived(object sender, LogMessage logMessage)
        {
            Timestamp = logMessage.Timestamp.ToString("HH:mm:ss");
            LogMessage = logMessage.Message;

            switch (logMessage.LogLevel)
            {
                case LogLevel.Success:
                    Icon = "✅";
                    break;
                case LogLevel.Error:
                    Icon = "❌";
                    break;
                case LogLevel.Info:
                    Icon = "ℹ️";
                    break;
                case LogLevel.Warning:
                    Icon = "⚠️";
                    break;
            }
 
            await Task.Delay(5000);
            Timestamp = string.Empty;
            LogMessage = string.Empty;
            Icon = string.Empty;
        }


        public AsyncRelayCommand RemoteConnectCommand { get; }
        public async Task SetupRemoteConnection()
        {
            // Cancel if route is local or invalid
            if (string.IsNullOrEmpty(SelectedTarget.NetId))
                return;

            if (IPAddress.TryParse(SelectedTarget.IpAddress, out IPAddress? address))
            {
                if (address is not null && address.AddressFamily == AddressFamily.InterNetwork)
                {
                    byte[] bytes = address.GetAddressBytes();

                    if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0)
                        return;
                }
            }
            

            // Check OS
            using AdsSystemClient systemClient = new AdsSystemClient();
            systemClient.Connect(SelectedTarget.NetId);
            SystemInfo sysInfo = await systemClient.GetSystemInfoAsync();
            string os = sysInfo.OsName;

            if (os.Contains("Windows"))
            {
                if (os.Contains("CE"))
                {
                    // Windows CE
                    await RemoteConnector.CerhostConnect(SelectedTarget.IpAddress);
                }
                else
                {
                    // Big Windows
                    await Task.Run(() => RemoteConnector.RdpConnect(SelectedTarget.IpAddress));
                }
            }
            else if (os.Contains("BSD"))
            {
                // TC/BSD
                RemoteConnector.SshPowershellConnect(SelectedTarget.IpAddress, SelectedTarget.Name);
            }
            else
            {
                // For RTOS or unknown OS -> display error message
                throw new ArgumentException("The selected system does not support remote control or there is no implementation currently. This error should be replaced with a message box."); // ToDo
            }          
        } 
    }
}
