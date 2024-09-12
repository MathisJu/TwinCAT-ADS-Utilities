using AdsUtilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TwinCAT.Ads;
using TwinCAT.Router;

namespace AdsUtilitiesUI
{
    /// <summary>
    /// Interaction logic for FileHandlingPage.xaml
    /// </summary>
    public partial class FileHandlingPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private StaticRoutesInfo _targetLeft;

        public StaticRoutesInfo TargetLeft
        {
            get => _targetLeft;
            set
            {
                _targetLeft = value;
                OnPropertyChanged();
                ReloadSecondaryRoutes(value.NetId);
                
            }
        }


        private ObservableCollection<StaticRoutesInfo> _SecondaryRoutes;
        public ObservableCollection<StaticRoutesInfo> SecondaryRoutes
        {
            get => _SecondaryRoutes;
            set
            {
                _SecondaryRoutes = value;
                OnPropertyChanged();                
            }
        }

        private StaticRoutesInfo _targetRight;

        public StaticRoutesInfo TargetRight
        {
            get => _targetRight;
            set
            {
                _targetRight = value;
                OnPropertyChanged();      
            }
        }

        public async void ReloadSecondaryRoutes(string netId)
        {

            // Asynchrone Methode aufrufen
            var routes = await AdsHelper.LoadOnlineRoutesAsync(netId);
            SecondaryRoutes = new ObservableCollection<StaticRoutesInfo>();
            foreach (var route in routes)
            {
                SecondaryRoutes.Add(route);
            }
            TargetRight = SecondaryRoutes.ElementAt(0);
        }

        public FileHandlingPage()
        {
            InitializeComponent();
            SecondaryRoutes = new();
            DataContext = this;          
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CmbBx_SeondaryRoute_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbBx_SeondaryRoutes.SelectedItem is StaticRoutesInfo selectedRoute)
            {
                // maybe update something, maybe delete this method
            }
        }
    }
}


