namespace DJI_Mission_Installer.Models
{
  using System.IO;

  public class KmzFile(FileInfo fileInfo)
  {
    #region Properties & Fields - Public

    public string   Name          => fileInfo.Name;
    public string   DirectoryName => fileInfo.DirectoryName ?? string.Empty;
    public DateTime LastWriteTime => fileInfo.LastWriteTime;
    public string   FullPath      => fileInfo.FullName;
    public ulong    FileSize      => (ulong)fileInfo.Length;

    #endregion
  }
}
