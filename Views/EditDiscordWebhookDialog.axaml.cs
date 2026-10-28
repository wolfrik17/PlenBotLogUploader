using Avalonia.Controls;
using Avalonia.Interactivity;
using PlenBotLogUploader.DiscordApi;
using PlenBotLogUploader.Services;
using System;

namespace PlenBotLogUploader.Views;

public partial class EditDiscordWebhookDialog : Window
{
    private readonly DiscordWebhookData _data;

    public DiscordWebhookData Result { get; private set; }

    public EditDiscordWebhookDialog()
    {
        InitializeComponent();
    }

    internal EditDiscordWebhookDialog(DiscordWebhookData data) : this()
    {
        _data = data ?? new DiscordWebhookData();

        TextBoxName.Text = _data.Name;
        TextBoxUrl.Text = _data.Url;
        CheckBoxActive.IsChecked = _data.Active;

        ComboBoxSuccessToggle.SelectedIndex = (int)_data.SuccessFailToggle;
        ComboBoxSummaryType.SelectedIndex = (int)_data.SummaryType;

        CheckBoxNormalLogs.IsChecked = _data.IncludeNormalLogs;
        CheckBoxCmLogs.IsChecked = _data.IncludeChallengeModeLogs;
        CheckBoxLegendaryCmLogs.IsChecked = _data.IncludeLegendaryChallengeModeLogs;
        CheckBoxAllowUnknown.IsChecked = _data.AllowUnknownBossIds;
    }

    private async void OnTestClick(object sender, RoutedEventArgs e)
    {
        var url = TextBoxUrl.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        var testData = new DiscordWebhookData { Url = url };
        var ok = await testData.TestWebhookAsync(LogUploaderService.Instance.HttpClientController);
        LogUploaderService.Instance.AddToText(ok ? ">>> Test succeeded! Discord webhook is reachable." : ">>> Test failed! Webhook could not be reached.");
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TextBoxUrl.Text))
        {
            return;
        }

        _data.Name = TextBoxName.Text?.Trim() ?? "Discord Webhook";
        _data.Url = TextBoxUrl.Text?.Trim() ?? "";
        _data.Active = CheckBoxActive.IsChecked ?? true;

        _data.SuccessFailToggle = (DiscordWebhookDataSuccessToggle)Math.Max(0, ComboBoxSuccessToggle.SelectedIndex);
        _data.SummaryType = (DiscordWebhookDataLogSummaryType)Math.Max(0, ComboBoxSummaryType.SelectedIndex);

        _data.IncludeNormalLogs = CheckBoxNormalLogs.IsChecked ?? true;
        _data.IncludeChallengeModeLogs = CheckBoxCmLogs.IsChecked ?? true;
        _data.IncludeLegendaryChallengeModeLogs = CheckBoxLegendaryCmLogs.IsChecked ?? true;
        _data.AllowUnknownBossIds = CheckBoxAllowUnknown.IsChecked ?? false;

        Result = _data;
        Close(Result);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close(null);
    }
}
