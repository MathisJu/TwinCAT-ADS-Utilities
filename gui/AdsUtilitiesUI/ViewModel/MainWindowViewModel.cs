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

namespace AdsUtilitiesUI;

class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<TabViewModel> Tabs { get; set; }
    private TabViewModel _selectedTab;

    public TargetService _targetService { get; }

    public LoggerService _loggerService { get; }

    //public ObservableCollection<StaticRouteStatus> AvailableTargets => _targetService.AvailableTargets;



    //private StaticRouteStatus _currentTarget;

    //public StaticRouteStatus CurrentTarget
    //{
    //    get => _currentTarget;
    //    set
    //    {
    //        if (value != null && _currentTarget?.NetId != value.NetId)
    //        {
    //            _currentTarget = value;
    //            OnPropertyChanged(nameof(CurrentTarget));
               
    //            if (_targetService.CurrentTarget?.NetId != value.NetId)
    //            {
    //                _targetService.CurrentTarget = value; 
    //            }
    //        }
    //    }
    //}

    public ObservableCollection<LogMessage> LogMessages { get; set; }

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
        ReloadRoutesCommand = new(ReloadRoutes);

        LogMessages = new();
        _loggerService = new LoggerService(LogMessages, System.Windows.Threading.Dispatcher.CurrentDispatcher);
        _loggerService.OnNewLogMessage += OnNewLogMessageReceived;

        Tabs = new ObservableCollection<TabViewModel>
        {
            new TabViewModel("ADS Routing", new AdsRoutingViewModel(_targetService, _loggerService)),
            new TabViewModel("File Access", new FileHandlingViewModel(_targetService, _loggerService)),
            new TabViewModel("Device Info", new DeviceInfoViewModel(_targetService, _loggerService)),
        };
        SelectedTab = Tabs[0];
    }

    private void TargetService_OnTargetChanged(object sender, StaticRouteStatus newTarget)
    {
        OnPropertyChanged(nameof(_targetService.CurrentTarget));
        //if (CurrentTarget?.NetId != newTarget.NetId)
        //{
        //    CurrentTarget = newTarget;
        //}
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

    
    public AsyncRelayCommand ReloadRoutesCommand { get; }

    public async Task ReloadRoutes()
    {
        await _targetService.Reload_Routes();
    }

    public AsyncRelayCommand RemoteConnectCommand { get; }
    public async Task SetupRemoteConnection()
    {
        // Cancel if route is local or invalid
        if (string.IsNullOrEmpty(_targetService.CurrentTarget.NetId))
            return;

        if (IPAddress.TryParse(_targetService.CurrentTarget.IpAddress, out IPAddress? address))
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
        systemClient.Connect(_targetService.CurrentTarget.NetId);
        SystemInfo sysInfo = await systemClient.GetSystemInfoAsync();
        string os = sysInfo.OsName;

        if (os.Contains("Windows"))
        {
            if (os.Contains("CE"))
            {
                // Windows CE
                await RemoteConnector.CerhostConnect(_targetService.CurrentTarget.IpAddress);
            }
            else
            {
                // Big Windows
                await Task.Run(() => RemoteConnector.RdpConnect(_targetService.CurrentTarget.IpAddress));
            }
        }
        else if (os.Contains("BSD"))
        {
            // TC/BSD
            RemoteConnector.SshPowershellConnect(_targetService.CurrentTarget.IpAddress, _targetService.CurrentTarget.Name);
        }
        else
        {
            // For RTOS or unknown OS -> display error message
            throw new ArgumentException("The selected system does not support remote control or there is no implementation currently. This error should be replaced with a message box."); // ToDo
        }          
    } 
}
