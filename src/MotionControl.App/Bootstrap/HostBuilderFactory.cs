using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MotionControl.Application.Interfaces;
using MotionControl.Application.Services;
using MotionControl.Control.Interfaces;
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

                var axisMappingOptions = new AxisMappingOptions();
                context.Configuration.GetSection("AxisMapping").Bind(axisMappingOptions);
                services.AddSingleton(MachineFactory.CreateDefaultMachine(axisMappingOptions));

                var zmcControllerOptions = new ZmcControllerOptions();
                context.Configuration.GetSection("ZmcController").Bind(zmcControllerOptions);
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
                services.AddSingleton<IAxisParameterAppService>(_ => new AxisParameterAppService(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));

                services.AddSingleton<SafetyInterlockService>();
                services.AddSingleton<ControllerRuntimeState>();
                services.AddSingleton<HomePlanRuntimeState>();
                services.AddSingleton<CommandFeedbackRuntimeState>();
                services.AddSingleton<FaultRecoveryService>();
                services.AddSingleton<AxisPollingService>();
                services.AddSingleton<IoPollingService>();
                services.AddSingleton<AlarmPollingService>();
                services.AddSingleton<AxisStateMachine>();
                services.AddSingleton<SystemStateMachine>();
                services.AddSingleton<ControllerPollingService>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MotionControl.Control.Interfaces.IUiRefreshNotifier>(serviceProvider =>
                    new DispatcherUiRefreshNotifier(serviceProvider.GetRequiredService<MainWindowViewModel>().RefreshViewModels));
                services.AddHostedService<PollingHostedService>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }
}
