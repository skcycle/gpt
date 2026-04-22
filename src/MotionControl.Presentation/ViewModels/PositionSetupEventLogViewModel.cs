using System.Collections.ObjectModel;
using MotionControl.Control.Services;

namespace MotionControl.Presentation.ViewModels;

public sealed class PositionSetupEventLogViewModel
{
    private readonly PositionSetupEventRuntimeState _positionSetupEventRuntimeState;
    private PositionSetupEventItemViewModel[] _lastEvents = Array.Empty<PositionSetupEventItemViewModel>();

    public PositionSetupEventLogViewModel(PositionSetupEventRuntimeState positionSetupEventRuntimeState)
    {
        _positionSetupEventRuntimeState = positionSetupEventRuntimeState;
        Events = new ObservableCollection<PositionSetupEventItemViewModel>();
        _positionSetupEventRuntimeState.EventsChanged += Refresh;
    }

    public ObservableCollection<PositionSetupEventItemViewModel> Events { get; }

    public void Refresh()
    {
        var events = _positionSetupEventRuntimeState.RecentEvents
            .Select(item => new PositionSetupEventItemViewModel(item))
            .ToArray();

        if (events.Length != _lastEvents.Length || (events.Length > 0 && (_lastEvents.Length == 0 || !events.SequenceEqual(_lastEvents))))
        {
            Events.Clear();
            foreach (var item in events.TakeLast(30))
            {
                Events.Add(item);
            }

            _lastEvents = events;
        }
    }
}
