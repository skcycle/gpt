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

        // Use length comparison to avoid SequenceEqual false negatives on same-content different-reference arrays
        var newLength = ioEvents.Length;
        var lastLength = _lastEvents.Length;
        if (newLength != lastLength || (newLength > 0 && (lastLength == 0 || !ioEvents.SequenceEqual(_lastEvents))))
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
