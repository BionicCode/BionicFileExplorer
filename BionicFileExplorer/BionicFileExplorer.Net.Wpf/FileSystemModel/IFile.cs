using System.IO;

namespace BionicFileExplorer.Net.Wpf.FileSystemModel
{
  public interface IFile : IFileSystemItemModel
  {
    new FileInfo Info { get; set; }
    FileSystemInfo IFileSystemItemModel.Info => Info;
  }
}