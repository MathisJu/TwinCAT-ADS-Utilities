using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AdsUtilitiesUI
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        // Event, das ausgelöst wird, wenn sich eine Property ändert
        public event PropertyChangedEventHandler PropertyChanged;

        // Methode, um die PropertyChanged-Events auszulösen
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Methode, um den Wert einer Property zu setzen und das PropertyChanged-Event auszulösen
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
