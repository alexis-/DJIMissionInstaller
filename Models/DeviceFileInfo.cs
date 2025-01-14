namespace DJI_Mission_Installer.Models
{
  public class DeviceFileInfo(string fullPath, DateTime? lastModified, ulong size)
  {
    #region Properties & Fields - Public

    public string    FullPath     { get; set; } = fullPath;
    public ulong     Size         { get; set; } = size;
    public DateTime? LastModified { get; set; } = lastModified;

    #endregion
  }
}
