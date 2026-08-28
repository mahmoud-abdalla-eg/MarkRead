using System;
using System.Windows;
using MarkRead.Services;

namespace MarkRead;

public partial class IntegrationSettingsWindow : Window
{
    public IntegrationSettingsWindow()
    {
        InitializeComponent();
        LoadCurrentState();
    }

    private void LoadCurrentState()
    {
        try
        {
            ChkFileContextMenu.IsChecked = ShellIntegration.IsFileContextMenuRegistered();
            ChkDirectoryContextMenu.IsChecked = ShellIntegration.IsDirectoryContextMenuRegistered();
            ChkDefaultViewer.IsChecked = ShellIntegration.IsDefaultProgIdRegistered();
        }
        catch { }
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool enableFileMenu = ChkFileContextMenu.IsChecked == true;
            bool enableDirMenu = ChkDirectoryContextMenu.IsChecked == true;
            bool enableDefault = ChkDefaultViewer.IsChecked == true;

            ShellIntegration.SetFileContextMenu(enableFileMenu);
            ShellIntegration.SetDirectoryContextMenu(enableDirMenu);

            if (enableDefault)
            {
                ShellIntegration.PromptSetAsDefault();
            }

            ShellIntegration.CreateDesktopShortcut();

            MessageBox.Show(
                "Windows Explorer integration settings updated successfully!\n\n" +
                (enableFileMenu ? "• File context menu enabled\n" : "") +
                (enableDirMenu ? "• Directory context menu enabled\n" : "") +
                (enableDefault ? "• Set as default Markdown viewer\n" : ""),
                "MarkRead - Integration Updated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update integration settings: {ex.Message}", "MarkRead Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnRemoveAll_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Are you sure you want to remove all MarkRead Windows Explorer context menu actions and file associations?",
            "Remove All Associations",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            ShellIntegration.UnregisterFileAssociations();
            ChkFileContextMenu.IsChecked = false;
            ChkDirectoryContextMenu.IsChecked = false;
            ChkDefaultViewer.IsChecked = false;

            MessageBox.Show("All MarkRead Windows Explorer associations have been removed.", "MarkRead", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
