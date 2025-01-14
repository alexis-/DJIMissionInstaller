namespace DJI_Mission_Installer.Devices.Operations
{
  using System.IO;
  using DeviceInfo;
  using MediaDevices;
  using Models;

  public class MtpDeviceOperations : IDeviceOperations, IDisposable
  {
    #region Properties & Fields - Non-Public

    private MediaDevice? _currentDevice;
    private IDeviceInfo? _currentDeviceInfo;

    #endregion

    #region Constructors

    public void Dispose()
    {
      Disconnect();
    }

    #endregion

    #region Properties Impl - Public

    public bool IsConnected => _currentDevice != null && _currentDeviceInfo != null;

    #endregion

    #region Methods Impl

    /// <inheritdoc />
    public Task InitializeAsync()
    {
      return Task.CompletedTask;
    }

    public IEnumerable<IDeviceInfo> GetDevices()
    {
      var devices     = MediaDevice.GetDevices();
      var deviceInfos = new List<IDeviceInfo>();

      foreach (var device in devices)
      {
        device.Connect();
        try
        {
          foreach (var storage in GetStorageLocations(device))
          {
            var displayName = $"{device.FriendlyName} - {Path.GetFileName(storage)}";
            var deviceInfo  = new MtpDeviceInfo(device.DeviceId, storage, displayName);

            // Only add devices that have the waypoint folder
            if (DirectoryExistsForDevice(device, deviceInfo, Const.WaypointFolder))
              deviceInfos.Add(deviceInfo);
          }
        }
        finally
        {
          device.Disconnect();
        }
      }

      return deviceInfos;
    }

    public void Connect(IDeviceInfo deviceInfo)
    {
      Disconnect();

      _currentDevice = MediaDevice.GetDevices()
                                  .FirstOrDefault(d => d.DeviceId == deviceInfo.DeviceId);

      if (_currentDevice == null)
        throw new InvalidOperationException($"Device {deviceInfo.DeviceId} not found");

      _currentDevice.Connect();
      _currentDeviceInfo = deviceInfo;
    }

    public void Disconnect()
    {
      if (_currentDevice != null)
      {
        _currentDevice.Disconnect();
        _currentDevice     = null;
        _currentDeviceInfo = null;
      }
    }

    // Add the missing interface method
    public DeviceFileInfo? GetFileInfo(string path)
    {
      EnsureConnected();

      try
      {
        var file = _currentDevice!.GetFileInfo(path);

        return new DeviceFileInfo(file.FullName, file.LastWriteTime, file.Length);
      }
      catch
      {
        return null;
      }
    }

    /// <inheritdoc />
    public string NormalizePath(string path)
    {
      return path;
    }

    public bool FileExists(string path)
    {
      EnsureConnected();
      return _currentDevice!.FileExists(path);
    }

    public void DeleteFile(string path)
    {
      EnsureConnected();
      _currentDevice!.DeleteFile(path);
    }

    public void UploadFile(Stream sourceStream, string destinationPath)
    {
      EnsureConnected();
      _currentDevice!.UploadFile(sourceStream, destinationPath);
    }

    public void DownloadFile(string sourcePath, string destinationPath)
    {
      EnsureConnected();
      _currentDevice!.DownloadFile(sourcePath, destinationPath);
    }

    public bool DirectoryExists(string path)
    {
      EnsureConnected();
      return _currentDevice!.DirectoryExists(path);
    }

    public IEnumerable<string> GetDirectories(string path, string searchPattern, SearchOption searchOption)
    {
      EnsureConnected();
      return _currentDevice!.GetDirectories(path, searchPattern, searchOption);
    }

    #endregion

    #region Methods

    private static IEnumerable<string> GetStorageLocations(MediaDevice device)
    {
      var rootDirectory    = device.GetRootDirectory();
      var storageLocations = device.GetDirectories(rootDirectory.FullName, "*", SearchOption.TopDirectoryOnly);

      return storageLocations;
    }

    private bool DirectoryExistsForDevice(MediaDevice device, IDeviceInfo deviceInfo, string relativePath)
    {
      var fullPath = Path.Combine(deviceInfo.StoragePath, relativePath);
      return device.DirectoryExists(fullPath);
    }

    private void EnsureConnected()
    {
      if (!IsConnected)
        throw new InvalidOperationException("Device is not connected");
    }

    #endregion
  }
}
