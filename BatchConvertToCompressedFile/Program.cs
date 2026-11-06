using Avalonia;

namespace BatchConvertToCompressedFile;

/// <summary>Application entry point.</summary>
internal static class Program
{
    /// <summary>Starts the Avalonia desktop application.</summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        // Bootstrap logger: App replaces this with the full Serilog pipeline once Avalonia starts.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .CreateLogger();

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception in the application entry point");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>Builds the Avalonia application configuration (also used by the visual designer).</summary>
    /// <returns>The configured application builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
