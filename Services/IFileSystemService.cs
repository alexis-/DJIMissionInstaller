namespace DJI_Mission_Installer.Services
{
  using System.IO;

  public interface IFileSystemService
  {
    event EventHandler KmzFilesChanged;
    void               WatchKmzFolder();
    List<FileInfo>     GetKmzFiles();
  }
}
