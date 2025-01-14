namespace DJI_Mission_Installer.UI.ViewModels
{
  using System.Collections.ObjectModel;
  using System.Diagnostics;
  using System.IO;
  using System.Runtime.CompilerServices;
  using System.Windows;
  using CommunityToolkit.Mvvm.Input;
  using Devices.DeviceInfo;
  using Devices.Operations;
  using DJI_Mission_Installer.Extensions;
  using DJI_Mission_Installer.Models;
  using Models;
  using Services;

  public class MainViewModel : ViewModelBase
  {
    #region Properties & Fields - Non-Public

    private readonly IDeviceOperations     _deviceOperations;
    private readonly IFileSystemService    _fileSystemService;
    private readonly IDialogService        _dialogService;
    private readonly IFileSortingService   _sortingService;
    private readonly IMapScreenshotService _mapScreenshotService;
    private readonly IConfigurationService _configurationService;
    private readonly IImageService         _imageService;

    private IDeviceInfo? _selectedDevice;
    private bool         _isLoading;

    #endregion

    #region Constructors

    public MainViewModel(
      IDeviceOperations     deviceOperations,
      IFileSystemService    fileSystemService,
      IDialogService        dialogService,
      IFileSortingService   sortingService,
      IConfigurationService configurationService)
    {
      _deviceOperations     = deviceOperations;
      _fileSystemService    = fileSystemService;
      _dialogService        = dialogService;
      _sortingService       = sortingService;
      _configurationService = configurationService;
      _mapScreenshotService    = new EsriMapScreenshotService();
      _imageService         = new ImageService();

      KmzFiles      = new FileListViewModel("KMZ Files", sortingService);
      WaypointFiles = new FileListViewModel("Device Waypoints", sortingService);

      RefreshDevicesCommand = new AsyncRelayCommand(RefreshDevicesAsync);
      TransferFileCommand   = new AsyncRelayCommand(TransferFileAsync, CanTransferFile);

      KmzFiles.SelectedItemChanged      += (s, e) => TransferFileCommand.NotifyCanExecuteChanged();
      WaypointFiles.SelectedItemChanged += (s, e) => TransferFileCommand.NotifyCanExecuteChanged();

      _fileSystemService.WatchKmzFolder();
      _fileSystemService.KmzFilesChanged += OnKmzFilesChanged;

      LoadKmzFiles();
    }

    #endregion

    #region Properties & Fields - Public

    public FileListViewModel                 KmzFiles              { get; }
    public FileListViewModel                 WaypointFiles         { get; }
    public ObservableCollection<IDeviceInfo> AvailableDevices      { get; } = [];
    public IAsyncRelayCommand                RefreshDevicesCommand { get; }
    public IAsyncRelayCommand                TransferFileCommand   { get; }

    public bool IsLoading
    {
      get => _isLoading;
      set => SetProperty(ref _isLoading, value);
    }

    public IDeviceInfo? SelectedDevice
    {
      get => _selectedDevice;
      set
      {
        if (_selectedDevice != value)
        {
          _selectedDevice = value;
          OnPropertyChanged();
          OnDeviceChanged();
        }
      }
    }

    #endregion

    #region Methods Impl

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      base.OnPropertyChanged(propertyName);

      // If the selected device changes, update the transfer command's can-execute state
      if (propertyName == nameof(SelectedDevice))
        ((AsyncRelayCommand)TransferFileCommand).NotifyCanExecuteChanged();
    }

    #endregion

    #region Methods

    public async Task InitializeAsync()
    {
      try
      {
        IsLoading = true;
        await _deviceOperations.InitializeAsync();
        await RefreshDevicesAsync();
      }
      catch (Exception ex)
      {
        await _dialogService.ShowErrorAsync("Failed to initialize device operations", ex.Message);
      }
      finally
      {
        IsLoading = false;
      }
    }

    private async Task RefreshDevicesAsync()
    {
      IsLoading = true;
      try
      {
        var devices = await Task.Run(() => _deviceOperations.GetDevices());
        AvailableDevices.Clear();
        foreach (var device in devices)
          AvailableDevices.Add(device);
      }
      catch (Exception ex)
      {
        await _dialogService.ShowErrorAsync("Failed to load devices", ex.Message);
      }
      finally
      {
        IsLoading = false;
      }
    }

    private async void OnDeviceChanged()
    {
      if (SelectedDevice == null)
      {
        WaypointFiles.Items.Clear();
        return;
      }

      IsLoading = true;
      try
      {
        await Task.Run(() =>
        {
          _deviceOperations.Connect(SelectedDevice);

          var waypoints = GetDeviceWaypoints();

          foreach (var waypoint in waypoints)
          {
            var imageUri = waypoint.GetLocalImageUri();

            if (imageUri != null && !File.Exists(imageUri.AbsolutePath))
              _deviceOperations.DownloadFile(waypoint.GetDeviceImagePath(_selectedDevice!), imageUri.AbsolutePath);
          }

          Application.Current.Dispatcher.Invoke(() => { WaypointFiles.UpdateItems(waypoints.Select(w => new FileListItem(w))); });
        });
      }
      catch (Exception ex)
      {
        await _dialogService.ShowErrorAsync("Failed to retrieve files", ex.Message);
      }
      finally
      {
        IsLoading = false;
      }
    }

    private List<WaypointFileInfo> GetDeviceWaypoints()
    {
      WaypointFileInfo CreateWaypointFileInfo(string id)
      {
        var kmzPath = _deviceOperations.NormalizePath(Path.Combine(_selectedDevice!.StoragePath, Const.WaypointFolder, id, $"{id}.kmz"));
        var imagePath =
          _deviceOperations.NormalizePath(Path.Combine(_selectedDevice!.StoragePath, Const.WaypointPreviewFolder, id, $"{id}.jpg"));
        var deviceFileInfo = _deviceOperations.GetFileInfo(kmzPath);

        return new WaypointFileInfo(id, kmzPath, imagePath, deviceFileInfo?.Size ?? 0,
                                    deviceFileInfo?.LastModified ?? DateTime.MinValue);
      }

      return _deviceOperations
             .GetDirectories(
               Path.Combine(_selectedDevice!.StoragePath, Const.WaypointFolder),
               "*",
               SearchOption.TopDirectoryOnly)
             .Select(Path.GetFileName)
             .Where(dir => !string.IsNullOrWhiteSpace(dir) && Guid.TryParse(dir, out _))
             .Select(id => CreateWaypointFileInfo(id!))
             .ToList();
    }

    private async Task TransferFileAsync()
    {
      if (!CanTransferFile())
        return;

      var kmzFile      = (KmzFile)KmzFiles.SelectedItem!.FileInfo;
      var waypointFile = (WaypointFileInfo)WaypointFiles.SelectedItem!.FileInfo;

      try
      {
        await Task.Run(async () =>
        {
          var directoryPath  = _deviceOperations.NormalizePath(Path.GetDirectoryName(waypointFile.DeviceKmzPath)!);

          if (!_deviceOperations.DirectoryExists(directoryPath))
            throw new DirectoryNotFoundException("Selected Waypoint file doesn't exist on the device anymore.");

          // KMZ file
          if (_deviceOperations.FileExists(waypointFile.DeviceKmzPath))
            _deviceOperations.DeleteFile(waypointFile.DeviceKmzPath);

          await using (var kmzStream = File.OpenRead(kmzFile.FullPath))
            _deviceOperations.UploadFile(kmzStream, waypointFile.DeviceKmzPath);

          // Map image
          var mapImagePath = await CreateMapImage(waypointFile.Id, kmzFile.Name);

          if (!string.IsNullOrWhiteSpace(mapImagePath))
          {
            if (_deviceOperations.FileExists(waypointFile.DeviceImagePath))
              _deviceOperations.DeleteFile(waypointFile.DeviceImagePath);

            await using (var imageStream = File.OpenRead(mapImagePath))
              _deviceOperations.UploadFile(imageStream, waypointFile.DeviceImagePath);
          }
        });

        await _dialogService.ShowInfoAsync("Success", $"Successfully transferred {kmzFile.Name} to the device.");

        // Refresh the waypoint files list after successful transfer
        OnDeviceChanged();
      }
      catch (Exception ex)
      {
        await _dialogService.ShowErrorAsync("Failed to transfer file", ex.Message);
      }
    }

    private async Task<string> CreateMapImage(string id, string kmzFileName)
    {
      var tempMapPath    = Path.Combine(Const.TempPath, $"{id}_map.jpg");
      var finalImagePath = Path.Combine(Const.TempPath, $"{id}.jpg");

      try
      {
        var progress = (IProgress<double>)new Progress<double>(p =>
        {
          // Update loading status or progress bar if needed
          Debug.WriteLine($"Map generation progress: {p:P0}");
        });

        // Generate and save the map screenshot
        await _mapScreenshotService.SaveMapScreenshotAsync(
          40.7128, // New York coordinates as placeholder
          -74.0060,
          15,
          tempMapPath,
          800,
          480
        );

        progress.Report(0.4);

        // Get the waypoint file info for last modified date
        var waypointFile = WaypointFiles.Items
                                        .Select(item => item.FileInfo as WaypointFileInfo)
                                        .FirstOrDefault(w => w?.Id == id);

        // Process the image with overlay
        return await _imageService.ProcessImageAsync(
          tempMapPath,
          finalImagePath,
          kmzFileName,
          waypointFile?.LastModified ?? DateTime.MinValue,
          progress);
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex);
        
        // Create a default white image with overlay if everything fails
        return await _imageService.CreateDefaultImageAsync(
          finalImagePath,
          id,
          DateTime.MinValue,
          800,
          480);
      }
      finally
      {
        // Clean up temporary map file
        if (File.Exists(tempMapPath))
        {
          try
          {
            File.Delete(tempMapPath);
          }
          catch (Exception ex)
          {
            Debug.WriteLine($"Failed to delete temporary map file: {ex.Message}");
          }
        }
      }
    }

    private bool CanTransferFile() =>
      _deviceOperations.IsConnected &&
      SelectedDevice != null &&
      KmzFiles.SelectedItem?.FileInfo is KmzFile &&
      WaypointFiles.SelectedItem?.FileInfo is WaypointFileInfo;

    private void OnKmzFilesChanged(object? sender, EventArgs e)
    {
      LoadKmzFiles();
    }

    private void LoadKmzFiles()
    {
      var files = _fileSystemService.GetKmzFiles()
                                    .Select(f => new FileListItem(new KmzFile(f)))
                                    .ToList();

      // Only call UpdateItems once, and do it on the UI thread
      Application.Current.Dispatcher.Invoke(() => { KmzFiles.UpdateItems(files); });
    }

    #endregion
  }
}
