using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using AdsUtilities;
using TwinCAT.Ads;

namespace AdsUtilitiesUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window , INotifyPropertyChanged
{

    public ObservableCollection<StaticRoutesInfo> StaticRoutes { get; set; }

    private StaticRoutesInfo _selectedRoute;
    public StaticRoutesInfo SelectedRoute
    {
        get => _selectedRoute;
        set
        {
            if (_selectedRoute.Name != value.Name)
            {
                _selectedRoute = value;
                OnPropertyChanged();
                fileHandlingPage.TargetLeft = value;
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private AdsRoutingPage adsRoutingPage = new();

    private FileHandlingPage fileHandlingPage = new();

    public MainWindow()
    {
        InitializeComponent();
        

        StaticRoutes = new ObservableCollection<StaticRoutesInfo>();
        DataContext = this;

        InitLogger();

        LoadRoutesAsync();
        
    }

    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl)
        {
            TabItem selectedTab = ((sender as TabControl).SelectedItem as TabItem);
            if (selectedTab != null)
            {
                switch (selectedTab.Header.ToString())
                {
                    case "ADS Routing":
                        if (AdsRoutingFrame.Content == null)
                        {
                            AdsRoutingFrame.Navigate(adsRoutingPage);
                        }
                        break;
                    case "File Handling":
                        if (FileHandlingFrame.Content == null)
                        {
                            FileHandlingFrame.Navigate(fileHandlingPage);
                        }
                        break;
                }
            }
        }
    }

    private void InitLogger()
    {

    }
    



    private async void LoadRoutesAsync()
    {
        using AdsRoutingClient adsRoutingClient = new();
        adsRoutingClient.ConnectLocal();
        List<StaticRoutesInfo> routes = await adsRoutingClient.GetRoutesListAsync();

        StaticRoutes.Clear();

        StaticRoutesInfo localSystem = new()
        {
            NetId = AmsNetId.Local.ToString(),
            Name = "<Local>",
            IpAddress = "0.0.0.0"
        };
        StaticRoutes.Add(localSystem);

        foreach (var route in routes)
        {
            StaticRoutes.Add(route);
        }
        SelectedRoute = localSystem;
        CmbBx_SelectRoute.SelectedItem = localSystem;
    }

    private void CmbBx_SelectRoute_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbBx_SelectRoute.SelectedItem is StaticRoutesInfo selectedRoute)
        {
            // Update ui elements
        }
    }
}

