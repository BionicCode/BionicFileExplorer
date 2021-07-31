using BionicCode.Utilities.Net.Wpf.Converter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace BionicFileExplorer.Net.Wpf
{
  internal class BooleanToVisibiltyInvertConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var val = new InvertValueConverter().Convert(
new BooleanToVisibilityConverter().Convert(value, targetType, parameter, culture), targetType, parameter, culture);
      return val;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }

  internal class BytesFileLengthConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is long longValue && parameter is BytesUnit bytesUnit)
      {
        return Math.Ceiling(longValue / Math.Pow(2, (int)bytesUnit)).ToString("N0") + $" {bytesUnit}";
      }
      return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is string longStringValue && parameter is BytesUnit bytesUnit)
      {
        return long.Parse(longStringValue) * Math.Pow(2, (int)bytesUnit);
      }
      return Binding.DoNothing;
    }
  }
}
