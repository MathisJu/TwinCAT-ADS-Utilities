using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

namespace AdsUtilitiesUI
{
    /// <summary>
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        public PropertiesWindow(FileSystemItem file)
        {
            InitializeComponent();
            
            Title = $"{file.Name} Properties";
            Icon = file.Image;

            // ToDo: Add further file details (expand FileProperty class)
            FileProperties = new ObservableCollection<FileProperty>
            {
                new() { Property = "Name",          Value = file.Name },
                new() { Property = "Location",      Value = file.ParentDirectory },
                new() { Property = "Size",          Value = FileSystemItem.ConvertByteSize(file.FileSize) }, 
                new() { Property = "Date created",  Value = file.CreationTime.ToString() },
                new() { Property = "Date accessed", Value = file.LastAccessTime.ToString() }
            };

            DataContext = this;
        }

        public ObservableCollection<FileProperty> FileProperties { get; set; }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class FileProperty
    {
        public string Property { get; set; }
        public string Value { get; set; }
    }
}
