namespace DJI_Mission_Installer.UI.Models
{
  using System.ComponentModel;
  using System.Diagnostics;
  using System.IO;
  using System.Runtime.CompilerServices;
  using System.Windows.Media.Imaging;
  using DJI_Mission_Installer.Models;
  using Extensions;

  public class FileListItem : INotifyPropertyChanged
  {
    #region Properties & Fields - Non-Public

    private bool         _isSelected;
    private bool         _isHighlighted;
    private BitmapImage? _imageSource;

    #endregion

    #region Constructors

    public FileListItem(KmzFile kmzFile)
    {
      FileInfo = kmzFile;
      Type     = FileItemType.LocalKmz;
    }

    public FileListItem(WaypointFileInfo waypointFile)
    {
      FileInfo = waypointFile;
      Type     = FileItemType.DeviceWaypoint;

      LoadImage();
    }

    #endregion

    #region Properties & Fields - Public

    public object       FileInfo { get; }
    public FileItemType Type     { get; }

    public string DisplayName => Type switch
    {
      FileItemType.LocalKmz => ((KmzFile)FileInfo).Name,
      FileItemType.DeviceWaypoint => ((WaypointFileInfo)FileInfo).Id,
      _ => string.Empty
    };

    public string FilePath => Type switch
    {
      FileItemType.LocalKmz => ((KmzFile)FileInfo).FullPath,
      FileItemType.DeviceWaypoint => ((WaypointFileInfo)FileInfo).DeviceKmzPath,
      _ => string.Empty
    };

    public string DirectoryName => Type switch
    {
      FileItemType.LocalKmz => ((KmzFile)FileInfo).DirectoryName,
      FileItemType.DeviceWaypoint => Path.GetDirectoryName(((WaypointFileInfo)FileInfo).DeviceKmzPath) ?? string.Empty,
      _ => string.Empty
    };

    public DateTime LastModified => Type switch
    {
      FileItemType.LocalKmz => ((KmzFile)FileInfo).LastWriteTime,
      FileItemType.DeviceWaypoint => ((WaypointFileInfo)FileInfo).LastModified,
      _ => DateTime.MinValue
    };

    public ulong FileSize => Type switch
    {
      FileItemType.LocalKmz => ((KmzFile)FileInfo).FileSize,
      FileItemType.DeviceWaypoint => ((WaypointFileInfo)FileInfo).FileSize,
      _ => 0
    };

    public Uri? ImageUri => Type switch
    {
      FileItemType.DeviceWaypoint => ((WaypointFileInfo)FileInfo).GetLocalImageUri(),
      _ => null
    };
    
    public BitmapImage? ImageSource
    {
      get => _imageSource;
      private set => SetProperty(ref _imageSource, value);
    }

    public bool IsSelected
    {
      get => _isSelected;
      set => SetProperty(ref _isSelected, value);
    }

    public bool IsHighlighted
    {
      get => _isHighlighted;
      set => SetProperty(ref _isHighlighted, value);
    }

    #endregion

    #region Methods
    
    private void LoadImage()
    {
      var imageUri = ImageUri;
      if (imageUri == null) return;

      try
      {
        if (File.Exists(imageUri.AbsolutePath))
        {
          ImageSource = ImageHelper.LoadImageWithCopyOption(imageUri.AbsolutePath);
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Failed to load image: {ex.Message}");
      }
    }

    protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
      if (!EqualityComparer<T>.Default.Equals(field, value))
      {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    #endregion

    #region Events

    // INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion
  }
}
