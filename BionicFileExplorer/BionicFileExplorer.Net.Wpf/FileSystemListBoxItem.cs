using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BionicFileExplorer.Net.Wpf
{
  public class FileSystemListBoxItem : ListViewItem
  {
    public ImageSource IconSource
    {
      get => (ImageSource)GetValue(IconSourceProperty);
      set => SetValue(IconSourceProperty, value);
    }

    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register("IconSource", typeof(ImageSource), typeof(FileSystemListBoxItem), new PropertyMetadata(default));

    static FileSystemListBoxItem()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(FileSystemListBoxItem), new FrameworkPropertyMetadata(typeof(FileSystemListBoxItem)));
    }
  }
}
