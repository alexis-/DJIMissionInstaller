namespace DJI_Mission_Installer
{
  using System.Diagnostics;
  using System.IO;
  using System.Windows;
  using Devices;
  using Services;
  using UI;

  public partial class App : Application
  {
    #region Properties & Fields - Non-Public

    private readonly IConfigurationService _configurationService;

    #endregion

    #region Constructors

    public App()
    {
      _configurationService        =  new ConfigurationService();
      DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    #endregion

    #region Methods Impl

    protected override void OnStartup(StartupEventArgs e)
    {
      try
      {
        base.OnStartup(e);

        // Create temp folder if it doesn't exist
        if (!Directory.Exists(Const.TempPath))
          Directory.CreateDirectory(Const.TempPath);

        // Create KMZ source folder if it doesn't exist
        if (!Directory.Exists(_configurationService.KmzSourceFolder))
          Directory.CreateDirectory(_configurationService.KmzSourceFolder);

        // Try to start with user's preferred connection type
        try
        {
          var connectionType = _configurationService.UseAdbByDefault ? DeviceConnectionType.Adb : DeviceConnectionType.Mtp;

          var mainWindow = new MainWindow(connectionType, _configurationService);
          mainWindow.Show();
        }
        catch (FileNotFoundException ex) when (ex.Message.Contains("adb.exe"))
        {
          // If ADB isn't available, fall back to MTP and update the setting
          MessageBox.Show("ADB not found. Falling back to MTP mode.\n\n" +
                          "To use ADB mode, please install Android SDK Platform Tools or copy adb.exe to the application directory.",
                          "ADB Not Found",
                          MessageBoxButton.OK,
                          MessageBoxImage.Warning);

          _configurationService.UseAdbByDefault = false;
          _configurationService.Save();

          var mainWindow = new MainWindow(DeviceConnectionType.Mtp, _configurationService);
          mainWindow.Show();
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Startup error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}");
        Shutdown(-1);
      }
    }

    #endregion

    #region Methods

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      Debug.WriteLine($"An error occurred: {e.Exception.Message}\n\nStack trace:\n{e.Exception.StackTrace}");
      e.Handled = true;
    }

    #endregion
  }
}
