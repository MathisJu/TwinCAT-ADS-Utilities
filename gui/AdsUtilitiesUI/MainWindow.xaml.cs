using System.Collections.ObjectModel;
using System.ComponentModel;
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
using AdsUtilities;

namespace AdsUtilitiesUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public ObservableCollection<StaticRoutesInfo> StaticRoutes { get; set; }
    

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        StaticRoutes = new ObservableCollection<StaticRoutesInfo>();
        

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
                            AdsRoutingFrame.Navigate(new AdsRoutingPage());
                        }
                        break;
                    case "File Handling":
                        if (FileHandlingFrame.Content == null)
                        {
                            FileHandlingFrame.Navigate(new FileHandlingPage());
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

        foreach (var route in routes)
        {
            StaticRoutes.Add(route);
        }
    }

    private void CmbBx_SelectRoute_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

}

