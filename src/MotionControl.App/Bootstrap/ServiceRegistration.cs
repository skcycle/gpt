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
using MotionControl.Domain.ValueObjects;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App.Bootstrap;

public static class ServiceRegistration
{
    public static ApplicationContext BuildApplicationContext()
    {
        var axes = Enumerable.Range(1, 32)
            .Select(axisNo => new Axis(new AxisId(axisNo), $"Axis {axisNo}", axisNo))
            .ToArray();

        var ioPoints = Enumerable.Range(0, 16)
            .Select(index => new IoPoint($"DI_{index}", index, false))
            .Concat(Enumerable.Range(0, 16).Select(index => new IoPoint($"DO_{index}", index, true)))
            .ToArray();

        var machine = new Machine(axes, Array.Empty<AxisGroup>(), ioPoints, Array.Empty<Alarm>());

        var zmcOptions = new ZmcControllerOptions();
        var statusTranslator = new ZmcStatusTranslator();
        var axisNativeFacade = new ZmcAxisNativeFacade();
        IMotionController motionController = new ZmcMotionController(zmcOptions, statusTranslator, axisNativeFacade);

        IAxisControlService axisControlService = new AxisControlService(motionController);
        IHomingService homingService = new HomingService(motionController);
        var pollingService = new ControllerPollingService(motionController, machine);
        _ = new SafetyInterlockService();
        IMotionAppService motionAppService = new MotionAppService(axisControlService, homingService, machine);
        ISystemAppService systemAppService = new SystemAppService(machine, pollingService);
        var mainWindowViewModel = new MainWindowViewModel(machine, systemAppService, motionAppService);
        var pollingHostedService = new PollingHostedService(pollingService, mainWindowViewModel);

        return new ApplicationContext(mainWindowViewModel, pollingHostedService);
    }
}
