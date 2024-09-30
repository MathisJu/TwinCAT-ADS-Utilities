using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace AdsUtilitiesUI.Views.Windows
{
    /// <summary>
    /// Interaction logic for CopyProgressWindow.xaml
    /// </summary>
    public partial class CopyProgressWindow : Window, INotifyPropertyChanged
    {
        public event Action CancellationRequested;

        public FileSystemItem SourceFile { get; private set; }

        public FileSystemItem DestinationFolder { get; private set; }

        public string CopyFileMessage 
        { 
            get
            {
                return $"Copying {FileSystemItem.ConvertByteSize(SourceFile.FileSize)} from {SourceFile.DeviceNetID} to {DestinationFolder.DeviceNetID}";
            }
        }

        public string ProgressMessage { get; set; }

        public CopyProgressWindow(FileSystemItem sourceFile, FileSystemItem destinationFolder)
        {
            InitializeComponent();
            Title = sourceFile.Name;
            Icon = sourceFile.Image;    // ToDo: Use a Clock or some kind of copying-image
            SourceFile = sourceFile ;
            DestinationFolder = destinationFolder ;
            DataContext = this;
        }

        // Methode zum Setzen des Fortschritts der ProgressBar
        public void SetProgress(double value)
        {
            progressBar.Value = value;
            ProgressMessage = $"{(int)value}% complete";
            OnPropertyChanged(nameof(ProgressMessage));
        }

        // Event-Handler für den "Cancel" Button
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancellationRequested?.Invoke(); // Löst das Abbruch-Event aus
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
