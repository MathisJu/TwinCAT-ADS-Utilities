using AdsUtilitiesUI.ViewModels;
using AdsUtilitiesUI.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AdsUtilitiesUI;

public class TabViewModel : ViewModelBase
{
    public string Title { get; }
    public Page Page { get; }

    public TabViewModel(string title, ViewModelBase viewModel)
    {
        Title = title;
        Page = CreatePage(viewModel);
    }

    private Page CreatePage(ViewModelBase viewModel)
    {
        Page page = viewModel switch
        {
            AdsRoutingViewModel _ => new AdsRoutingPage { DataContext = viewModel },
            FileHandlingViewModel _ => new FileHandlingPage { DataContext = viewModel },
            DeviceInfoViewModel _ => new DeviceInfoPage { DataContext = viewModel },

            _ => throw new ArgumentException("Unknown ViewModel type"),
        };
        return page;
    }
}
