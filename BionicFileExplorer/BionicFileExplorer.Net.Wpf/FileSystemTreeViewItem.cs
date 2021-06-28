using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BionicFileExplorer.Net.Wpf
{
  public class FileSystemTreeViewItem : TreeViewItem
  {
    public ImageSource IconSource
    {
      get => (ImageSource)GetValue(IconSourceProperty);
      set => SetValue(IconSourceProperty, value);
    }

    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register("IconSource", typeof(ImageSource), typeof(FileSystemTreeViewItem), new PropertyMetadata(default));

    static FileSystemTreeViewItem()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(FileSystemTreeViewItem), new FrameworkPropertyMetadata(typeof(FileSystemTreeViewItem)));
    }

    public FileSystemTreeViewItem()
    {
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
      base.PrepareContainerForItemOverride(element, item);
      var itemContainer = element as FileSystemTreeViewItem;
      var dataModel = item as IFileSystemItemModel;

      var itemIconProvider = FileSystemTreeView.GetFileSystemIconProvider();
      itemContainer.IconSource = itemIconProvider.SelectIconSource(dataModel, dataModel.IsDirectory, dataModel.Info.FullName);
    }

    protected override DependencyObject GetContainerForItemOverride() => new FileSystemTreeViewItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is FileSystemTreeViewItem;
  }
}
