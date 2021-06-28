using System.Windows.Media;

namespace BionicFileExplorer.Net.Wpf
{
  public interface IFileSystemIconSelector
  {
    ImageSource SelectIconSource(IFileSystemItemModel itemModel, bool isDirectory, string fileSystemItemFullName);
  }
}
