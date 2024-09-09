using System;
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
using System.Windows.Media.Animation;
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

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _viewModel.AddRouteSelection.Password = passwordBox.Password;
            }
        }

        private async void AddRouteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.AddRouteSelection.AllParametersProvided())
            {
                await _viewModel.AddRoute();
                return;
            }
            var textBoxes = new[] { InputName, InputNetid, InputIp, InputHostname, InputNameRemote, InputUsername };
            var passwordBoxes = new[] { InputPassword };

            // Überprüfen, ob alle TextBoxen Eingaben haben
            var missingTextInputs = textBoxes.Where(tb => string.IsNullOrWhiteSpace(tb.Text)).ToList();

            // Überprüfen, ob die PasswordBox Eingaben hat
            var missingPasswordInputs = passwordBoxes.Where(pb => string.IsNullOrWhiteSpace(pb.Password)).ToList();

            // Fehlende Eingaben hervorheben
            if (missingTextInputs.Any() || missingPasswordInputs.Any())
            {
                foreach (var textBox in missingTextInputs)
                {
                    HighlightControl(textBox);
                }

                foreach (var passwordBox in missingPasswordInputs)
                {
                    HighlightControl(passwordBox);
                }
            }
        }
        private void HighlightControl(Control control)
        {
            // Highlight control by changing background color
            control.Background = new SolidColorBrush(Colors.Pink);

            // Animation that resets color
            var animation = new ColorAnimation
            {
                To = Colors.Transparent,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                AutoReverse = false
            };
            control.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
        

    }
}
