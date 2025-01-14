namespace DJI_Mission_Installer.UI.Converters
{
  using System.Globalization;
  using System.Windows.Data;

  public class BooleanToDoubleConverter : IValueConverter
  {
    #region Properties & Fields - Public

    public double TrueValue  { get; set; }
    public double FalseValue { get; set; }

    #endregion

    #region Methods Impl

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return value is bool boolValue && boolValue ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return value is double doubleValue && Math.Abs(doubleValue - TrueValue) < double.Epsilon;
    }

    #endregion
  }
}
