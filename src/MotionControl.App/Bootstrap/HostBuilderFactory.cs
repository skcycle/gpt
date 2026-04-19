using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MotionControl.Application.Interfaces;
using MotionControl.Application.Services;
using MotionControl.Control.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Zmc.Config;
using MotionControl.Device.Zmc.Controllers;
using MotionControl.Device.Zmc.Native;
using MotionControl.Device.Zmc.Translators;
using MotionControl.Diagnostics.Services;
using MotionControl.Domain.Entities;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App.Bootstrap;

public static class HostBuilderFactory
{
    public static IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(MachineFactory.CreateDefaultMachine());
                services.AddSingleton<ZmcControllerOptions>();
                services.AddSingleton<ZmcStatusTranslator>();
                services.AddSingleton<ZmcAxisNativeFacade>();

                services.AddSingleton<IMotionController, ZmcMotionController>();
                services.AddSingleton<IAxisControlService, AxisControlService>();
                services.AddSingleton<IHomingService, HomingService>();
                services.AddSingleton<ISystemAppService, SystemAppService>();
                services.AddSingleton<IMotionAppService, MotionAppService>();

                services.AddSingleton<SafetyInterlockService>();
                services.AddSingleton<ControllerPollingService>();
                services.AddSingleton<PollingHostedService>();

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }
}
