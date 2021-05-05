using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Image = System.Windows.Controls.Image;

namespace Bionic.FileExplorer
{
  public class FileSystemItemIconSelector : IFileSystemItemIconSelector
  {
    public virtual object SelectFileIconSource(IFileSystemItemModel fileSystemItemModel)
    {
      switch (fileSystemItemModel)
      {
        case IFileSystemItemModel model when model.IsDirectory:
          return null;
        default:
        {
          using (var systemIcon = Icon.ExtractAssociatedIcon(fileSystemItemModel.Info.FullName))
          {
              return new Viewbox()
              {
                Child = new Image()
                {
                  Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        systemIcon.Handle,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions())
                }
              };
          }
        }
      }
    }

    public virtual object SelectClosedFolderIconSource(IFileSystemItemModel fileSystemItemModel)
    {
      switch (fileSystemItemModel)
      {
        case IFileSystemItemModel model when model.IsDirectory:
        {
          var resourceDictionary = new ResourceDictionary() {Source = new Uri(@"pack://application:,,,/Bionic.FileExplorer;component/Resources/Images/folder_closed.xaml", UriKind.Absolute) };
          return resourceDictionary["FolderClosedIcon"];
        }
        default: return null;
      }
    }

    public virtual object SelectOpenedFolderIconSource(IFileSystemItemModel fileSystemItemModel)
    {
      switch (fileSystemItemModel)
      {
        case IFileSystemItemModel model when model.IsDirectory:
        {
            var resourceDictionary = new ResourceDictionary() {Source = new Uri(@"pack://application:,,,/Bionic.FileExplorer;component/Resources/Images/folder_opened.xaml", UriKind.Absolute) };
              return resourceDictionary["FolderOpenedIcon"];
        }
        default: return null;
      }
    }
  }
}
