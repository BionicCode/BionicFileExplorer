using System.IO;

namespace BionicFileExplorer.Net
{
  public interface IFile : IFileSystemItemModel
  {
    new FileInfo Info { get; set; }
    FileSystemInfo IFileSystemItemModel.Info => this.Info;
  }
}