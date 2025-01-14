namespace DJI_Mission_Installer.UI.ViewModels
{
  using System.ComponentModel;
  using System.Runtime.CompilerServices;

  public abstract class ViewModelBase : INotifyPropertyChanged
  {
    #region Methods

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
      if (EqualityComparer<T>.Default.Equals(field, value)) return false;

      field = value;
      OnPropertyChanged(propertyName);
      return true;
    }

    #endregion

    #region Events

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion
  }
}
