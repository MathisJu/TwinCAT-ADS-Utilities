﻿using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AdsUtilitiesUI
{
    /// <summary>
    /// Interaction logic for AdsRoutingPage.xaml
    /// </summary>
    public partial class AdsRoutingPage : Page
    {
        public AdsRoutingViewModel _viewModel = new();

        public AdsRoutingPage()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        private async void BroadcastBttn_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.Broadcast();
        }
    }
}
