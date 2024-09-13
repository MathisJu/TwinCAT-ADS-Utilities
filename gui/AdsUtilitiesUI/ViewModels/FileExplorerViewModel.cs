using AdsUtilities;
using AdsUtilitiesUI.Views.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace AdsUtilitiesUI
{
    class FileExplorerViewModel : INotifyPropertyChanged
    {
        public FileExplorerViewModel()
        {
            
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private StaticRoutesInfo _Target;
        public StaticRoutesInfo Target
        {
            get => _Target;
            set
            {
                _Target = value;
                LoadRootDirectories(_Target.NetId);
            }
        }

        private ObservableCollection<FileSystemItem> _rootItems = new ObservableCollection<FileSystemItem>();
        public ObservableCollection<FileSystemItem> RootItems
        {
            get => _rootItems;
            set
            {
                _rootItems = value;
            }
        }

        private void LoadRootDirectories(string netId)
        {
            string rootDirectory = "/";

            FileSystemItem rootFolder = new FileSystemItem(
                DeviceNetID: Target.NetId,
                Name: rootDirectory,
                IsDirectory: true,
                ParentDirectory: string.Empty,
                FileSize: 0,
                CreationTime: DateTime.Now,
                LastAccessTime: DateTime.Now,
                IsSystemFile: true,
                IsCompressed: false,
                IsRoot: true);

            rootFolder.LoadChildren();
            
            RootItems = new ObservableCollection<FileSystemItem> { rootFolder };
            OnPropertyChanged(nameof(RootItems));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task CopyFile(FileSystemItem sourceFile, FileSystemItem targetFolder)
        {
            AdsFileClient sourceFileClient = new AdsFileClient();
            if (!sourceFileClient.Connect(sourceFile.DeviceNetID)) { return; }

            AdsFileClient destinationFileClient = new AdsFileClient();
            if (!destinationFileClient.Connect(targetFolder.DeviceNetID)) { return; }

            var progressWindow = new CopyProgressWindow(sourceFile, targetFolder);
            var cts = new CancellationTokenSource(); // Erstelle ein CancellationTokenSource
            progressWindow.CancellationRequested += () => cts.Cancel(); // Event für Cancel-Button

            progressWindow.Show();

            var progress = new Progress<double>(value =>
            {
                // Aktualisiere die ProgressBar im ProgressWindow
                progressWindow.SetProgress(value);
            });

            try
            {
                await sourceFileClient.FileCopyAsync($"{sourceFile.ParentDirectory}/{sourceFile.Name}", destinationFileClient, $"{targetFolder.ParentDirectory}/{targetFolder.Name}/{sourceFile.Name}", true, progress, 100, cts.Token); // ToDo: add progress bar
                await Task.Delay(15000);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Task wurde abgebrochen.");
            }
            finally
            {
                progressWindow.Close();
            }
        }
    }


    public class FileSystemItem
    {
        public string DeviceNetID { get; }
        public string Name { get; }
        public bool IsDirectory { get; }
        public string ParentDirectory { get; }
        public bool IsHidden { get; }
        public long FileSize { get; }
        public DateTime CreationTime { get; }
        public DateTime LastAccessTime { get; }
        public bool IsSystemFile { get; }
        public bool IsCompressed { get; }

        public BitmapImage Image { get; }
        public bool IsRoot { get; }

        public ObservableCollection<FileSystemItem> Children { get; set; } = new ObservableCollection<FileSystemItem>();

        public FileSystemItem(string DeviceNetID, string Name, bool IsDirectory, string ParentDirectory, long FileSize, DateTime CreationTime, DateTime LastAccessTime, bool IsSystemFile, bool IsCompressed, bool IsRoot)
        {
            this.DeviceNetID = DeviceNetID;
            this.Name = Name;
            this.ParentDirectory = ParentDirectory;
            this.FileSize = FileSize;
            this.CreationTime = CreationTime;
            this.LastAccessTime = LastAccessTime;
            this.IsSystemFile = IsSystemFile;
            this.IsCompressed = IsCompressed;
            this.IsRoot = IsRoot;
            this.IsDirectory = IsDirectory;
            if (IsRoot)
            {
                this.Image = LoadBitmapImage("Images/harddrive.png");
            }
            else if (IsDirectory)
            {
                if (IsHidden)
                    this.Image = LoadBitmapImage("Images/folderdrive_hidden.png");
                else
                    this.Image = LoadBitmapImage("Images/folderdrive.png");
            }
            else
            {
                if (IsHidden)
                    this.Image = LoadBitmapImage("Images/file_simple_hidden.png");
                else
                    this.Image = LoadBitmapImage("Images/file_simple.png");
            }
        }
        public static string ConvertByteSize(long byteSize)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = byteSize;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:F2} {units[unitIndex]}";
        }


        public void LoadChildren()
        {
            using AdsFileClient fileClient = new();
            fileClient.Connect(DeviceNetID);
            string fullPath = System.IO.Path.Combine(ParentDirectory, Name);
            foreach (var file in fileClient.GetFolderContentStream(fullPath))   // ToDo: Use async version
            {
                FileSystemItem item = new
                (
                    DeviceNetID: this.DeviceNetID,
                    Name: file.fileName,
                    IsDirectory: file.isDirectory,
                    ParentDirectory: fullPath,
                    FileSize: file.fileSize,
                    CreationTime: file.creationTime,
                    LastAccessTime: file.lastAccessTime,
                    IsSystemFile: file.isSystemFile,
                    IsCompressed: file.isCompressed,
                    IsRoot: false
                );
                if (item.IsDirectory)
                {
                    item.Children.Add(null); // Placeholder for Lazy Loading
                }
                Children.Add(item);
            }
        }
        private static BitmapImage LoadBitmapImage(string relativePath)
        {
            var uri = new Uri($"pack://application:,,,/{relativePath}");
            return new BitmapImage(uri);
        }
    }
}
