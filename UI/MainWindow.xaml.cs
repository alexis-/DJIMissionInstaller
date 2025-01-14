namespace DJI_Mission_Installer.UI;

using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Devices;
using Devices.Operations;
using Services;
using ViewModels;

public partial class MainWindow : Window
{
  #region Properties & Fields - Non-Public

  private readonly IDeviceOperations     _deviceOperations;
  private readonly IConfigurationService _configurationService;

  #endregion

  #region Constructors

  public MainWindow(DeviceConnectionType connectionType, IConfigurationService configurationService)
  {
    InitializeComponent();
    
    _configurationService = configurationService;
    _deviceOperations     = DeviceOperationsFactory.Create(connectionType);

    var fileSystemService = new FileSystemService(ConfigurationManager.AppSettings["kmzSourceFolder"] ?? string.Empty);
    var dialogService     = new DialogService();
    var sortingService    = new FileSortingService();

    var viewModel = new MainViewModel(_deviceOperations, fileSystemService, dialogService, sortingService, configurationService);
    DataContext = viewModel;

    // Initialize device operations after UI is shown
    Loaded += async (s, e) => await viewModel.InitializeAsync();

    // Handle application shutdown
    Application.Current.Exit += Current_Exit;
    Closing                  += MainWindow_Closing;
  }

  #endregion

  #region Methods

  private void Current_Exit(object sender, ExitEventArgs e)
  {
    Cleanup();
  }

  private void MainWindow_Closing(object? sender, CancelEventArgs e)
  {
    Cleanup();
  }

  private void Cleanup()
  {
    try
    {
      // Dispose of device operations
      if (_deviceOperations is IDisposable disposable)
        disposable.Dispose();

      // Kill any remaining adb processes
      foreach (var process in Process.GetProcessesByName("adb"))
        try
        {
          process.Kill();
          process.WaitForExit(1000); // Wait up to 1 second for the process to exit
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Failed to kill adb process: {ex.Message}");
        }
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Error during cleanup: {ex.Message}");
    }
  }

  private void ListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
  {
    var scrollViewer = GetDescendantByType<ScrollViewer>((ListView)sender);
    if (scrollViewer != null)
    {
      // Adjust this value to control scroll speed
      double scrollAmount = -e.Delta * 0.8; // Makes scrolling slower
      scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + scrollAmount);
      e.Handled = true;
    }
  }

  private static T? GetDescendantByType<T>(DependencyObject? element) where T : class
  {
    if (element == null) return null;

    if (element is T)
      return element as T;

    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
    {
      var child  = VisualTreeHelper.GetChild(element, i);
      var result = GetDescendantByType<T>(child);
      if (result != null)
        return result;
    }

    return null;
  }

  #endregion
}
