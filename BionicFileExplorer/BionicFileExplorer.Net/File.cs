using System.IO;

namespace BionicFileExplorer.Net
{
  public class File : FileSystemItemModel, IFile
  {
    public File(FileInfo fileInfo, IDirectory parentDirectoryItem) : base(fileInfo, parentDirectoryItem)
    {
      this.Info = fileInfo;
      this.IsDirectory = false;
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
      get => this.info;
      set
      {
        this.info = value;
        OnPropertyChanged();
      }
    }

    private FileInfo info;
  }
}
