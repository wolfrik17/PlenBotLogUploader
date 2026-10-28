using Avalonia.Controls;
using Avalonia.Interactivity;
using PlenBotLogUploader.Teams;
using System;

namespace PlenBotLogUploader.Views;

public partial class EditTeamDialog : Window
{
    private readonly Team _team;

    public Team Result { get; private set; }

    public EditTeamDialog()
    {
        InitializeComponent();
    }

    internal EditTeamDialog(Team team) : this()
    {
        _team = team ?? new Team();

        TextBoxName.Text = _team.Name;
        ComboBoxLimiter.SelectedIndex = (int)_team.Limiter;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TextBoxName.Text))
        {
            return;
        }

        _team.Name = TextBoxName.Text.Trim();
        _team.Limiter = (TeamLimiter)Math.Max(0, ComboBoxLimiter.SelectedIndex);

        Result = _team;
        Close(Result);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close(null);
    }
}
