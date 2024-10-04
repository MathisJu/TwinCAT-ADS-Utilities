using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AdsUtilitiesUI.Model
{
    internal static class RemoteConnector
    {
        public static void RdpConnect(string ipAddress)
        {
            // For Big Windows -> Start RDP w/ IP
            // HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server -- fDenyTSConnections > 0
            // (runas) netsh advfirewall firewall set rule group=\"Remote Desktop\" new enable=Yes

            try
            {
                // Prozess starten, der den RDP-Client mit der angegebenen IP-Adresse öffnet
                Process rdpProcess = new Process();
                rdpProcess.StartInfo.FileName = "mstsc";
                rdpProcess.StartInfo.Arguments = $"/v:{ipAddress}";
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

        public static async Task CerhostConnect(string ipAddress)
        {
            string cerhostPath = await GetCerhostPathAsync();

            if (string.IsNullOrEmpty(cerhostPath))
                return;


            using TcpClient tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(ipAddress, 987);

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
            cerhostProcess.StartInfo.Arguments = ipAddress;
            cerhostProcess.Start();

        }

        public static void SshPowershellConnect(string ipAddress, string targetName)
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
                SshConnectWithoutKey(username, ipAddress);
                return;
            }
            else
            {
                GenerateAndDeploySSHKeyAsync(username, targetName, ipAddress);
                SshConnectWithoutKey(username, ipAddress);
            }
        }

        private static void GenerateAndDeploySSHKeyAsync(string username, string targetName, string targetIp)
        {
            string localHostname = Environment.MachineName;
            string sshKeyName = targetName + DateTime.Now.ToString("_yyyy-MM-dd_HH-mm-ss");
            string keyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", sshKeyName);    // ToDo: Test if exists
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "config");

            // Kommentar für den Schlüssel: Hostname + Username des aktuellen Rechners
            string localUsername = Environment.UserName;
            string comment = $"{localHostname}_{localUsername}";

            // Generiere den SSH-Schlüssel mit Kommentar
            //string keyGenCommand = $"ssh-keygen -t ed25519 -f \"{keyPath}\" -C \"{comment}\" -N \"\"";
            string keyGenCommand = $"-t ed25519 -f \"{keyPath}\" -C \"{comment}\" -N \"\"";
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\log.txt", keyGenCommand, Encoding.UTF8);
            ExecuteSshKeygenCommand(keyGenCommand);

            // Pfad des öffentlichen Schlüssels
            string publicKeyPath = $"{keyPath}.pub";

            // Kopiere den öffentlichen Schlüssel zum Zielgerät (füge ihn zu authorized_keys hinzu)
            string scpCommand = $"scp -o StrictHostKeyChecking=no '{publicKeyPath}' {username}@{targetIp}:~/.ssh ; ssh {username}@{targetIp} -t 'cd ~/.ssh && cat {sshKeyName}.pub >> ./authorized_keys && rm ./{sshKeyName}.pub'";
            ExecuteShellCommand(scpCommand);

            string configEntry = $"\nHost {targetName}\n\tUser {username}\n\tHostName {targetName}\n\tIdentityFile \"{keyPath}\"\n"
                + $"Host {targetIp}\n\tUser {username}\n\tHostName {targetIp}\n\tIdentityFile \"{keyPath}\"";

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Ausführen des Befehls: {ex.Message}");
            }
        }

        public static void SshConnectWithoutKey(string username, string ipAddress)
        {
            string sshCommand = $"ssh {username}@{ipAddress}";

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

        private static async Task<string> GetCerhostPathAsync()
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
