using AdsUtilities;
using AdsUtilitiesUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using TwinCAT.Ads;

namespace AdsUtilitiesUI;

public class DeviceInfoViewModel : ViewModelTargetAccessPage
{

    public ObservableCollection<NetworkInterfaceInfo> NetworkInterfaces { get; set; }

    private SystemInfo _systemInfo;
    public SystemInfo SystemInfo
    {
        get => _systemInfo;
        set
        {
            _systemInfo = value;
            OnPropertyChanged(nameof(SystemInfo));
        }
    }
    
    private readonly System.Timers.Timer _updateTimer;

    private string _TcState;
    public string TcState
    {
        get => _TcState;
        set
        {
            _TcState = value;
            OnPropertyChanged(nameof(TcState));
        }
    }

    private RouterStatusInfo _routerStatusInfo;
    public RouterStatusInfo RouterStatusInfo
    {
        get => _routerStatusInfo;
        set
        {
            _routerStatusInfo = value;
            OnPropertyChanged(nameof(RouterStatusInfo));
        }
    }

    private DateTime _targetTime;

    public DateTime TargetTime
    {
        get => _targetTime;
        set
        {
            _targetTime = value;
            OnPropertyChanged(nameof(TargetTime));
            OnPropertyChanged(nameof(TargetTimeDisplay));
        }
    }

    public string TargetTimeDisplay
    {
        get => TargetTime.ToString("yyyy/MM/dd-HH:mm");
    }

    private string _systemId;
    public string SystemId
    {
        get => _systemId;
        set
        {
            _systemId = value;
            OnPropertyChanged(nameof(SystemId));
        }
    }

    private uint _volumeNumber;
    public uint VolumeNumber
    {
        get => _volumeNumber;
        set
        {
            _volumeNumber = value;
            OnPropertyChanged(nameof(VolumeNumber));
        }
    }

    private short _platformLevel;
    public short PlatformLevel
    {
        get => _platformLevel;
        set
        {
            _platformLevel = value;
            OnPropertyChanged(nameof(PlatformLevel));
        }
    }

    public ICommand InstallRteDriverCommand { get; }

    public ICommand SetTickCommand { get; }

    public ICommand RebootCommand { get; }

    public DeviceInfoViewModel(TargetService targetService, ILoggerService loggerService)
    {
        _TargetService = targetService;
        InitTargetAccess(_TargetService);

        _LoggerService = (LoggerService)loggerService;

        _TargetService.OnTargetChanged += async (sender, args) => await UpdateDeviceInfo();

        _updateTimer = new System.Timers.Timer(10_000);
        _updateTimer.Elapsed += async (sender, e) => await UpdateLiveValues();
        _updateTimer.AutoReset = true;
        _updateTimer.Start();
        
        InstallRteDriverCommand = new AsyncRelayCommand(async (parameter) => await InstallRteDriver(parameter), CanInstallRteDriver);
        SetTickCommand = new AsyncRelayCommand(async async => await ExecuteSetTick());
        RebootCommand = new AsyncRelayCommand(async async => await RebootTarget());

        NetworkInterfaces = new ObservableCollection<NetworkInterfaceInfo>(); 
    }

    public async Task UpdateDeviceInfo()
    {
        await LoadNetworkInterfacesAsync();
        await LoadSystemInfoAsync();
        await UpdateTcState();
        await UpdateRouterUsage();
        await UpdateTime();
        await UpdateSystemId();
        await UpdateVolumeNumber();
        await UpdatePlatformLevel();
    }

    private bool _isUpdating = false;

    public async Task UpdateLiveValues()
    {
        if (_isUpdating) return;

        _isUpdating = true;
        await UpdateTcState();         
        await UpdateRouterUsage();     
        await UpdateTime();            
        _isUpdating = false;
    }

    public async Task LoadSystemInfoAsync(CancellationToken cancel = default)
    {
        try
        {
            using AdsSystemClient systemClient = new();
            await systemClient.Connect(Target?.NetId);
            SystemInfo = await systemClient.GetSystemInfoAsync(cancel);
        }
        catch (Exception ex) { }
    }

    public async Task UpdateTime(CancellationToken cancel = default)
    {
        try
        {
            using AdsSystemClient systemClient = new();
            await systemClient.Connect(Target?.NetId);
            TargetTime = await systemClient.GetSystemTimeAsync(cancel);
        }
        catch (Exception ex) { }
    }

    public async Task UpdateSystemId(CancellationToken cancel = default)
    {
        try
        {
            using AdsSystemClient systemClient = new();
            await systemClient.Connect(Target?.NetId);
            SystemId = await systemClient.GetSystemIdStringAsync(cancel);
        }
        catch (Exception ex) { }
    }

    public async Task UpdateVolumeNumber(CancellationToken cancel = default)
    {
        try
        {
            using AdsSystemClient systemClient = new();
            await systemClient.Connect(Target?.NetId);
            VolumeNumber = await systemClient.GetVolumeNumberAsync(cancel);
        }
        catch (Exception ex) { }
    }

    public async Task UpdatePlatformLevel(CancellationToken cancel = default)
    {
        try
        {
            using AdsSystemClient systemClient = new();
            await systemClient.Connect(Target?.NetId);
            PlatformLevel = await systemClient.GetPlatformLevelAsync(cancel);
        }
        catch (Exception ex) { }
    }

    public async Task UpdateTcState(CancellationToken cancel = default)
    {
        try
        {
            using AdsClient adsClient = new();
            adsClient.Connect(Target?.NetId, 10_000);
            ResultReadDeviceState state = await adsClient.ReadStateAsync(cancel);
            TcState = state.State.AdsState.ToString();
        }
        catch (Exception ex) 
        {
            TcState = ex.Message;
        }
    }

    public async Task UpdateRouterUsage(CancellationToken cancel = default)
    {
        try
        {
            using AdsSystemClient systemClient = new();
            await systemClient.Connect(Target?.NetId);
            var routerInfo = await systemClient.GetRouterStatusInfoAsync(cancel);
            RouterStatusInfo = routerInfo;
        }
        catch (Exception ex) { }
    }


    public bool CanInstallRteDriver()
    {
        // RTE drivers are preinstalled on WinCE, BSD and RTOS
        if (!SystemInfo.OsName.Contains("Win") || SystemInfo.OsName.Contains("CE"))
        {
            _LoggerService.LogInfo("Drivers are preinstalled on the selected target");
            return false;
        }
            
        // There is no cli to install drivers remotely on TC2 systems
        if (SystemInfo.TargetVersion.StartsWith("2."))
        {
            _LoggerService.LogWarning("Cannot install drivers on TC2 systems remotely");
            return false;
        }

        // ToDo: Check if driver is installed already

        return true;
    }

    public async Task LoadNetworkInterfacesAsync(CancellationToken cancel = default)
    {
        try
        {
            using AdsRoutingClient routingClient = new();
            await routingClient.Connect(Target?.NetId);
            var interfaces = await routingClient.GetNetworkInterfacesAsync(cancel);

            NetworkInterfaces.Clear();
            foreach (var nic in interfaces)
            {
                NetworkInterfaces.Add(nic);
            }
        }
        catch (Exception ex)
        {
            // Error handling
        }
    }

    private async Task InstallRteDriver(object networkInterface)
    {
        if (networkInterface is NetworkInterfaceInfo nic)
        {
            var rteInstallerPath = @"C:\TwinCAT\3.1\System\TcRteInstall.exe";   // ToDo: Get path and dir at runtime
            var directory = @"C:\TwinCAT\3.1\System";

            var installCommand = $"-r installnic \"{nic.Name}\"";

            using AdsFileClient fileClient = new ();
            await fileClient.Connect(Target?.NetId);
            await fileClient.StartProcessAsync(rteInstallerPath, directory, installCommand);
            return;
        }
        _LoggerService.LogError("Unexpected Error occured");
    }

    private async Task ExecuteSetTick()
    {
        // No need to set tick on WinCE, BSD and RTOS, ToDo: move to CanSetTick() method and reference it in relay command
        if (!SystemInfo.OsName.Contains("Win") || SystemInfo.OsName.Contains("CE"))
        {
            _LoggerService.LogInfo("No need to set tick on selected target");
            return;
        }

        string path = "";
        string dir = "";

        // There is no cli to install drivers remotely on TC2 systems
        if (SystemInfo.TargetVersion.StartsWith("2."))
        {
            path = @"C:\TwinCAT\Io\win8settick.bat";  // ToDo: Get path and dir at runtime
            dir = @"C:\TwinCAT\Io";
        }
        else
        {
            path = @"C:\TwinCAT\3.1\System\win8settick.bat";    // ToDo: Get path and dir at runtime
            dir = @"C:\TwinCAT\3.1\System";
        }

        try
        {
            using AdsFileClient fileClient = new();
            await fileClient.Connect(Target?.NetId);
            await fileClient.StartProcessAsync(path, dir, string.Empty);
        }
        catch (Exception ex)
        {
            _LoggerService.LogError("Execution of win8settick.bat failed");
            return;
        }
        _LoggerService.LogSuccess("Set tick successfully. Please reboot target");
    }

    private async Task RebootTarget()
    {
        if (Target?.NetId == AmsNetId.Local.ToString())
        {
            _LoggerService.LogInfo("Reboot of local system blocked");
            return;
        }

        try
        {
            using AdsSystemClient systemClient = new();
            await systemClient.Connect(Target?.NetId);
            await systemClient.RebootAsync();
        }
        catch
        {
            _LoggerService.LogError("Could not reboot target");
        }
        _LoggerService.LogSuccess("Rebooting now...");
    }
}
