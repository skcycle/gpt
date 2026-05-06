using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MotionControl.App.Controls;

/// <summary>
/// Shared scroll handler for DataGrid mouse wheel events across panels.
/// </summary>
internal static class ScrollHelper
{
    /// <summary>
    /// Handles PreviewMouseWheel on a DataGrid: scrolls the DataGrid's internal
    /// ScrollViewer first, then falls back to the parent container.
    /// </summary>
    public static void HandleDataGridMouseWheel(object sender, MouseWheelEventArgs e, DependencyObject parent)
    {
        if (sender is not DataGrid dataGrid) return;

        var dataGridScrollViewer = FindVisualChild<ScrollViewer>(dataGrid);
        if (dataGridScrollViewer != null)
        {
            var scrollingDown = e.Delta < 0;
            var scrollingUp = e.Delta > 0;
            var canScrollDown = dataGridScrollViewer.VerticalOffset < dataGridScrollViewer.ScrollableHeight;
            var canScrollUp = dataGridScrollViewer.VerticalOffset > 0;

            if ((scrollingDown && canScrollDown) || (scrollingUp && canScrollUp))
            {
                dataGridScrollViewer.ScrollToVerticalOffset(dataGridScrollViewer.VerticalOffset - e.Delta / 3.0);
                e.Handled = true;
                return;
            }
        }

        var parentScrollViewer = FindVisualChild<ScrollViewer>(parent);
        if (parentScrollViewer != null)
        {
            parentScrollViewer.ScrollToVerticalOffset(parentScrollViewer.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }

    public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;

            var grandChild = FindVisualChild<T>(child);
            if (grandChild != null) return grandChild;
        }
        return null;
    }
}
