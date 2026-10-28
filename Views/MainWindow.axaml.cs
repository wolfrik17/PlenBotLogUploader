using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PlenBotLogUploader.AppSettings;
using PlenBotLogUploader.Services;
using PlenBotLogUploader.Tools;
using PlenBotLogUploader.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlenBotLogUploader.Views;

public partial class MainWindow : Window
{
    private static readonly Dictionary<string, string> DefaultPostData = new()
    {
        { "generator", "ei" },
        { "json", "1" },
    };

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);

        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.RequestBrowseFolderDialog = async () =>
            {
                var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select arcDPS Combat Logs Folder",
                    AllowMultiple = false,
                });
                return folders.Count > 0 ? folders[0].Path.LocalPath : null;
            };

            vm.RequestCopyToClipboard = async text =>
            {
                if (Clipboard is not null)
                {
                    await Clipboard.SetTextAsync(text);
                }
            };

            vm.RequestEditWebhookDialog = async webhook =>
            {
                var dialog = new EditDiscordWebhookDialog(webhook);
                return await dialog.ShowDialog<DiscordApi.DiscordWebhookData>(this);
            };

            vm.RequestEditTeamDialog = async team =>
            {
                var dialog = new EditTeamDialog(team);
                return await dialog.ShowDialog<Teams.Team>(this);
            };

            vm.RequestEditPingDialog = async ping =>
            {
                var dialog = new EditPingDialog(ping);
                return await dialog.ShowDialog<RemotePing.PingConfiguration>(this);
            };

            vm.RequestEditAleevaDialog = async aleeva =>
            {
                var dialog = new EditAleevaDialog(aleeva);
                return await dialog.ShowDialog<Aleeva.AleevaIntegration>(this);
            };

            vm.RequestEditGw2ApiDialog = async api =>
            {
                var dialog = new EditGw2ApiDialog(api);
                return await dialog.ShowDialog<ApplicationSettingsGw2Api>(this);
            };

            vm.RequestEditUserTokenDialog = async token =>
            {
                var dialog = new EditDpsReportTokenDialog(token);
                return await dialog.ShowDialog<ApplicationSettingsUploadUserToken>(this);
            };

            vm.RequestBrowseGw2FolderDialog = async () =>
            {
                var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Guild Wars 2 Installation Folder",
                    AllowMultiple = false,
                });
                return folders.Count > 0 ? folders[0].Path.LocalPath : null;
            };
        }
    }

    private void OnWindowClosing(object sender, WindowClosingEventArgs e)
    {
        if (ApplicationSettings.Current.CloseToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;

        var files = e.Data.GetFiles();
        if (files is null) return;

        foreach (var storageItem in files)
        {
            var path = storageItem.Path.LocalPath;
            if (File.Exists(path) && (path.EndsWith(".evtc", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".zevtc", StringComparison.OrdinalIgnoreCase)))
            {
                _ = LogUploaderService.Instance.UploadLogFileAsync(path, DefaultPostData);
            }
        }
    }

    private void OnOpenLogLinkClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url } && !string.IsNullOrWhiteSpace(url))
        {
            ProcessHelper.OpenUrl(url);
        }
    }

    private async void OnCopyLogLinkClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url } && !string.IsNullOrWhiteSpace(url) && Clipboard is not null)
        {
            await Clipboard.SetTextAsync(url);
            LogUploaderService.Instance.AddToText($">>> Copied link to clipboard: {url}");
        }
    }
}
