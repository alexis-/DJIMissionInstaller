namespace DJI_Mission_Installer.Services
{
  using UI.Models;

  public class FileSortingService : IFileSortingService
  {
    #region Properties & Fields - Non-Public

    private readonly NaturalStringComparer _naturalComparer = new();

    #endregion

    #region Methods Impl

    public IEnumerable<T> SortFiles<T>(IEnumerable<T> files, string sortMethod, bool ascending)
      where T : FileListItem
    {
      var orderedFiles = sortMethod switch
      {
        "Name" => ascending
          ? files.OrderBy(f => f.DisplayName, _naturalComparer)
          : files.OrderByDescending(f => f.DisplayName, _naturalComparer),

        "Date Modified" => ascending
          ? files.OrderBy(f => f.LastModified)
          : files.OrderByDescending(f => f.LastModified),

        "Size" => ascending
          ? files.OrderBy(f => f.FileSize)
          : files.OrderByDescending(f => f.FileSize),

        _ => files
      };

      return orderedFiles;
    }

    #endregion
  }
}
