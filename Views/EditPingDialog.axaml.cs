using Avalonia.Controls;
using Avalonia.Interactivity;
using PlenBotLogUploader.RemotePing;
using System;

namespace PlenBotLogUploader.Views;

public partial class EditPingDialog : Window
{
    private readonly PingConfiguration _ping;

    public PingConfiguration Result { get; private set; }

    public EditPingDialog()
    {
        InitializeComponent();
    }

    internal EditPingDialog(PingConfiguration ping) : this()
    {
        _ping = ping ?? new PingConfiguration();

        TextBoxName.Text = _ping.Name;
        TextBoxUrl.Text = _ping.Url;
        ComboBoxMethod.SelectedIndex = (int)_ping.Method;
        CheckBoxActive.IsChecked = _ping.Active;
        CheckBoxSendJson.IsChecked = _ping.SendDataAsJson;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TextBoxUrl.Text))
        {
            return;
        }

        _ping.Name = TextBoxName.Text?.Trim() ?? "Remote Ping";
        _ping.Url = TextBoxUrl.Text.Trim();
        _ping.Method = (PingMethod)Math.Max(0, ComboBoxMethod.SelectedIndex);
        _ping.Active = CheckBoxActive.IsChecked ?? true;
        _ping.SendDataAsJson = CheckBoxSendJson.IsChecked ?? true;

        Result = _ping;
        Close(Result);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close(null);
    }
}
