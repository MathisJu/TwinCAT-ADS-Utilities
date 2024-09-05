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
using System.Windows.Automation;
using TwinCAT.Ads;

namespace AdsUtilitiesUI
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel() 
        {

        }

        public async void MainWindow_Initilaize(object sender, RoutedEventArgs e)
        {
            await Reload_Routes();
        }

        public AdsRoutingPage adsRoutingPage = new();

        public FileHandlingPage fileHandlingPage = new();


        public async Task Reload_Routes()
        {
            // Asynchrone Methode aufrufen
            var routes = await AdsHelper.LoadOnlineRoutesAsync(AmsNetId.Local.ToString());
            StaticRoutes = new ObservableCollection<StaticRoutesInfo>();
            foreach (var route in routes)
            {
                StaticRoutes.Add(route);
            }
            SelectedRoute = routes.ElementAt(0);
        }

        private ObservableCollection<StaticRoutesInfo> _StaticRoutes;
        public ObservableCollection<StaticRoutesInfo> StaticRoutes
        {
            get => _StaticRoutes;
            set
            {
                _StaticRoutes = value;
                OnPropertyChanged();
            }
        }

        private StaticRoutesInfo _selectedRoute;
        public StaticRoutesInfo SelectedRoute
        {
            get => _selectedRoute;
            set
            {
                _selectedRoute = value;
                OnPropertyChanged();
                fileHandlingPage.TargetLeft = value;     
                adsRoutingPage._viewModel.Target = value;   // ToDo: Rework this
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        
    }
}
