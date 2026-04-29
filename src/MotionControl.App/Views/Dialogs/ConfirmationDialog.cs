using System.Windows;

namespace MotionControl.App.Views.Dialogs;

/// <summary>
/// 确认框（Yes/No 或 Yes/No/Cancel）
/// </summary>
public class ConfirmationDialog : DialogWindow
{
    public ConfirmationDialog(string message, string title = "确认", DialogIcon icon = DialogIcon.Warning, DialogButton buttons = DialogButton.YesNo)
    {
        Title = title;
        Message = message;
        Icon = icon;
        Buttons = buttons;
    }

    public static MessageBoxResult Show(string message, string title = "确认", DialogIcon icon = DialogIcon.Warning, DialogButton buttons = DialogButton.YesNo)
    {
        var dialog = new ConfirmationDialog(message, title, icon, buttons);
        dialog.ShowDialog();
        return dialog.DialogResult ?? MessageBoxResult.Cancel;
    }
}
