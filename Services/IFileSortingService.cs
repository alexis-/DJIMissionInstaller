namespace DJI_Mission_Installer.Services
{
  using UI.Models;

  public interface IFileSortingService
  {
    IEnumerable<T> SortFiles<T>(IEnumerable<T> files, string sortMethod, bool ascending) where T : FileListItem;
  }
}
