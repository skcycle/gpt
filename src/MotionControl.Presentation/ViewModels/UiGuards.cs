using System.Windows;
using MotionControl.Presentation.Dialogs;

namespace MotionControl.Presentation.ViewModels;

internal static class UiGuards
{
    public static bool Confirm(string title, string message)
        => DialogService.Instance.Confirm(message, title) == MessageBoxResult.Yes;
}
