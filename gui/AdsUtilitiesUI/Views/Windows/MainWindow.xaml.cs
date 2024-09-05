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
public partial class MainWindow : Window 
{
    private MainWindowViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;
        Loaded += _viewModel.MainWindow_Initilaize;
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
                            AdsRoutingFrame.Navigate(_viewModel.adsRoutingPage);
                        }
                        break;
                    case "File Handling":
                        if (FileHandlingFrame.Content == null)
                        {
                            FileHandlingFrame.Navigate(_viewModel.fileHandlingPage);
                        }
                        break;
                }
            }
        }
    }

    private void CmbBx_SelectRoute_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (CmbBx_SelectRoute.SelectedItem is StaticRoutesInfo selectedRoute)
        {
            // Update ui elements
        }

    }
    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        // Aufruf der Refresh-Methode im ViewModel
        _viewModel?.Reload_Routes();
    }
}

