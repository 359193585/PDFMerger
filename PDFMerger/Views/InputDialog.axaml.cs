using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PDFMerger.Views;
public partial class InputDialog : Window
{
    public InputDialog(string message,string notice = "", string title = "Please Input")
    {
        InitializeComponent();

        MessageText.Text = message;
        NoticeText.Text = notice;
        Title = title;

        Opened += (_, _) =>
        {
            PasswordTextBox.Focus();
        };
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(PasswordTextBox.Text);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
