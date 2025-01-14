namespace DJI_Mission_Installer.Models
{
  public class WaypointFileInfo(
    string   id,
    string   deviceKmzPath,
    string   deviceImagePath,
    ulong    fileSize,
    DateTime lastModified)
  {
    #region Properties & Fields - Public

    public string   Id              { get; } = id;
    public string   DeviceKmzPath   { get; } = deviceKmzPath;
    public string   DeviceImagePath { get; } = deviceImagePath;
    public ulong    FileSize        { get; } = fileSize;
    public DateTime LastModified    { get; } = lastModified;

    #endregion
  }
}
