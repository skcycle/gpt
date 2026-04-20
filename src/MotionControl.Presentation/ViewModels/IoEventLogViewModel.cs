using System.Collections.ObjectModel;
using MotionControl.Control.Services;

namespace MotionControl.Presentation.ViewModels;

public sealed class IoEventLogViewModel
{
    private readonly CommandFeedbackRuntimeState _commandFeedbackRuntimeState;
    private IoEventItemViewModel[] _lastEvents = Array.Empty<IoEventItemViewModel>();

    public IoEventLogViewModel(CommandFeedbackRuntimeState commandFeedbackRuntimeState)
    {
        _commandFeedbackRuntimeState = commandFeedbackRuntimeState;
        Events = new ObservableCollection<IoEventItemViewModel>();
    }

    public ObservableCollection<IoEventItemViewModel> Events { get; }

    public void Refresh()
    {
        var ioEvents = _commandFeedbackRuntimeState.RecentFeedback
            .Where(f => f.CommandName is "DI" or "DO")
            .Select(f => new IoEventItemViewModel(f))
            .ToArray();

        if (!ioEvents.SequenceEqual(_lastEvents))
        {
            Events.Clear();
            foreach (var evt in ioEvents.TakeLast(30))
            {
                Events.Add(evt);
            }

            _lastEvents = ioEvents;
        }
    }
}
