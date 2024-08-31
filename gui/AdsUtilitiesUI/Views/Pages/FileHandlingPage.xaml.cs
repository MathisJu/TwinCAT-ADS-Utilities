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
                if (_targetLeft.Name != value.Name)
                {
                    _targetLeft = value;
                    OnPropertyChanged();
                    ReloadSecondaryRoutes(value.NetId);
                }
            }
        }    


        public ObservableCollection<StaticRoutesInfo> SecondaryRoutes { get; set; }
        private StaticRoutesInfo _targetRight;

        public StaticRoutesInfo TargetRight
        {
            get => _targetRight;
            set
            {
                if (_targetRight.Name != value.Name)
                {
                    _targetRight = value;
                    OnPropertyChanged();
                }
            }
        }

        public async void ReloadSecondaryRoutes(string netId)
        {
            using AdsRoutingClient routingClient = new ();
            routingClient.Connect (netId);
            var routes = await routingClient.GetRoutesListAsync();
            SecondaryRoutes.Clear();

            StaticRoutesInfo localSystem = new()
            {
                NetId = AmsNetId.Local.ToString(),
                Name = "<Local>",
                IpAddress = "0.0.0.0"
            };
            SecondaryRoutes.Add(localSystem);

            foreach (var route in routes)
            {
                SecondaryRoutes.Add(route);
            }
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


