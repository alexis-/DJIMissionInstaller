namespace DJI_Mission_Installer
{
  using System.IO;

  public static class Utils
  {
    #region Methods

    public static FileSystemWatcher WatchFolder(string                 folderPath,
                                                string                 filter,
                                                FileSystemEventHandler onChanged,
                                                RenamedEventHandler    onRenamed,
                                                NotifyFilters notifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                                  | NotifyFilters.FileName | NotifyFilters.DirectoryName)
    {
      var watcher = new FileSystemWatcher();

      watcher.Path = folderPath;

      // Watch for changes in LastAccess and LastWrite times, and
      // the renaming of files or directories.
      watcher.NotifyFilter = notifyFilter;

      // Only watch text files.
      watcher.Filter = filter;

      // Add event handlers.
      watcher.Changed += onChanged;
      watcher.Created += onChanged;
      watcher.Deleted += onChanged;
      watcher.Renamed += onRenamed;

      // Begin watching.
      watcher.EnableRaisingEvents = true;

      return watcher;
    }

    #endregion
  }
}
