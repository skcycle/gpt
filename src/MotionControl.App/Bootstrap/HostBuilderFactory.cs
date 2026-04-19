using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MotionControl.Application.Interfaces;
using MotionControl.Application.Services;
using MotionControl.Control.Interfaces;
using System.Windows;
using MotionControl.Control.Homing;
using MotionControl.Control.Services;
using MotionControl.Control.StateMachines;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Zmc.Config;
using MotionControl.Device.Zmc.Controllers;
using MotionControl.Device.Zmc.Native;
using MotionControl.Device.Zmc.Translators;
using MotionControl.Diagnostics.Services;
using MotionControl.Domain.Entities;
using MotionControl.Infrastructure.Configuration;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App.Bootstrap;

public static class HostBuilderFactory
{
    public static IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.Configure<ZmcControllerOptions>(context.Configuration.GetSection("ZmcController"));
                services.Configure<AxisMappingOptions>(context.Configuration.GetSection("AxisMapping"));

                var axisMappingOptions = context.Configuration.GetSection("AxisMapping").Get<AxisMappingOptions>() ?? new AxisMappingOptions();
                services.AddSingleton(MachineFactory.CreateDefaultMachine(axisMappingOptions));

                var zmcControllerOptions = context.Configuration.GetSection("ZmcController").Get<ZmcControllerOptions>() ?? new ZmcControllerOptions();
                services.AddSingleton(zmcControllerOptions);
                services.AddSingleton<ZmcStatusTranslator>();
                services.AddSingleton<ZmcAxisNativeFacade>();

                services.AddSingleton<IMotionController, ZmcMotionController>();
                services.AddSingleton<IAxisControlService, AxisControlService>();
                services.AddSingleton<IHomeStrategy, DefaultHomeStrategy>();
                services.AddSingleton<IHomeStrategy, LimitThenIndexHomeStrategy>();
                services.AddSingleton<IHomeStrategy, IndexOnlyHomeStrategy>();
                services.AddSingleton<IHomeStrategy, SlaveFollowMasterHomeStrategy>();
                services.AddSingleton<IHomingService, HomingService>();
                services.AddSingleton<ISystemAppService, SystemAppService>();
                services.AddSingleton<IMotionAppService, MotionAppService>();

                services.AddSingleton<SafetyInterlockService>();
                services.AddSingleton<ControllerRuntimeState>();
                services.AddSingleton<AxisPollingService>();
                services.AddSingleton<IoPollingService>();
                services.AddSingleton<AlarmPollingService>();
                services.AddSingleton<AxisStateMachine>();
                services.AddSingleton<SystemStateMachine>();
                services.AddSingleton<ControllerPollingService>();
                services.AddSingleton<IUiRefreshNotifier>(_ => new DispatcherUiRefreshNotifier(Application.Current.Dispatcher));
                services.AddHostedService<PollingHostedService>();

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }
}
