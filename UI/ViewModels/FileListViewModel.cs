namespace DJI_Mission_Installer.UI.ViewModels
{
  using System.Collections.ObjectModel;
  using System.Windows.Input;
  using CommunityToolkit.Mvvm.Input;
  using Models;
  using Services;

  public class FileListViewModel : ViewModelBase
  {
    #region Properties & Fields - Non-Public

    private readonly IFileSortingService _sortingService;
    private          string              _sortMethod = "Date Modified";
    private          bool                _sortAscending;
    private          FileListItem?       _selectedItem;

    #endregion

    #region Constructors

    public FileListViewModel(string title, IFileSortingService sortingService)
    {
      Title                      = title;
      _sortingService            = sortingService;
      ToggleSortDirectionCommand = new RelayCommand(() => SortAscending = !SortAscending);
    }

    #endregion

    #region Properties & Fields - Public

    public string                             Title { get; }
    public ObservableCollection<FileListItem> Items { get; } = [];
    public ObservableCollection<string> SortMethods { get; } =
    [
      "Name",
      "Date Modified",
      "Size"
    ];

    public string SortMethod
    {
      get => _sortMethod;
      set
      {
        if (SetProperty(ref _sortMethod, value))
          ApplySort();
      }
    }

    public bool SortAscending
    {
      get => _sortAscending;
      set
      {
        if (SetProperty(ref _sortAscending, value))
          ApplySort();
      }
    }

    public FileListItem? SelectedItem
    {
      get => _selectedItem;
      set
      {
        if (SetProperty(ref _selectedItem, value))
          SelectedItemChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public ICommand ToggleSortDirectionCommand { get; }

    #endregion

    #region Methods

    public void UpdateItems(IEnumerable<FileListItem> items)
    {
      var oldItems    = Items.ToDictionary(i => i.FilePath);
      var sortedItems = _sortingService.SortFiles(items, SortMethod, SortAscending);

      Items.Clear();

      foreach (var item in sortedItems)
      {
        item.IsHighlighted = !oldItems.ContainsKey(item.FilePath);
        Items.Add(item);
      }
    }

    private void ApplySort()
    {
      var items = Items.ToList();
      Items.Clear();
      foreach (var item in _sortingService.SortFiles(items, SortMethod, SortAscending))
        Items.Add(item);
    }

    #endregion

    #region Events

    public event EventHandler? SelectedItemChanged;

    #endregion
  }
}
