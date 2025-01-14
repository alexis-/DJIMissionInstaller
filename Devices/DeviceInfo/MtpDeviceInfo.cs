namespace DJI_Mission_Installer.Devices.DeviceInfo
{
  public class MtpDeviceInfo(string deviceId, string storagePath, string displayName) : IDeviceInfo
  {
    #region Properties Impl - Public

    public string DeviceId    { get; } = deviceId;
    public string StoragePath { get; } = storagePath;
    public string DisplayName { get; } = displayName;

    #endregion

    #region Methods

    public static MtpDeviceInfo FromDeviceInfo(IDeviceInfo deviceInfo)
    {
      return new MtpDeviceInfo(deviceInfo.DeviceId, deviceInfo.StoragePath, deviceInfo.DisplayName);
    }

    #endregion
  }
}
