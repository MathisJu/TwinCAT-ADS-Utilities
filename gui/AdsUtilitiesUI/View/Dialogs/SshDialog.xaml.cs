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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AdsUtilitiesUI
{
    /// <summary>
    /// Interaction logic for SshDialog.xaml
    /// </summary>
    public partial class SshDialog : Window
    {
        public bool AddSshKey { get; set; } = false;

        public SshDialog(string title, string prompt, string defaultResponse = "")
        {
            InitializeComponent();
            DataContext = this;
            Title = title;
            PromptText.Text = prompt;
            ResponseTextBox.Text = defaultResponse;
        }

        public string ResponseText
        {
            get { return ResponseTextBox.Text; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
