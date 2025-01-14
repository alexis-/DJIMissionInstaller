namespace DJI_Mission_Installer.Services
{
  using System.Windows;

  public class DialogService : IDialogService
  {
    #region Methods Impl

    public Task ShowErrorAsync(string title, string message)
    {
      return Application.Current.Dispatcher.InvokeAsync(() =>
      {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
      }).Task;
    }

    public Task ShowInfoAsync(string title, string message)
    {
      return Application.Current.Dispatcher.InvokeAsync(() =>
      {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
      }).Task;
    }

    #endregion
  }
}
