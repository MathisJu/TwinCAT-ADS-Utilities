using AdsUtilities;
using AdsUtilitiesUI.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

            LogMessages = new();
            _loggerService = new LoggerService(LogMessages, System.Windows.Threading.Dispatcher.CurrentDispatcher);

            Tabs = new ObservableCollection<TabViewModel>
            {
                new TabViewModel("Ads Routing", new AdsRoutingViewModel(_targetService, _loggerService)),
                new TabViewModel("File Access", new FileHandlingViewModel(_targetService, _loggerService)),
            };
            SelectedTab = Tabs[0];
        }

        private void TargetService_OnTargetChanged(object sender, StaticRoutesInfo newTarget)
        {
            // Synchronisieren des ausgewählten Targets
            SelectedTarget = newTarget;
        }
      

        public async Task SetupRemoteConnection()
        {
            // Cancel if route is local or invalid
            if (IPAddress.TryParse(SelectedTarget.IpAddress, out IPAddress address))
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    byte[] bytes = address.GetAddressBytes();

                    if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0)
                        return;
                }
            }

            if (string.IsNullOrEmpty(SelectedTarget.NetId))
                return;

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
                    await cerhostConnect();
                }
                else
                {
                    // Big Windows
                    await Task.Run(() => rdpConnect());
                }
            }
            else if (os.Contains("BSD"))
            {
                // TC/BSD
                sshPowershellConnect();
            }
            else
            {
                // For RTOS or unknown OS -> display error message
                throw new ArgumentException("The selected system does not support remote control or there is no implementation currently. This error should be replaced with a message box."); // ToDo
            }          
        }

        private void rdpConnect()
        {
            // For Big Windows -> Start RDP w/ IP
            // HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server -- fDenyTSConnections > 0
            // (runas) netsh advfirewall firewall set rule group=\"Remote Desktop\" new enable=Yes

            try
            {
                // Prozess starten, der den RDP-Client mit der angegebenen IP-Adresse öffnet
                Process rdpProcess = new Process();
                rdpProcess.StartInfo.FileName = "mstsc";
                rdpProcess.StartInfo.Arguments = $"/v:{SelectedTarget.IpAddress}";
                rdpProcess.StartInfo.UseShellExecute = true;
                rdpProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                // Startet den Prozess
                rdpProcess.Start();
            }
            catch (Exception ex)
            {
                ;
            }
        }

        private async Task cerhostConnect()
        {
            string cerhostPath = await GetCerhostPathAsync();

            if (string.IsNullOrEmpty(cerhostPath))
                return;


            using TcpClient tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(SelectedTarget.IpAddress, 987);

            if (await Task.WhenAny(connectTask, Task.Delay(100)) == connectTask)
            {
                // Connection successfull
                bool cerhostEnabled = tcpClient.Connected;
            }
            else
            {
                // Timeout --> ToDo: Dialog "Enable CerHost and reboot?"
            }

            Process cerhostProcess = new Process();
            cerhostProcess.StartInfo.FileName = cerhostPath;
            cerhostProcess.StartInfo.Arguments = SelectedTarget.IpAddress;
            cerhostProcess.Start();

        }

        private void sshPowershellConnect()
        {
            //ToDo: Add InputDialog for Username
            string username = string.Empty;// = "Administrator";

            SshDialog sshDialog = new("Enter Username", "Enter Username:", "Administrator");

            if (sshDialog.ShowDialog() == true)
            {
                username = sshDialog.ResponseText;
            }

            if (string.IsNullOrEmpty(username))
                return;

            bool addSshKey = sshDialog.AddSshKey;

            if (!addSshKey)
            {
                SshConnectWithoutKey(username); 
                return;

            }
            else
            {
                GenerateAndDeploySSHKeyAsync(username);
            }
        }

        private void GenerateAndDeploySSHKeyAsync(string username)
        {
            string localHostname = Environment.MachineName;
            string sshKeyName = SelectedTarget.Name;
            string keyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", sshKeyName);    // ToDo: Test if exists
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "config");

            // Kommentar für den Schlüssel: Hostname + Username des aktuellen Rechners
            string localUsername = Environment.UserName;
            string comment = $"{localHostname}_{localUsername}";

            // Generiere den SSH-Schlüssel mit Kommentar
            // string keyGenCommand = $"ssh-keygen -t ed25519 -f \"{keyPath}\" -C \"{comment}\" -N \"\"";
            string keyGenCommand = $"-t ed25519 -f \"{keyPath}\" -C \"{comment}\" -N \"\"";
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\log.txt", keyGenCommand, Encoding.UTF8);
            ExecuteSshKeygenCommand(keyGenCommand);

            // Pfad des öffentlichen Schlüssels
            string publicKeyPath = $"{keyPath}.pub";

            // Kopiere den öffentlichen Schlüssel zum Zielgerät (füge ihn zu authorized_keys hinzu)
            string scpCommand = $"scp -o StrictHostKeyChecking=no '{publicKeyPath}' {username}@{SelectedTarget.IpAddress}:~/.ssh ; ssh {username}@{SelectedTarget.IpAddress} -t 'cd ~/.ssh && cat {sshKeyName}.pub >> ./authorized_keys && rm ./{sshKeyName}.pub'";
            ExecuteShellCommand(scpCommand);

            string configEntry = $"Host {SelectedTarget.Name}\n\tUser {username}\n\tHostName {SelectedTarget.Name}\n\tIdentityFile \"{keyPath}\"\n"
                + $"Host {SelectedTarget.IpAddress}\n\tUser {username}\n\tHostName {SelectedTarget.IpAddress}\n\tIdentityFile \"{keyPath}\"";

            // Füge den Eintrag in die SSH-Konfigurationsdatei ein
            File.AppendAllText(configPath, configEntry);
        }

        static void ExecuteShellCommand(string command)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            try
            {
                using (var process = Process.Start(processStartInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"Fehler: {error}");
                    }
                    else
                    {
                        Console.WriteLine($"Ausgabe: {output}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Ausführen des Befehls: {ex.Message}");
            }
        }

        static async Task ExecuteSshKeygenCommand(string command)
        {

            // This is a tricky one. If there is no existing key with the same name, this works fine. If there is, user input is required.
            // ToDo: Execute this in a minimized window. If there is an existing key, open up the window to let the user know and decide himself

            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo();
            processStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            processStartInfo.FileName = "ssh-keygen";
            processStartInfo.Arguments = command;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardInput = true;

            try
            {
                using var process = Process.Start(processStartInfo);
                int numRead = 0;
                char[] buffer = new char[2048];
                while (( numRead = await process.StandardOutput.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string output = new string(buffer, 0, numRead);
                    if (output.Contains("(y/n)"))
                    {
                        
                    }
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Ausführen des Befehls: {ex.Message}");
            }
        }

        public void SshConnectWithoutKey(string username)
        {
            string sshCommand = $"ssh {username}@{SelectedTarget.IpAddress}";

            // PowerShell-Prozess konfigurieren
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command \"{sshCommand}\"",
                UseShellExecute = true,   // Verwende die Shell-Ausführung, damit das PowerShell-Fenster sichtbar ist
                CreateNoWindow = false    // Fenster erstellen, damit der Benutzer die SSH-Verbindung sehen kann
            };

            try
            {
                // start PowerShell 
                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                ;
            }
        }

        private async Task<string> GetCerhostPathAsync()
        {
            if (!Directory.Exists(GlobalVars.AppFolder))
            {
                Directory.CreateDirectory(GlobalVars.AppFolder);
            }

            // Check if config file exists and contains the correct path
            if (File.Exists(GlobalVars.ConfigFilePath))
            {
                string json = await File.ReadAllTextAsync(GlobalVars.ConfigFilePath);
                string cerhostPath = JsonSerializer.Deserialize<string>(json);
                if (File.Exists(cerhostPath))
                {
                    return cerhostPath;
                }
            }

            // Path not found --> Select file dialog
            OpenFileDialog openFileDialog = new()
            {
                Filter = "CERHOST.exe|cerhost.exe",
                Title = "Select cerhost.exe"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Speichere den Pfad in der Konfigurationsdatei
                string json = JsonSerializer.Serialize(openFileDialog.FileName);
                await File.WriteAllTextAsync(GlobalVars.ConfigFilePath, json);
                return openFileDialog.FileName;
            }
            
            return string.Empty;
        }
    }
}
