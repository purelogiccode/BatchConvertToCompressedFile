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

    private void OnWebsiteClick(object? sender, RoutedEventArgs e) =>
        OpenLink(AppConfig.WebsiteUrl);

    private void OnGitHubClick(object? sender, RoutedEventArgs e) =>
        OpenLink(AppConfig.RepositoryUrl);

    private void OnDonateClick(object? sender, RoutedEventArgs e) =>
        OpenLink(AppConfig.DonateUrl);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OpenLink(string url)
    {
        if (!_systemIntegration.OpenUrl(url))
        {
            Log.Warning("Could not open link {Url}", url);
        }
    }
}
