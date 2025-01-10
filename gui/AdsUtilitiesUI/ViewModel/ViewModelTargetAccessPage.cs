using AdsUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdsUtilitiesUI.ViewModels
{
    public abstract class ViewModelTargetAccessPage : ViewModelBase // A ViewModel template for all pages that access target access functions
    {
        public TargetService _TargetService
        {
            get; set;
        }

        public LoggerService _LoggerService
        {
            get; set;
        }

        private void UpdateTarget(object sender, StaticRoutesInfo newTarget)
        {
            Target = newTarget; // Update target on change
        }

        public void Dispose()
        {
            _TargetService.OnTargetChanged -= UpdateTarget;
        }

        private StaticRoutesInfo? _Target;

        public StaticRoutesInfo? Target
        {
            get => _Target;
            set
            {
                _Target = value;
                OnPropertyChanged();

            }
        }

        public void InitTargetAccess(TargetService TargetSetvice)
        {
            _TargetService.OnTargetChanged += UpdateTarget;
            Target = _TargetService.CurrentTarget;

        }
    }
}
