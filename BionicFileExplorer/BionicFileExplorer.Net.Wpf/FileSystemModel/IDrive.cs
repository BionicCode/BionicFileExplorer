namespace BionicFileExplorer.Net.Wpf.FileSystemModel
{
  public interface IDrive : IDirectory
  {
    bool IsReady { get; }
  }
}