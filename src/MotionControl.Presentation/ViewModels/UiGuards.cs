using System.Windows;

namespace MotionControl.Presentation.ViewModels;

internal static class UiGuards
{
    public static bool Confirm(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
}
