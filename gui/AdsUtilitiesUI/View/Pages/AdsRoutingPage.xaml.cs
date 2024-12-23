using AdsUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        public AdsRoutingPage()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && this.DataContext != null)
            {
                ((AdsRoutingViewModel)this.DataContext).AddRouteSelection.Password = ((PasswordBox)sender).Password;    // ToDo: Find a solution that does not violate MVVM
            }
        }

        // This is meant to highlight a textbox / passwordbox if input is missing
        private static void HighlightControl(Control control, Color highlightBackgroundColor, int durationMilliseconds)
        {
            if (control.Tag is bool isAnimating && isAnimating)
            {
                // Do not start a new animation if it is already running
                return;
            }

            // Highlight control by changing background color
            if (control.Background is SolidColorBrush originalBrush)
            {
                // Use Tag to indicate that animation is running
                control.Tag = true;
                var originalColor = originalBrush.Color;

                var highlightBrush = new SolidColorBrush(highlightBackgroundColor);
                control.Background = highlightBrush;

                // Animation that resets color
                var animation = new ColorAnimation
                {
                    To = originalColor,
                    Duration = new Duration(TimeSpan.FromMilliseconds(durationMilliseconds)),
                    AutoReverse = false
                };

                animation.Completed += (s, e) =>
                {
                    // Reset flag when animation is finished
                    control.Tag = false;
                };

                highlightBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
        }

        private void AddRouteButton_Click(object sender, RoutedEventArgs e)
        {
            Span<Control> inputControls = [InputName, InputNetid, InputIp, InputHostname, InputNameRemote, InputUsername, InputPassword];
            foreach (Control inputControl in inputControls)
            {
                if ((inputControl is TextBox inputTextBox && inputTextBox.Text == string.Empty)
                    || (inputControl is PasswordBox inputPassword && string.IsNullOrEmpty(inputPassword.Password)))
                {
                    HighlightControl(inputControl, Colors.LightPink, 750);
                }
            }
        }

    }
}
