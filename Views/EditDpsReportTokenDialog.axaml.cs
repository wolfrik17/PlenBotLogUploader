using Avalonia.Controls;
using Avalonia.Interactivity;
using PlenBotLogUploader.AppSettings;

namespace PlenBotLogUploader.Views;

public partial class EditDpsReportTokenDialog : Window
{
    private readonly ApplicationSettingsUploadUserToken _token;

    public ApplicationSettingsUploadUserToken Result { get; private set; }

    public EditDpsReportTokenDialog()
    {
        InitializeComponent();
    }

    internal EditDpsReportTokenDialog(ApplicationSettingsUploadUserToken token) : this()
    {
        _token = token ?? new ApplicationSettingsUploadUserToken();

        TextBoxName.Text = _token.Name;
        TextBoxToken.Text = _token.UserToken;
        CheckBoxActive.IsChecked = _token.Active;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _token.Name = TextBoxName.Text?.Trim() ?? "User Token";
        _token.UserToken = TextBoxToken.Text?.Trim() ?? "";
        _token.Active = CheckBoxActive.IsChecked ?? true;

        Result = _token;
        Close(Result);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close(null);
    }
}
