namespace DJI_Mission_Installer.UI.Converters
{
  using System.Globalization;
  using System.Windows.Data;

  public class AspectRatioConverter : IValueConverter
  {
    #region Properties & Fields - Public

    public double Ratio { get; set; } = 0.5625; // Default 16:9 ratio

    #endregion

    #region Methods Impl

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is double width)
        return width * Ratio;

      return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
