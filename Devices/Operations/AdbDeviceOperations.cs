namespace DJI_Mission_Installer.Devices.Operations
{
  using System.Diagnostics;
  using System.IO;
  using System.Text;
  using System.Text.RegularExpressions;
  using AdvancedSharpAdbClient;
  using AdvancedSharpAdbClient.Models;
  using AdvancedSharpAdbClient.Receivers;
  using DeviceInfo;
  using DJI_Mission_Installer.Models;

  public class AdbDeviceOperations : IDeviceOperations, IDisposable
  {
    #region Properties & Fields - Non-Public

    private AdbClient?    _client;
    private AdbServer?    _server;
    private DeviceData?  _currentDevice;
    private IDeviceInfo? _currentDeviceInfo;
    private bool         _initialized;

    #endregion

    #region Constructors

    public void Dispose()
    {
      Disconnect();
    }

    #endregion

    #region Properties Impl - Public

    public bool IsConnected => _currentDevice != null && _currentDeviceInfo != null;
    
    private AdbClient Client => _client ?? throw new InvalidOperationException("ADB client not initialized. Call InitializeAsync first.");

    #endregion

    #region Methods Impl

    public async Task InitializeAsync()
    {
      if (_initialized) return;

      await Task.Run(() =>
      {
        var adbPath = FindAdbPath();
        _server = new AdbServer();
        var serverResult = _server.StartServer(adbPath, false);

        if (serverResult != StartServerResult.Started && serverResult != StartServerResult.AlreadyRunning)
          throw new InvalidOperationException($"Failed to start ADB server. Result: {serverResult}");

        _client      = new AdbClient();
        _initialized = true;
      });
    }

    public DeviceFileInfo? GetFileInfo(string path)
    {
      var device         = GetConnectedDevice();
      var normalizedPath = path.Replace("\\", "/");

      if (!FileExists(normalizedPath))
        return null;

      var receiver = new ConsoleOutputReceiver();

      Client.ExecuteRemoteCommand($"stat -c '%s %Y' {normalizedPath}", device, receiver);
      var parts = receiver.ToString().Trim().Split(' ');
            
      if (parts.Length == 2
        && long.TryParse(parts[0], out var size) && long.TryParse(parts[1], out var timestamp))
      {
        return new DeviceFileInfo(path, DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime, (ulong)size);
      }

      return null;
    }

    public IEnumerable<IDeviceInfo> GetDevices()
    {
      if (!_initialized) throw new InvalidOperationException("ADB client not initialized. Call InitializeAsync first.");

      var devices = new List<IDeviceInfo>();

      foreach (var device in Client.GetDevices())
        try
        {
          foreach (var storagePath in GetDeviceStoragePaths(device))
            try
            {
              var deviceInfo   = new AdbDeviceInfo(device, storagePath);
              var waypointPath = Path.Combine(storagePath, Const.WaypointFolder).Replace("\\", "/");

              var receiver = new ConsoleOutputReceiver();
              Client.ExecuteRemoteCommand($"ls -d {waypointPath}", device, receiver, Encoding.UTF8);

              if (!receiver.ToString().Contains("No such file or directory"))
                devices.Add(deviceInfo);
            }
            catch (Exception ex)
            {
              Debug.WriteLine($"Failed to process storage path {storagePath} for device {device.Serial}: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Failed to process device {device.Serial}: {ex.Message}");
        }

      return devices;
    }

    public void Connect(IDeviceInfo deviceInfo)
    {
      if (IsConnected)
        Disconnect();

      var device = Client.GetDevices()
                         .FirstOrDefault(d => d.Serial == deviceInfo.DeviceId);

      if (device == null)
        throw new InvalidOperationException($"Device {deviceInfo.DeviceId} not found");

      // Verify the storage path exists and is accessible
      var availablePaths = GetDeviceStoragePaths(device).ToList();
      if (!availablePaths.Contains(deviceInfo.StoragePath))
        throw new InvalidOperationException(
          $"Storage path {deviceInfo.StoragePath} is no longer accessible. " +
          $"Available paths: {string.Join(", ", availablePaths)}");

      _currentDevice     = device;
      _currentDeviceInfo = deviceInfo;
    }

    public void UploadFile(Stream sourceStream, string destinationPath)
    {
      var device         = GetConnectedDevice();
      var normalizedPath = destinationPath.Replace("\\", "/");

      // Ensure the directory exists
      var directory = Path.GetDirectoryName(normalizedPath)!.Replace("\\", "/");
      var receiver  = new ConsoleOutputReceiver();

      Client.ExecuteRemoteCommand($"mkdir -p {directory}", device, receiver, Encoding.UTF8);


      // Upload the file directly from the source stream
      using var sync = new SyncService(Client, device);
      sync.Push(
        sourceStream,
        normalizedPath,
        UnixFileStatus.Regular | UnixFileStatus.UserRead | UnixFileStatus.UserWrite,
        DateTime.Now,
        null);
    }

    public void DownloadFile(string sourcePath, string destinationPath)
    {
      var device         = GetConnectedDevice();
      var normalizedPath = sourcePath.Replace("\\", "/");

      if (!FileExists(normalizedPath))
        throw new FileNotFoundException($"Source file {normalizedPath} not found on device");

      Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

      using var sync   = new SyncService(Client, device);
      using var stream = File.Create(destinationPath);
      sync.Pull(normalizedPath, stream, null);
    }

    public bool FileExists(string path)
    {
      var device         = GetConnectedDevice();
      var normalizedPath = path.Replace("\\", "/");

      var receiver = new ConsoleOutputReceiver();
      Client.ExecuteRemoteCommand($"test -f {normalizedPath} && echo 'exists'", device, receiver, Encoding.UTF8);
      return receiver.ToString().Contains("exists");
    }

    public void DeleteFile(string path)
    {
      var device         = GetConnectedDevice();
      var normalizedPath = path.Replace("\\", "/");

      if (!FileExists(normalizedPath))
        return; // File doesn't exist, nothing to delete

      var receiver = new ConsoleOutputReceiver();
      Client.ExecuteRemoteCommand($"rm {normalizedPath}", device, receiver, Encoding.UTF8);

      // Verify deletion
      if (FileExists(normalizedPath))
        throw new IOException($"Failed to delete file {normalizedPath}");
    }

    public bool DirectoryExists(string path)
    {
      var device         = GetConnectedDevice();
      var normalizedPath = path.Replace("\\", "/");

      var receiver = new ConsoleOutputReceiver();
      Client.ExecuteRemoteCommand($"test -d {normalizedPath} && echo 'exists'", device, receiver, Encoding.UTF8);
      return receiver.ToString().Contains("exists");
    }

    public IEnumerable<string> GetDirectories(string path, string searchPattern, SearchOption searchOption)
    {
      var device         = GetConnectedDevice();
      var normalizedPath = path.Replace("\\", "/");

      if (!DirectoryExists(normalizedPath))
        return Enumerable.Empty<string>();

      var receiver = new ConsoleOutputReceiver();
      var command = searchOption == SearchOption.AllDirectories
        ? $"find {normalizedPath} -type d"
        : $"find {normalizedPath} -maxdepth 1 -type d";

      Client.ExecuteRemoteCommand(command, device, receiver, Encoding.UTF8);

      return receiver.ToString()
                     .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                     .Where(dir => MatchesPattern(Path.GetFileName(dir), searchPattern));
    }

    public void Disconnect()
    {
      _currentDevice     = null;
      _currentDeviceInfo = null;
    }

    public string NormalizePath(string path)
    {
      return path.Replace("\\", "/");
    }

    #endregion

    #region Methods

    private IEnumerable<string> GetDeviceStoragePaths(DeviceData device)
    {
      var paths = new HashSet<string>();

      // Modern Android storage detection strategy
      var storagePaths = new[]
      {
        // Primary storage locations (in order of preference)
        "/storage/emulated/0",   // Modern Android primary storage
        "/storage/self/primary", // Symbolic link to primary storage
        "/sdcard",               // Universal symbolic link

        // Legacy paths (fallback)
        "/storage/sdcard0",        // Old Android primary storage
        "/storage/emulated/legacy" // Legacy Android storage
      };

      foreach (var path in storagePaths)
        try
        {
          // First verify the path exists
          var receiver = new ConsoleOutputReceiver();
          Client.ExecuteRemoteCommand($"test -d {path} && echo 'exists'", device, receiver, Encoding.UTF8);
          if (!receiver.ToString().Contains("exists"))
            continue;

          // Then verify we can list contents (confirms permissions)
          receiver = new ConsoleOutputReceiver();
          Client.ExecuteRemoteCommand($"ls {path}/", device, receiver, Encoding.UTF8);
          var output = receiver.ToString();
          if (!output.Contains("Permission denied") && !string.IsNullOrWhiteSpace(output))
          {
            // Additional write test
            receiver = new ConsoleOutputReceiver();
            var testPath = $"{path}/.dji_test_write";
            Client.ExecuteRemoteCommand(
              $"touch {testPath} && rm {testPath} && echo 'writable'",
              device,
              receiver,
              Encoding.UTF8);

            if (receiver.ToString().Contains("writable"))
            {
              paths.Add(path);
              Debug.WriteLine($"Found valid storage path: {path}");
              break; // Found a working primary storage, no need to check others
            }
          }
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Failed to verify path {path}: {ex.Message}");
          // Continue checking other paths
        }

      // If no primary storage found, try to detect external SD card
      if (paths.Count == 0)
        try
        {
          var receiver = new ConsoleOutputReceiver();
          // List all storage locations
          Client.ExecuteRemoteCommand("ls -d /storage/*/", device, receiver, Encoding.UTF8);
          var output = receiver.ToString();

          foreach (var path in output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(p => p.TrimEnd('/')))
          {
            // Skip already known paths
            if (storagePaths.Contains(path)) continue;

            // Verify this new path
            receiver = new ConsoleOutputReceiver();
            Client.ExecuteRemoteCommand($"ls {path}/", device, receiver, Encoding.UTF8);
            if (!receiver.ToString().Contains("Permission denied"))
            {
              paths.Add(path);
              Debug.WriteLine($"Found additional storage path: {path}");
            }
          }
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Failed to detect additional storage: {ex.Message}");
        }

      return paths;
    }

    private bool IsPathWritable(DeviceData device, string path)
    {
      var receiver = new ConsoleOutputReceiver();
      Client.ExecuteRemoteCommand($"test -w {path} && echo 'writable'", device, receiver, Encoding.UTF8);
      return receiver.ToString().Contains("writable");
    }

    private bool MatchesPattern(string name, string pattern)
    {
      var regex = "^" + Regex.Escape(pattern)
                             .Replace("\\*", ".*")
                             .Replace("\\?", ".") + "$";

      return Regex.IsMatch(name, regex, RegexOptions.IgnoreCase);
    }

    private static string FindAdbPath()
    {
      var possiblePaths = new[]
      {
        "adb.exe",
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb.exe"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "adb.exe"),
        @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
        @"C:\Program Files\Android\android-sdk\platform-tools\adb.exe"
      };

      return possiblePaths.FirstOrDefault(File.Exists)
        ?? throw new FileNotFoundException(
          "adb.exe not found. Please ensure Android SDK Platform Tools are installed or copy adb.exe to the application directory.");
    }

    private DeviceData GetConnectedDevice()
    {
      if (!IsConnected || _currentDevice == null)
        throw new InvalidOperationException("No device connected");

      var serial = ((DeviceData)_currentDevice).Serial;

      // Verify device is still connected
      if (Client.GetDevices().All(d => d.Serial != serial))
      {
        Disconnect();
        throw new InvalidOperationException("Device has been disconnected");
      }

      return (DeviceData)_currentDevice;
    }

    #endregion
  }
}
