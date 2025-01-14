namespace DJI_Mission_Installer.Devices.DeviceInfo
{
  public interface IDeviceInfo
  {
    string DeviceId    { get; }
    string StoragePath { get; }
    string DisplayName { get; }
  }
}
