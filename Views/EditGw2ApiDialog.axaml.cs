using Avalonia.Controls;
using Avalonia.Interactivity;
using PlenBotLogUploader.AppSettings;

namespace PlenBotLogUploader.Views;

public partial class EditGw2ApiDialog : Window
{
    private readonly ApplicationSettingsGw2Api _api;

    public ApplicationSettingsGw2Api Result { get; private set; }

    public EditGw2ApiDialog()
    {
        InitializeComponent();
    }

    internal EditGw2ApiDialog(ApplicationSettingsGw2Api api) : this()
    {
        _api = api ?? new ApplicationSettingsGw2Api();

        TextBoxName.Text = _api.Name;
        TextBoxApiKey.Text = _api.ApiKey;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _api.Name = TextBoxName.Text?.Trim() ?? "GW2 API Key";
        _api.ApiKey = TextBoxApiKey.Text?.Trim() ?? "";

        Result = _api;
        Close(Result);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close(null);
    }
}
