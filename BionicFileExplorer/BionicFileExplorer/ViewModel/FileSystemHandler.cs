using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Bionic.FileExplorer.Zip;
using BionicUtilities.NetStandard.Generic;

namespace Bionic.FileExplorer.ViewModel
{
  internal class FileSystemHandler : IFileSystemHandler
  {
    public FileSystemHandler()
    {
      // Create the virtual root for later binding of its child collection to a TreeViews's items source. 
      this.VirtualExplorerRootDirectory = new FileSystemItemModel();
      this.ArchiveExtractorInstanceTable = new Dictionary<FileSystemItemModel, IFileExtractor>();
      this.ExtractionProgressTable = new Dictionary<FileSystemInfo, ExtractionProgressEventArgs>();
    }

    public async Task InitializeRootFolderAsync(int preloadDepth, IEnumerable<string> startingPaths = null)
    {
      // Preload available system drives
      List<DriveInfo> driveInfos = DriveInfo.GetDrives()
        .Where((driveInfo) => !driveInfo.DriveType.HasFlag(DriveType.CDRom))
        .OrderBy((driveInfo) => driveInfo.Name).ToList();

      await AddFileSystemPathsAsync(startingPaths ?? driveInfos.Select(driveInfo => driveInfo.RootDirectory.FullName), preloadDepth).ConfigureAwait(false);
      OnFileSystemTreeCreated();
    }

    public void PreloadChildTree(IFileSystemItemModel fileSystemItem, int relativeDepth)
    {
      if (!(fileSystemItem.Info is DirectoryInfo directoryInfo) || relativeDepth == 0)
      {
        return;
      }

      if (!fileSystemItem.ChildFileSystemItems.Any())
      {
        try
        {
          directoryInfo.EnumerateFileSystemInfos()
            .Select(fileSystemInfo => CreateFileSystemTreeItemModel(fileSystemItem, fileSystemInfo))
            .OrderBy(item => item, fileSystemItem)
            .ToList()
            .ForEach(
              childItem => Application.Current.Dispatcher?.Invoke(
                () => fileSystemItem.ChildFileSystemItems
                  .Add(childItem),
                DispatcherPriority.Background));
        }
        catch (UnauthorizedAccessException)
        {
          return;
        }
      }

      --relativeDepth;
      foreach (IFileSystemItemModel childFileSystemItem in fileSystemItem.ChildFileSystemItems)
      {
        PreloadChildTree(childFileSystemItem, relativeDepth);
      }
    }

    private void ReplaceArchiveWithExtractedContents(FileSystemItemModel archiveFileSystemItemModel, DirectoryInfo archiveExtractionDirectory)
    {
      //if (archiveFileSystemItemModel.ParentFileSystemItem.IsDirectory)
      //{
      //  FileSystemItemModel parent = archiveFileSystemItemModel.ParentFileSystemItem;
      //  ObservableCollection<FileSystemItemModel> parentContainigFileSystemElementCollection = parent.ChildFileSystemItems;
      //  int archiveFileIndex = parentContainigFileSystemElementCollection.IndexOf(archiveFileSystemItemModel);
      //  var archiveRepresentationNode = new FileSystemItemModel(
      //    parent.RootFileSystemItemModel,
      //    parent,
      //    archiveExtractionDirectory) {IsArchive = true, DisplayName = archiveFileSystemItemModel.Info.Name.Equals(archiveExtractionDirectory.Name, StringComparison.OrdinalIgnoreCase) ?  "" : archiveExtractionDirectory.Name };

      //  List<FileSystemItemModel> lazySubdirectories = archiveRepresentationNode.InitializeWithLazyTreeStructure();
      //  archiveRepresentationNode.SortChildren();
      //  FilterFileSystemTree(archiveRepresentationNode);

      //  Application.Current.Dispatcher.Invoke(
      //    () =>
      //    {
      //      parentContainigFileSystemElementCollection.Insert(archiveFileIndex, archiveRepresentationNode);
      //      parentContainigFileSystemElementCollection.Remove(archiveFileSystemItemModel);
      //      archiveFileSystemItemModel.ParentFileSystemItem.ChildFileSystemItems =
      //        parentContainigFileSystemElementCollection;
      //      if (archiveRepresentationNode.IsLazyLoading)
      //      {
      //        ObserveVirtualDirectories(lazySubdirectories);
      //      }
      //      archiveRepresentationNode.IsExpanded = true;
      //    },
      //    DispatcherPriority.Send);
      //}
    }

    public void SetIsBusy()
    {
      lock (this.syncLock)
      {
        this.busyCount = this.busyCount < 0 ? 1 : ++this.busyCount;
        this.IsBusy = this.busyCount > 0;
      }
    }

    public void ClearIsBusy()
    {
      lock (this.syncLock)
      {
        this.IsBusy = --this.busyCount > 0;
      }
    }

    public void SetIsExtracting()
    {
    }

    public void ClearIsExtracting()
    {
    }

    public async Task AddFileSystemPathsAsync(IEnumerable<string> newFileSystemItemModels, int preloadDepth)
    {
      await Task.Run(() =>
      {
        SetIsBusy();

        foreach (string path in newFileSystemItemModels)
        {
          FileSystemInfo newFileSystemInfo;
          if (File.Exists(path))
          {
            newFileSystemInfo = new FileInfo(path);
          }
          else if (Directory.Exists(path))
          {
            newFileSystemInfo = new DirectoryInfo(path);
          }
          else
          {
            continue;
          }

          IFileSystemItemModel fileSystemItemModel = CreateFileSystemTreeItemModel(this.VirtualExplorerRootDirectory, newFileSystemInfo);
          this.VirtualExplorerRootDirectory.ChildFileSystemItems.Add(fileSystemItemModel);
          OnFileSystemTreeChanged();
          Task.Run(() => PreloadChildTree(fileSystemItemModel, preloadDepth)).ContinueWith(task => ClearIsBusy());
        }
      }).ConfigureAwait(false);
    }

    public void AddFileSystemItemModelToExplorerTree(IFileSystemItemModel fileSystemItemModel, int preloadDepth)
    {
      if (fileSystemItemModel.ParentFileSystemItem == null)
      {
        fileSystemItemModel.ParentFileSystemItem = this.VirtualExplorerRootDirectory;
      }
      this.VirtualExplorerRootDirectory.ChildFileSystemItems.Add(fileSystemItemModel);
      this.VirtualExplorerRootDirectory.SortChildrenAsync();
      OnFileSystemTreeChanged();
      Task.Run(() => PreloadChildTree(fileSystemItemModel, preloadDepth));
    }

    public void RemoveFileSystemItemModelToExplorerTree(IFileSystemItemModel fileSystemItemModel)
    {
      this.VirtualExplorerRootDirectory.ChildFileSystemItems.Remove(fileSystemItemModel);
      OnFileSystemTreeChanged();
    }

    private IFileSystemItemModel CreateFileSystemTreeItemModel(IFileSystemItemModel parentFileSystemItem, FileSystemInfo pathInfo)
    {
      IFileSystemItemModel fileSystemTreeElement = parentFileSystemItem.CreateModel();
      fileSystemTreeElement.ParentFileSystemItem = parentFileSystemItem;
      fileSystemTreeElement.Info = pathInfo;
      fileSystemTreeElement.DisplayName = pathInfo.Name;

      fileSystemTreeElement.IsDirectory = pathInfo is DirectoryInfo;

      fileSystemTreeElement.IsArchive = !fileSystemTreeElement.IsDirectory
                                        && FileExtractor.FileIsArchive(fileSystemTreeElement.Info as FileInfo);

      fileSystemTreeElement.IsSystem = pathInfo.Attributes.HasFlag(FileAttributes.System);
      fileSystemTreeElement.IsHidden = pathInfo.Attributes.HasFlag(FileAttributes.Hidden);
      fileSystemTreeElement.IsDrive = pathInfo is DirectoryInfo directoryInfo && directoryInfo.Root.FullName.Equals(
                                            directoryInfo.FullName,
                                            StringComparison.OrdinalIgnoreCase);
      return fileSystemTreeElement;
    }

    public void ClearFileSystemTree()
    {
      this.VirtualExplorerRootDirectory.ChildFileSystemItems
        .Where((fileSystemTreeElement) => !fileSystemTreeElement.IsSystem && !fileSystemTreeElement.IsDirectory)
        .ToList()
        .ForEach(
          (fileSystemTreeElement) =>
          {
            this.VirtualExplorerRootDirectory.ChildFileSystemItems.Remove(fileSystemTreeElement);
            //fileSystemTreeElement.ExpandedChanged -= ToggleDragDropHintOnTopLevelDirectoryExpanded;
          });
      OnFileSystemTreeChanged();

//      UpdateFileSystemElementTreeState(FileSystemItemModel.EmptyNode);
    }

    public async Task<(bool IsSuccessful, DirectoryInfo DestinationDirectory)> ExtractArchive(FileSystemItemModel archiveFileItemModel)
    {
      if (archiveFileItemModel.IsDirectory || !archiveFileItemModel.Info.Exists)
      {
        return (false, new DirectoryInfo(@"c:\"));
      }

      SetIsBusy();
      var fileExtractor = new SharpCompressLibArchiveExtractor();
      lock (this.syncLock)
      {
        if (this.ArchiveExtractorInstanceTable.ContainsKey(archiveFileItemModel))
        {
          ClearIsBusy();
          return (false, new DirectoryInfo(@"c:\"));
        }

        this.ArchiveExtractorInstanceTable.Add(archiveFileItemModel, fileExtractor);
      }

      SetIsExtracting();

      var progressReporter = new Progress<ExtractionProgressEventArgs>(ReportExtractionProgress);

      (bool IsSuccessful, DirectoryInfo DestinationDirectory) extractionResult = await
        fileExtractor.ExtractZipArchiveAsync(archiveFileItemModel.Info as FileInfo, progressReporter);

      // Replace archive file with directory representation in explorer after extraction completed
      if (extractionResult.IsSuccessful)
      {
        ReplaceArchiveWithExtractedContents(archiveFileItemModel, extractionResult.DestinationDirectory);
      }

      CleanUpExtraction(archiveFileItemModel);
      return extractionResult;
    }

    private void CleanUpExtraction(FileSystemItemModel archiveFileItemModel)
    {
      ClearIsExtracting();
      lock (this.syncLock)
      {
        this.ArchiveExtractorInstanceTable.Remove(archiveFileItemModel);
        this.ExtractionProgressTable.Remove(archiveFileItemModel.Info);
      }
      ClearIsBusy();
    }

    //private void UpdateFileSystemElementTreeState(FileSystemItemModel fileSystemItemModel)
    //{
    //  this.IsExplorerInDefaultState = (fileSystemItemModel.IsEmptyElement || 
    //                                   fileSystemItemModel.IsSystemDirectory && !fileSystemItemModel.IsExpanded)
    //                                  && this.VirtualExplorerRootDirectory.ChildFileSystemItems
    //                                    .All((topLevelElement) => topLevelElement.IsSystemDirectory 
    //                                                              && !topLevelElement.IsExpanded 
    //                                                              || !this.VirtualExplorerRootDirectory.ChildFileSystemItems.Any());
    //}

    //public void ObserveTopLevelDirectoryIsExpanded(FileSystemItemModel topLevelDirectory) => topLevelDirectory.ExpandedChanged += ToggleDragDropHintOnTopLevelDirectoryExpanded;

    //private void ToggleDragDropHintOnTopLevelDirectoryExpanded(object sender, ValueChangedEventArgs<bool> e)
    //{
    //  UpdateFileSystemElementTreeState(sender as FileSystemItemModel);
    //}


    private void ReportExtractionProgress(ExtractionProgressEventArgs e)
    {
      Application.Current.Dispatcher?.InvokeAsync(
        () =>
        {
          if (!this.ExtractionProgressTable.ContainsKey(e.ArchiveInfo))
          {
            this.ExtractionProgressTable.Add(e.ArchiveInfo, e);
            return;
          }

          if (e.PercentageRead <= this.ExtractionProgressTable[e.ArchiveInfo].PercentageRead)
          {
            return;
          }

          this.ExtractionProgressTable[e.ArchiveInfo] = e;
        }, DispatcherPriority.Render);
    }

    private bool CanExecuteRemoveFile(object obj) => obj is IFileSystemItemModel fileSystemTreeElement && !fileSystemTreeElement.IsSystem;


    private int busyCount;
    private int runningExtractionsCount;

    private readonly object syncLock = new object();

    /// <inheritdoc />
    public event EventHandler<ValueEventArgs<FileSystemItemModel>> FileSystemTreeCreated;

    /// <inheritdoc />
    public event EventHandler<ValueEventArgs<FileSystemItemModel>> FileSystemTreeChanged;

    public FileSystemItemModel VirtualExplorerRootDirectory { get; private set; }

    public bool IsBusy { get; private set; }

    public Dictionary<FileSystemItemModel, IFileExtractor> ArchiveExtractorInstanceTable { get; set; }

    public Dictionary<FileSystemInfo, ExtractionProgressEventArgs> ExtractionProgressTable { get; set; }

    public bool HasExtractionsRunning { get; }

    protected virtual void OnFileSystemTreeCreated()
    {
      this.FileSystemTreeCreated?.Invoke(this, new ValueEventArgs<FileSystemItemModel>(this.VirtualExplorerRootDirectory));
    }

    protected virtual void OnFileSystemTreeChanged()
    {
      this.FileSystemTreeChanged?.Invoke(this, new ValueEventArgs<FileSystemItemModel>(this.VirtualExplorerRootDirectory));
    }
  }
}
