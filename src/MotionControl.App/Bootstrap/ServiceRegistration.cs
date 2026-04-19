using MotionControl.Application.Interfaces;
using MotionControl.Application.Services;
using MotionControl.Control.Interfaces;
using MotionControl.Control.Services;
using MotionControl.Device.Abstractions.Controllers;
using MotionControl.Device.Zmc.Config;
using MotionControl.Device.Zmc.Controllers;
using MotionControl.Device.Zmc.Translators;
using MotionControl.Domain.Entities;
using MotionControl.Domain.ValueObjects;
using MotionControl.Presentation.ViewModels;

namespace MotionControl.App.Bootstrap;

public static class ServiceRegistration
{
    public static MainWindowViewModel BuildMainWindowViewModel()
    {
        var axes = Enumerable.Range(1, 32)
            .Select(axisNo => new Axis(new AxisId(axisNo), $"Axis {axisNo}", axisNo))
            .ToArray();

        var machine = new Machine(axes, Array.Empty<AxisGroup>());

        var zmcOptions = new ZmcControllerOptions();
        var statusTranslator = new ZmcStatusTranslator();
        IMotionController motionController = new ZmcMotionController(zmcOptions, statusTranslator);

        IAxisControlService axisControlService = new AxisControlService(motionController);
        IHomingService homingService = new HomingService(motionController);
        var pollingService = new ControllerPollingService(motionController, machine);
        IMotionAppService motionAppService = new MotionAppService(axisControlService, homingService, machine);
        ISystemAppService systemAppService = new SystemAppService(machine, pollingService);

        return new MainWindowViewModel(machine, systemAppService, motionAppService);
    }
}
