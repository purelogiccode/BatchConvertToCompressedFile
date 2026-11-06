using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace BatchConvertToCompressedFile.Views;

/// <summary>Button sets supported by <see cref="MessageDialog"/>.</summary>
internal enum MessageDialogButtons
{
    /// <summary>Show a single OK button.</summary>
    Ok,

    /// <summary>Show Yes and No buttons.</summary>
    YesNo
}

/// <summary>Severity icon shown by <see cref="MessageDialog"/>.</summary>
internal enum MessageDialogIcon
{
    /// <summary>Informational message.</summary>
    Information,

    /// <summary>Warning message.</summary>
    Warning,

    /// <summary>Error message.</summary>
    Error
}

/// <summary>A themed modal message dialog used instead of platform-specific message boxes.</summary>
internal partial class MessageDialog : Window
{
    private MessageDialog()
    {
        InitializeComponent();
    }

    /// <summary>Shows a modal dialog and waits for the user's choice.</summary>
    /// <param name="owner">Owner window.</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Dialog message.</param>
    /// <param name="buttons">Button set.</param>
    /// <param name="icon">Severity icon.</param>
    /// <returns><see langword="true"/> for OK or Yes; <see langword="false"/> for No.</returns>
    public static async Task<bool> ShowAsync(
        Window owner,
        string title,
        string message,
        MessageDialogButtons buttons,
        MessageDialogIcon icon)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var dialog = new MessageDialog
        {
            Title = title
        };

        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        dialog.NoButton.IsVisible = buttons == MessageDialogButtons.YesNo;
        dialog.OkButton.Content = buttons == MessageDialogButtons.YesNo ? "Yes" : "OK";
        ToolTip.SetTip(
            dialog.OkButton,
            buttons == MessageDialogButtons.YesNo ? "Confirm and continue" : "Close this dialog");

        switch (icon)
        {
            case MessageDialogIcon.Warning:
                dialog.IconText.Text = "!";
                dialog.IconText.Foreground = new SolidColorBrush(Color.Parse("#FBBF24"));
                break;
            case MessageDialogIcon.Error:
                dialog.IconText.Text = "×";
                dialog.IconText.Foreground = new SolidColorBrush(Color.Parse("#F87171"));
                break;
            default:
                dialog.IconText.Text = "i";
                dialog.IconText.Foreground = new SolidColorBrush(Color.Parse("#6366F1"));
                break;
        }

        return await dialog.ShowDialog<bool>(owner);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnNoClick(object? sender, RoutedEventArgs e) => Close(false);
}
