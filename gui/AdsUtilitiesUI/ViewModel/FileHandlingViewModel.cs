using AdsUtilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdsUtilitiesUI.ViewModels;

class FileHandlingViewModel : ViewModelTargetAccessPage
{

    public FileHandlingViewModel(TargetService targetService, ILoggerService loggerService)
    {
        _TargetService = targetService;    
        InitTargetAccess(_TargetService);

        // Set secondary route to local as soon as target service has loaded all routes
        _TargetService.OnTargetChanged += (sender, e) =>
        {
            if (SecondaryTarget is null)
            {
                SecondaryTarget = _TargetService.CurrentTarget;
            }
            _TargetService.OnTargetChanged -= InitSecondaryRoute;
        };
        
        _LoggerService = (LoggerService)loggerService;

    }

    private void InitSecondaryRoute(object? sender, StaticRouteStatus e)
    {
        if(SecondaryTarget is null)
        {
            SecondaryTarget = _TargetService.CurrentTarget;
        }
    }

    private StaticRoutesInfo _SecondaryTarget;

    public StaticRoutesInfo SecondaryTarget
    {
        get => _SecondaryTarget;
        set
        {
            _SecondaryTarget = value;
            OnPropertyChanged();
        }
    }

}
