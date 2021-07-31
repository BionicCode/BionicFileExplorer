using System.IO;

namespace BionicFileExplorer.Net.Wpf.FileSystemModel
{
  public class File : FileSystemItemModel, IFile
  {
    public File(FileInfo fileInfo, IDirectory parentDirectoryItem) : base(fileInfo, parentDirectoryItem)
    {
      Info = fileInfo;
      IsDirectory = false;
    }

    public File(FileInfo fileInfo) : this(fileInfo, null)
    {
    }

    public File(string filePath, IDirectory parentDirectoryItem) : this(new FileInfo(filePath), parentDirectoryItem)
    {
    }

    public File(string filePath) : this(new FileInfo(filePath))
    {
    }

    public new FileInfo Info
    {
      get => info;
      set
      {
        info = value;
        OnPropertyChanged();
      }
    }

    private FileInfo info;
  }
}
