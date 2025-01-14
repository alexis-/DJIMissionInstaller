namespace DJI_Mission_Installer.Extensions
{
  using System.IO;
  using Devices.DeviceInfo;
  using Models;

  public static class WaypointFileInfoEx
  {
    #region Methods

    public static string GetDeviceKmzPath(this WaypointFileInfo wfi, IDeviceInfo deviceInfo) =>
      Path.Combine(deviceInfo.StoragePath, Const.WaypointFolder, wfi.Id, $"{wfi.Id}.kmz");

    public static string GetDeviceImagePath(this WaypointFileInfo wfi, IDeviceInfo deviceInfo) =>
      Path.Combine(deviceInfo.StoragePath, Const.WaypointPreviewFolder, wfi.Id, $"{wfi.Id}.jpg");

    public static Uri? GetLocalImageUri(this WaypointFileInfo wfi)
    {
      try
      {
        var path = Path.Combine(Const.TempPath, $"{wfi.Id}.jpg");
        return new Uri(path);
      }
      catch
      {
        return null;
      }
    }

    #endregion
  }
}
