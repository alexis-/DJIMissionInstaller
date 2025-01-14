namespace DJI_Mission_Installer.Devices.Operations
{
  using System.IO;
  using DeviceInfo;
  using Models;

  public interface IDeviceOperations
  {
    Task                     InitializeAsync();
    IEnumerable<IDeviceInfo> GetDevices();
    void                     Connect(IDeviceInfo deviceInfo);
    void                     Disconnect();
    bool                     IsConnected { get; }

    string NormalizePath(string path);

    // File operations
    bool FileExists(string   path);
    void DeleteFile(string   path);
    void UploadFile(Stream   sourceStream, string destinationPath);
    void DownloadFile(string sourcePath,   string destinationPath);

    // Directory operations
    bool                DirectoryExists(string path);
    IEnumerable<string> GetDirectories(string  path, string searchPattern, SearchOption searchOption);
    DeviceFileInfo?     GetFileInfo(string     path);
  }
}
