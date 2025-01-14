namespace DJI_Mission_Installer.UI.Elements
{
  using System.Windows;
  using System.Windows.Controls;

  public class SortDirectionButton : Button
  {
    #region Constants & Statics

    public static readonly DependencyProperty SortAscendingProperty =
      DependencyProperty.Register(
        nameof(SortAscending),
        typeof(bool),
        typeof(SortDirectionButton),
        new PropertyMetadata(false));

    #endregion

    #region Properties & Fields - Public

    public bool SortAscending
    {
      get => (bool)GetValue(SortAscendingProperty);
      set => SetValue(SortAscendingProperty, value);
    }

    #endregion
  }
}
