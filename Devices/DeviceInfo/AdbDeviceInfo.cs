using AdvancedSharpAdbClient.Models;

namespace DJI_Mission_Installer.Devices.DeviceInfo;

public class AdbDeviceInfo : IDeviceInfo
{
  public AdbDeviceInfo(DeviceData deviceData, string storagePath)
  {
    DeviceId    = deviceData.Serial;
    StoragePath = storagePath;
        
    // Build a more user-friendly display name
    var deviceModel = CleanupDeviceModel(deviceData.Model);
    var storageType = DetermineStorageType(storagePath);
        
    DisplayName = $"{deviceModel} ({storageType})";
  }

  private string CleanupDeviceModel(string model)
  {
    // Remove common prefixes/suffixes and clean up the model name
    return model
           .Replace("SM_", "Samsung ") // Samsung model prefix
           .Replace("SM-", "Samsung ")
           .Replace("_", " ") // Replace underscores with spaces
           .Trim();
  }

  private string DetermineStorageType(string path)
  {
    // Determine a user-friendly storage description
    return path.ToLowerInvariant() switch
    {
      "/storage/emulated/0" => "Internal Storage",
      "/storage/self/primary" => "Internal Storage",
      "/sdcard" => "Internal Storage",
      var p when p.Contains("sdcard") => "SD Card",
      _ => "Storage"
    };
  }

  public string DeviceId    { get; }
  public string StoragePath { get; }
  public string DisplayName { get; }
}