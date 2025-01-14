namespace DJI_Mission_Installer.Services
{
  using System.IO;

  public class FileSystemService : IFileSystemService, IDisposable
  {
    #region Properties & Fields - Non-Public

    private readonly string             _kmzSourceFolder;
    private          FileSystemWatcher? _watcher;

    #endregion

    #region Constructors

    public FileSystemService(string kmzSourceFolder)
    {
      _kmzSourceFolder = kmzSourceFolder;
    }

    public void Dispose()
    {
      if (_watcher != null)
      {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
      }
    }

    #endregion

    #region Methods Impl

    public void WatchKmzFolder()
    {
      _watcher = new FileSystemWatcher(_kmzSourceFolder)
      {
        Filter                = "*.kmz",
        IncludeSubdirectories = true,
        EnableRaisingEvents   = true
      };

      _watcher.Created += OnKmzFileChanged;
      _watcher.Deleted += OnKmzFileChanged;
      _watcher.Changed += OnKmzFileChanged;
      _watcher.Renamed += OnKmzFileRenamed;
    }

    public List<FileInfo> GetKmzFiles()
    {
      return Directory.GetFiles(_kmzSourceFolder, "*.kmz", SearchOption.AllDirectories)
                      .Select(f => new FileInfo(f))
                      .ToList();
    }

    #endregion

    #region Methods

    private void OnKmzFileChanged(object sender, FileSystemEventArgs e)
    {
      KmzFilesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnKmzFileRenamed(object sender, RenamedEventArgs e)
    {
      KmzFilesChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Events

    public event EventHandler? KmzFilesChanged;

    #endregion
  }
}
