using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Bionic.FileExplorer.Zip;
using BionicUtilities.NetStandard.Generic;

namespace Bionic.FileExplorer.ViewModel
{
  public interface IFileSystemHandler 
  {
    Task InitializeRootFolderAsync(int preloadDepth, IEnumerable<string> startingPaths = null);
    void PreloadChildTree(IFileSystemItemModel fileSystemItem, int relativeDepth);
    Task AddFileSystemPathsAsync(IEnumerable<string> newFilePaths, int preloadDepth);
    void AddFileSystemItemModelToExplorerTree(IFileSystemItemModel fileSystemItem, int preloadDepth);
    void RemoveFileSystemItemModelToExplorerTree(IFileSystemItemModel fileSystemItem);
    Task<(bool IsSuccessful, DirectoryInfo DestinationDirectory)> ExtractArchive(FileSystemItemModel archiveFileItemModel);
    void ClearFileSystemTree();
    void SetIsExtracting();
    void ClearIsExtracting();
    void SetIsBusy();
    void ClearIsBusy();
    bool IsBusy { get; }
    bool HasExtractionsRunning { get; }
    FileSystemItemModel VirtualExplorerRootDirectory { get; }
    Dictionary<FileSystemItemModel, IFileExtractor> ArchiveExtractorInstanceTable { get; }
    Dictionary<FileSystemInfo, ExtractionProgressEventArgs> ExtractionProgressTable { get; }
    event EventHandler<ValueEventArgs<FileSystemItemModel>> FileSystemTreeCreated;
    event EventHandler<ValueEventArgs<FileSystemItemModel>> FileSystemTreeChanged;
  }
}