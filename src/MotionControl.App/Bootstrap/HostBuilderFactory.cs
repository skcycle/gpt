using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MotionControl.App.Services;
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
using MotionControl.Presentation.Dialogs;
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
                services.Configure<IoMappingOptions>(context.Configuration.GetSection("IoMapping"));
                services.Configure<CylinderMappingOptions>(context.Configuration.GetSection("CylinderMapping"));
                services.Configure<WorkHeadMappingOptions>(context.Configuration.GetSection("WorkHeadMapping"));
                services.Configure<PositionSetupMappingOptions>(context.Configuration.GetSection("PositionSetupMapping"));

                var axisMappingOptions = new AxisMappingOptions();
                context.Configuration.GetSection("AxisMapping").Bind(axisMappingOptions);
                var ioMappingOptions = new IoMappingOptions();
                context.Configuration.GetSection("IoMapping").Bind(ioMappingOptions);
                var cylinderMappingOptions = new CylinderMappingOptions();
                context.Configuration.GetSection("CylinderMapping").Bind(cylinderMappingOptions);
                var workHeadMappingOptions = new WorkHeadMappingOptions();
                context.Configuration.GetSection("WorkHeadMapping").Bind(workHeadMappingOptions);
                services.AddSingleton(MachineFactory.CreateDefaultMachine(axisMappingOptions, ioMappingOptions.Points, cylinderMappingOptions.Cylinders, workHeadMappingOptions.WorkHeads));

                var zmcControllerOptions = new ZmcControllerOptions();
                context.Configuration.GetSection("ZmcController").Bind(zmcControllerOptions);
                services.AddSingleton(zmcControllerOptions);
                services.AddSingleton<ZmcStatusTranslator>();
                services.AddSingleton<ZmcAxisNativeFacade>();
                services.AddSingleton<IEtherCatStatusProvider, ZmcPlaceholderEtherCatStatusProvider>();

                services.AddSingleton<ZmcMotionController>();
                services.AddSingleton<IAxisMotionController>(sp => sp.GetRequiredService<ZmcMotionController>());
                services.AddSingleton<IIoController>(sp => sp.GetRequiredService<ZmcMotionController>());
                services.AddSingleton<IEtherCatController>(sp => sp.GetRequiredService<ZmcMotionController>());
                services.AddSingleton<ISafetyController>(sp => sp.GetRequiredService<ZmcMotionController>());
                services.AddSingleton<IAxisControlService, AxisControlService>();
                services.AddSingleton<IHomeStrategy, DefaultHomeStrategy>();
                services.AddSingleton<IHomeStrategy, LimitThenIndexHomeStrategy>();
                services.AddSingleton<IHomeStrategy, IndexOnlyHomeStrategy>();
                services.AddSingleton<IHomeStrategy, SlaveFollowMasterHomeStrategy>();
                services.AddSingleton<IHomingService, HomingService>();
                services.AddSingleton<ISystemAppService, SystemAppService>();
                services.AddSingleton<MotionControl.Presentation.Dialogs.IDialogService>(_ => new MotionControl.App.Services.DialogService());
                services.AddSingleton<IMotionAppService, MotionAppService>();
                services.AddSingleton<IAxisRuntimeParameterSyncService, AxisRuntimeParameterSyncService>();
                services.AddSingleton<IIoRuntimeSyncService, IoRuntimeSyncService>();
                services.AddSingleton<IAxisControllerParameterAppService, AxisControllerParameterAppService>();
                services.AddSingleton<IAxisParameterAppService>(_ => new AxisParameterAppService(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));
                services.AddSingleton<IAxisManagementAppService, AxisManagementAppService>();
                services.AddSingleton<IIoConfigAppService>(_ => new IoConfigAppService(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));
                services.AddSingleton<IIoManagementAppService, IoManagementAppService>();
                services.AddSingleton<ICylinderRuntimeSyncService, CylinderRuntimeSyncService>();
                services.AddSingleton<ICylinderConfigAppService>(_ => new CylinderConfigAppService(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));
                services.AddSingleton<ICylinderManagementAppService, CylinderManagementAppService>();
                services.AddSingleton<IWorkHeadRuntimeSyncService, WorkHeadRuntimeSyncService>();
                services.AddSingleton<IWorkHeadConfigAppService>(_ => new WorkHeadConfigAppService(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));
                services.AddSingleton<IWorkHeadManagementAppService, WorkHeadManagementAppService>();
                services.AddSingleton<IPositionSetupConfigAppService>(_ => new PositionSetupConfigAppService(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));
                services.AddSingleton<IPositionSetupManagementAppService, PositionSetupManagementAppService>();

                services.AddSingleton<SafetyInterlockService>();
                services.AddSingleton<ControllerRuntimeState>();
                services.AddSingleton<HomePlanRuntimeState>();
                services.AddSingleton<CommandFeedbackRuntimeState>();
                services.AddSingleton<IoEventRuntimeState>();
                services.AddSingleton<CylinderEventRuntimeState>();
                services.AddSingleton<WorkHeadEventRuntimeState>();
                services.AddSingleton<PositionSetupEventRuntimeState>();
                services.AddSingleton<FaultRecoveryService>();
                services.AddSingleton<AxisPollingService>();
                services.AddSingleton<IoPollingService>();
                services.AddSingleton<IoControlService>();
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
