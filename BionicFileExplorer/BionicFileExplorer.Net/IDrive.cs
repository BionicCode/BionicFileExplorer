
namespace BionicFileExplorer.Net
{
  public interface IDrive : IDirectory
  {
    bool IsReady { get; }
  }
}