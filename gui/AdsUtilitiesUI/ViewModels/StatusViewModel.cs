using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AdsUtilitiesUI
{
    public class StatusViewModel : INotifyPropertyChanged
    {
        private string _message;
        private string _icon;
        private bool _isVisible;

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }

        private void ShowMessage(string message, string icon)
        {
            Message = message;
            Icon = icon;
            IsVisible = true;

            // Optional: Timer to hide the message after a few seconds
            Task.Delay(3000).ContinueWith(_ => IsVisible = false);
        }

        public void ShowSuccess(string message)
        {
            ShowMessage(message, "success_icon_path.png");
        }

        public void ShowError(string message)
        {
            ShowMessage(message, "error_icon_path.png");
        }

        public void ShowInfo(string message)
        {
            ShowMessage(message, "info_icon_path.png");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
