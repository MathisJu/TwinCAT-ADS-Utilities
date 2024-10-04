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
        _TargetService.OnTargetChanged += ReloadSecondaryRoutes;

        _LoggerService = (LoggerService)loggerService;

    }

    private StaticRoutesInfo _targetRight;

    public StaticRoutesInfo TargetRight
    {
        get => _targetRight;
        set
        {
            _targetRight = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<StaticRoutesInfo> _SecondaryRoutes = new();
    public ObservableCollection<StaticRoutesInfo> SecondaryRoutes
    {
        get => _SecondaryRoutes;
        set
        {
            _SecondaryRoutes = value;
            OnPropertyChanged();
        }
    }

    public void ReloadSecondaryRoutes(object sender, StaticRouteStatus newTarget)
    {
        if (Target is not null)
            _ = ReloadSecondaryRoutes(Target?.NetId);
    }

    public async Task ReloadSecondaryRoutes(string netId)
    {
        var routes = await _TargetService.LoadOnlineRoutesAsync(netId);
        SecondaryRoutes = new ObservableCollection<StaticRoutesInfo>();
        foreach (var route in routes)
        {
            SecondaryRoutes.Add(route);
        }
        TargetRight = SecondaryRoutes.ElementAt(0);
    }

}
