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
                // start RDP client with given IP address
                Process rdpProcess = new();
                rdpProcess.StartInfo.FileName = "mstsc";
                rdpProcess.StartInfo.Arguments = $"/v:{ipAddress}";
                rdpProcess.StartInfo.UseShellExecute = true;
                rdpProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
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


            using TcpClient tcpClient = new ();
            var connectTask = tcpClient.ConnectAsync(ipAddress, 987);

            if (await Task.WhenAny(connectTask, Task.Delay(100)) == connectTask)
            {
                // Connection successful
                bool cerhostEnabled = tcpClient.Connected;
            }
            else
            {
                // Timeout --> ToDo: Dialog "Enable CerHost and reboot?"
            }

            Process cerhostProcess = new();
            cerhostProcess.StartInfo.FileName = cerhostPath;
            cerhostProcess.StartInfo.Arguments = ipAddress;
            cerhostProcess.Start();

        }

        public static void SshPowerShellConnect(string ipAddress, string targetName)
        {
            string username = string.Empty;

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
            // Create a key in .ssh
            string sshKeyName = targetName + DateTime.Now.ToString("_yyyy-MM-dd_HH-mm-ss");
            string keyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", sshKeyName);
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "config");

            // Comment for key: local Hostname + Username 
            string comment = $"{Environment.MachineName}_{Environment.UserName}";

            // Generate SSH key
            string keyGenCommand = $"-t ed25519 -f \"{keyPath}\" -C \"{comment}\" -N \"\"";
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\log.txt", keyGenCommand, Encoding.UTF8);
            ExecuteSshKeygenCommand(keyGenCommand);

            // Copy public key to target (add to authorized_keys)
            string publicKeyPath = $"{keyPath}.pub";
            string scpCommand = $"scp -o StrictHostKeyChecking=no '{publicKeyPath}' {username}@{targetIp}:~/.ssh ; ssh {username}@{targetIp} -t 'cd ~/.ssh && cat {sshKeyName}.pub >> ./authorized_keys && rm ./{sshKeyName}.pub'";
            ExecuteShellCommand(scpCommand);

            // Add entry to SSH config file
            string configEntry = $"\nHost {targetName}\n\tUser {username}\n\tHostName {targetName}\n\tIdentityFile \"{keyPath}\"\n"
                + $"Host {targetIp}\n\tUser {username}\n\tHostName {targetIp}\n\tIdentityFile \"{keyPath}\"";
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
                using var process = Process.Start(processStartInfo) ;
                
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Error: {error}");
                }
                else
                {
                    Console.WriteLine($"Output: {output}");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing a command: {ex.Message}");
            }
        }

        static void ExecuteSshKeygenCommand(string command)
        {
            ProcessStartInfo processStartInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "ssh-keygen",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            try
            {
                using var process = Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing a command: {ex.Message}");
            }
        }

        public static void SshConnectWithoutKey(string username, string ipAddress)
        {
            string sshCommand = $"ssh {username}@{ipAddress}";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command \"{sshCommand}\"",
                UseShellExecute = true,   // --> PowerShell window visible
                CreateNoWindow = false    
            };

            try
            {
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
                // Save path in config file
                string json = JsonSerializer.Serialize(openFileDialog.FileName);
                await File.WriteAllTextAsync(GlobalVars.ConfigFilePath, json);
                return openFileDialog.FileName;
            }

            return string.Empty;
        }

    }
}
