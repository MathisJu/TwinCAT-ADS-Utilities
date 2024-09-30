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

namespace AdsUtilitiesUI
{
    public class DeviceInfoViewModel : ViewModelTargetAccessPage
    {

        public ObservableCollection<NetworkInterfaceInfo> NetworkInterfaces { get; set; }

        public ICommand InstallRteDriverCommand { get; }

        public DeviceInfoViewModel(TargetService targetService, ILoggerService loggerService)
        {
            _TargetService = targetService;
            InitTargetAccess(_TargetService);
            _TargetService.OnTargetChanged += async (sender, args) => await LoadNetworkInterfacesAsync();

            _LoggerService = (LoggerService)loggerService;

            InstallRteDriverCommand = new AsyncRelayCommand(async (parameter) => await InstallRteDriver(parameter), CanInstallRteDriver);

            NetworkInterfaces = new ObservableCollection<NetworkInterfaceInfo>();

            
        }

        public bool CanInstallRteDriver()
        {
            // ToDo: Handle the following cases:
            // On TC2: Remote installation not possible
            // On CE, BSD and RTOS: Drivers are preinstalled

            // Check if driver is installed already and write to logger. Return false if installation is not possible
            return true;
        }

        public async Task LoadNetworkInterfacesAsync(CancellationToken cancel = default)
        {
            try
            {
                using AdsRoutingClient routingClient = new();
                routingClient.Connect(Target?.NetId);
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
                fileClient.Connect(Target?.NetId);
                await fileClient.StartProcessAsync(rteInstallerPath, directory, installCommand);
                return;
            }
            _LoggerService.LogError("Unexpected Error occured");
        }
    }
}
