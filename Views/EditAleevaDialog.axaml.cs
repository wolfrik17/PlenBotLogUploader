using Avalonia.Controls;
using Avalonia.Interactivity;
using PlenBotLogUploader.Aleeva;
using System;

namespace PlenBotLogUploader.Views;

public partial class EditAleevaDialog : Window
{
    private readonly AleevaIntegration _integration;

    public AleevaIntegration Result { get; private set; }

    public EditAleevaDialog()
    {
        InitializeComponent();
    }

    internal EditAleevaDialog(AleevaIntegration integration) : this()
    {
        _integration = integration ?? new AleevaIntegration();

        TextBoxName.Text = _integration.Name;
        TextBoxServer.Text = _integration.Server;
        TextBoxChannel.Text = _integration.Channel;
        CheckBoxActive.IsChecked = _integration.Active;
        CheckBoxNotification.IsChecked = _integration.SendNotification;
        CheckBoxSuccessOnly.IsChecked = _integration.SendOnSuccessOnly;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _integration.Name = TextBoxName.Text?.Trim() ?? "Aleeva Integration";
        _integration.Server = TextBoxServer.Text?.Trim() ?? "";
        _integration.Channel = TextBoxChannel.Text?.Trim() ?? "";
        _integration.Active = CheckBoxActive.IsChecked ?? true;
        _integration.SendNotification = CheckBoxNotification.IsChecked ?? true;
        _integration.SendOnSuccessOnly = CheckBoxSuccessOnly.IsChecked ?? false;

        Result = _integration;
        Close(Result);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close(null);
    }
}
