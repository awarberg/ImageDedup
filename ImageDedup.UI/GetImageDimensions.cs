using System.Globalization;
using System.Windows.Data;
using System.Windows;
using SixLabors.ImageSharp;

namespace ImageDedup.UI
{
    public class GetImageDimensions : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            using var image = Image.Load(path);
            return $"{image.Width}x{image.Height}px";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
