using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Logging;

namespace BatchConvertToCompressedFile.Views;

/// <summary>About window with version, platform, and project links.</summary>
internal partial class AboutWindow : Window
{
    private readonly ISystemIntegrationService _systemIntegration;

    /// <summary>Initializes a new About window.</summary>
    /// <param name="systemIntegration">Shell integration service used to open links.</param>
    public AboutWindow(ISystemIntegrationService systemIntegration)
    {
        _systemIntegration = systemIntegration ?? throw new ArgumentNullException(nameof(systemIntegration));

        InitializeComponent();

        Title = $"About {AppConfig.DisplayName}";
        DescriptionText.Text = AppConfig.Description;
        VersionText.Text = $"Version {AppConfig.ApplicationVersionString}";
        PlatformText.Text = $"{OsEnvironmentInfo.GetOsVersionName()} ({RuntimeInformation.ProcessArchitecture})";
        RuntimeText.Text = RuntimeInformation.FrameworkDescription;
    }

    private async void OnWebsiteClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await OpenLinkAsync(AppConfig.WebsiteUrl);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not open the website link");
        }
    }

    private async void OnGitHubClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await OpenLinkAsync(AppConfig.RepositoryUrl);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not open the GitHub link");
        }
    }

    private async void OnDonateClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await OpenLinkAsync(AppConfig.DonateUrl);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not open the donation link");
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private async Task OpenLinkAsync(string url)
    {
        if (_systemIntegration.OpenUrl(url)) return;

        Log.Information("Could not open link {Url}", url);
        if (!IsVisible) return;

        try
        {
            await MessageDialog.ShowAsync(
                this,
                "Could not open link",
                $"The link could not be opened in your browser:\n\n{url}",
                MessageDialogButtons.Ok,
                MessageDialogIcon.Warning);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not show the link error dialog");
        }
    }
}
