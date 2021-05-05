namespace Bionic.FileExplorer
{
  public interface IFileSystemItemIconSelector
  {
    object SelectFileIconSource(IFileSystemItemModel fileSystemItemModel);
    object SelectOpenedFolderIconSource(IFileSystemItemModel fileSystemItemModel);
    object SelectClosedFolderIconSource(IFileSystemItemModel fileSystemItemModel);
  }
}
