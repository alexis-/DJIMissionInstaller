namespace DJI_Mission_Installer.UI.Converters
{
  using System.Globalization;
  using System.Windows.Data;

  public class ComboBoxDisplayConverter : IMultiValueConverter
  {
    #region Methods

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      // First value is DisplayName, second is the object itself
      if (values[0] != null)
        return values[0].ToString(); // Return DisplayName if it exists

      if (values[1] != null)
        return values[1].ToString(); // Return the object's ToString() if no DisplayName

      return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
