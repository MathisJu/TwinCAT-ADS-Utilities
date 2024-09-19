using AdsUtilitiesUI.ViewModels;
using AdsUtilitiesUI.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AdsUtilitiesUI
{
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
            Page page;
            switch (viewModel)
            {
                case AdsRoutingViewModel _:
                    page = new AdsRoutingPage { DataContext = viewModel };
                    break;
                case FileHandlingViewModel _:
                    page = new FileHandlingPage { DataContext = viewModel };
                    break;
                default:
                    throw new ArgumentException("Unknown ViewModel type");
            }
            return page;
        }
    }
}
