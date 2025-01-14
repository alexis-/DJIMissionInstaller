namespace DJI_Mission_Installer.Devices
{
  using Operations;

  public static class DeviceOperationsFactory
  {
    #region Methods

    public static IDeviceOperations Create(DeviceConnectionType connectionType)
    {
      return connectionType switch
      {
        DeviceConnectionType.Mtp => new MtpDeviceOperations(),
        DeviceConnectionType.Adb => new AdbDeviceOperations(),
        _ => throw new ArgumentException($"Unsupported connection type: {connectionType}")
      };
    }

    #endregion
  }
}
