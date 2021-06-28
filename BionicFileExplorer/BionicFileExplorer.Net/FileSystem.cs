using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BionicFileExplorer.Net
{
  public class FileSystem
  {
    public FileSystem()
    {
      this.FileSystemEnumerationOptions = new EnumerationOptions() 
      { AttributesToSkip = 0,
        ReturnSpecialDirectories = false, 
        IgnoreInaccessible = true        
      };

      this.IsShowingFileExtensions = true;
      this.FileSystemItemFactory = (fileSystemInfo, parent, isDrive) => new FileSystemItemModel(fileSystemInfo, parent, isDrive);
      this.FileFactory = (fileInfo, parent) => new File(fileInfo, parent);
      this.DirectoryFactory = (directoryInfo, parent) => new Directory(directoryInfo, parent);
      this.DriveFactory = (driveInfo) => new Drive(driveInfo, this.FileSystemMount);
      this.InternalDrives = new ObservableCollection<DriveInfo>();
      this.Drives = new ReadOnlyObservableCollection<DriveInfo>(this.InternalDrives);
      this.InternalFileSystemRoot = new ObservableCollection<IFileSystemItemModel>();
      this.FileSystemRoot = new ReadOnlyObservableCollection<IFileSystemItemModel>(this.InternalFileSystemRoot);
      this.FileSystemRootChangeObservers = new Dictionary<string, FileSystemWatcher>();
      this.Index = new Dictionary<string, IFileSystemItemModel>();
      this.HiddenIndex = new Dictionary<string, IFileSystemItemModel>();
      this.SystemIndex = new Dictionary<string, IFileSystemItemModel>();

      this.FileSystemMount = this.DirectoryFactory.Invoke(null, null);
      this.SyncObject = new object();
      Application.Current.Dispatcher.Invoke(() =>
      {
        BindingOperations.EnableCollectionSynchronization(this.InternalDrives, this.SyncObject);
        BindingOperations.EnableCollectionSynchronization(this.Drives, this.SyncObject);
        BindingOperations.EnableCollectionSynchronization(this.InternalFileSystemRoot, this.SyncObject);
        BindingOperations.EnableCollectionSynchronization(this.FileSystemRoot, this.SyncObject);
      }, System.Windows.Threading.DispatcherPriority.Normal);
    }

    public async Task ShowFileExtensionsAsync()
    {
      if (this.IsShowingFileExtensions)
      {
        return;
      }

      this.IsShowingFileExtensions = true;

      await Task.Run(() =>
      {
        foreach (IFileSystemItemModel fileSystemModel in this.Index.Values.Where(fileSystemModel => !fileSystemModel.IsDirectory && !fileSystemModel.IsDrive))
        {
          fileSystemModel.IsHidingExtension = false;
        }
      });
    }

    public async Task HideFileExtensionsAsync()
    {
      if (!this.IsShowingFileExtensions)
      {
        return;
      }

      this.IsShowingFileExtensions = false;

      await Task.Run(() =>
      {
        foreach (IFileSystemItemModel fileSystemModel in this.Index.Values.Where(fileSystemModel => !fileSystemModel.IsDirectory && !fileSystemModel.IsDrive))
        {
          fileSystemModel.IsHidingExtension = true;
        }
      });
    }

    public async Task RefreshAsync(int initialRealizedFileSystemTreeLevels = FileSystem.defaultInitializationDepth) => await RefreshAsync(CancellationToken.None, initialRealizedFileSystemTreeLevels);

    public async Task RefreshAsync(CancellationToken cancellationToken, int initialRealizedFileSystemTreeLevels = FileSystem.defaultInitializationDepth)
    {
      if (initialRealizedFileSystemTreeLevels < 1)
      {
        throw new ArgumentOutOfRangeException("Level count can't be smaller than 1");
      }
      this.InternalDrives.Clear();
      this.FileSystemMount.ChildFileSystemItems.Clear();
      this.InternalFileSystemRoot.Clear();
      await InitializeFileSystemTreeAsync(cancellationToken, initialRealizedFileSystemTreeLevels);
    }

    public async Task InitializeFileSystemTreeAsync(int initialRealizedFileSystemTreeLevels = FileSystem.defaultInitializationDepth) => await InitializeFileSystemTreeAsync(CancellationToken.None, initialRealizedFileSystemTreeLevels);

    public async Task InitializeFileSystemTreeAsync(CancellationToken cancellationToken, int initialRealizedFileSystemTreeLevels = FileSystem.defaultInitializationDepth, bool isSortingEnabled = false)
    {
      if (initialRealizedFileSystemTreeLevels < 1)
      {
        throw new ArgumentOutOfRangeException("Level count can't be smaller than 1");
      }
      await Task.Run(() =>
      {
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
          this.InternalDrives.Add(drive);
        }
        var rootItems = CreateFileSystemRootItems(this.InternalDrives, cancellationToken);
        foreach (IFileSystemItemModel rootItem in rootItems)
        {
          this.FileSystemMount.ChildFileSystemItems.Add(rootItem);
          this.InternalFileSystemRoot.Add(rootItem);
        }
        RealizeFromRoot(initialRealizedFileSystemTreeLevels, cancellationToken, isSortingEnabled);
      }, cancellationToken);
    }

    public async Task<bool> TryRealizeChildrenAsync(IEnumerable<IDirectory> treeLevelDirectories, int levelCount = FileSystem.defaultInitializationDepth, bool isSortingEnabled = false) => await Task.Run(() => TryRealizeChildren(treeLevelDirectories, CancellationToken.None, levelCount, isSortingEnabled)).ConfigureAwait(false);

    public async Task<bool> TryRealizeChildrenAsync(IEnumerable<IDirectory> treeLevelDirectories, CancellationToken cancellationToken, int levelCount = FileSystem.defaultInitializationDepth, bool isSortingEnabled = false) => await Task.Run(() => TryRealizeChildren(treeLevelDirectories, cancellationToken, levelCount, isSortingEnabled), cancellationToken).ConfigureAwait(false);

    private bool TryRealizeChildren(IEnumerable<IDirectory> treeLevelDirectories, CancellationToken cancellationToken, int levelCount, bool isSortingEnabled)
    {
      if (levelCount < 1)
      {
        throw new ArgumentOutOfRangeException("Level count can't be smaller than 1");
      }

      bool hasRealizedItem = false;

      if (treeLevelDirectories == null)
      {
        return false;
      }

      cancellationToken.ThrowIfCancellationRequested();

      int remainingLevelCount = levelCount;
      var breadthFirstQueue = new List<IDirectory>();

      foreach (IDirectory parentDirectory in treeLevelDirectories.Where(directory => !directory.IsRealized))
      {
        foreach (FileSystemInfo childInfo in parentDirectory.Info.EnumerateFileSystemInfos("*", this.FileSystemEnumerationOptions))
        {
          IFileSystemItemModel childItem;
          if (childInfo is DirectoryInfo directory)
          {
            childItem = this.DirectoryFactory.Invoke(directory, parentDirectory);
            breadthFirstQueue.Add((IDirectory)childItem);
          }
          else
          {
            childItem = this.FileFactory.Invoke(childInfo as FileInfo, parentDirectory);
          }

          IndexFileSystemItem(childItem);
          parentDirectory.ChildFileSystemItems.Add(childItem);
          hasRealizedItem = true;
        }
        if (isSortingEnabled)
        {
          parentDirectory.Sort(); 
        }
      }

      if (--remainingLevelCount > 0)
      {
        cancellationToken.ThrowIfCancellationRequested();
        TryRealizeChildren(breadthFirstQueue, cancellationToken, remainingLevelCount, isSortingEnabled);
      }
      return hasRealizedItem;
    }

    private bool RealizeFromRoot(int levelCount, CancellationToken cancellationToken, bool isSortingEnabled)
    {
      bool hasRealizedItem = false;
      var nextLevelBreadthFirstQueue = new Queue<IDirectory>(this.InternalFileSystemRoot
                .OfType<IDrive>()
                .Where(rootItem => rootItem.IsReady));

      int remainingLevels = levelCount;
      while (remainingLevels > 0 && nextLevelBreadthFirstQueue.Any())
      {
        remainingLevels--;
        var currentLevelBreadthFirstQueue = new Queue<IDirectory>(nextLevelBreadthFirstQueue);
        nextLevelBreadthFirstQueue.Clear();
        while (currentLevelBreadthFirstQueue.TryDequeue(out IDirectory parentDirectory))
        {
          parentDirectory.ChildFileSystemItems.Clear();
          foreach (FileSystemInfo childInfo in parentDirectory.Info.EnumerateFileSystemInfos("*", this.FileSystemEnumerationOptions))
          {
            IFileSystemItemModel childItem;
            if (childInfo is DirectoryInfo directory)
            {
              childItem = this.DirectoryFactory.Invoke(directory, parentDirectory);
              nextLevelBreadthFirstQueue.Enqueue((IDirectory)childItem);
            }
            else
            {
              childItem = this.FileFactory.Invoke(childInfo as FileInfo, parentDirectory);
            }

            IndexFileSystemItem(childItem);
            parentDirectory.ChildFileSystemItems.Add(childItem);
            hasRealizedItem = true;
          }

          if (isSortingEnabled)

          {
            parentDirectory.Sort();
          }          
        }
        cancellationToken.ThrowIfCancellationRequested();
      }
      return hasRealizedItem;
    }

    private IEnumerable<IFileSystemItemModel> CreateFileSystemRootItems(IEnumerable<DriveInfo> drives, CancellationToken cancellationToken)
    {
      var rootItems = new List<IFileSystemItemModel>();
      foreach (DriveInfo drive in drives)
      {
        cancellationToken.ThrowIfCancellationRequested();
        var rootItem = this.DriveFactory.Invoke(drive);
        rootItems.Add(rootItem);
        IndexFileSystemItem(rootItem);

        if (rootItem.IsReady && !this.FileSystemRootChangeObservers.ContainsKey(rootItem.Info.FullName))
        {
          var driveChangedObserver = new FileSystemWatcher(rootItem.Info.FullName);
          driveChangedObserver.Changed += OnFileSystemItemChanged;
          driveChangedObserver.Renamed += OnFileSystemItemRenamed;
          driveChangedObserver.Created += OnFileSystemItemCreated;
          driveChangedObserver.Deleted += OnFileSystemItemChanged;
          driveChangedObserver.Error += OnFileSystemWatcherError;
          driveChangedObserver.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
          driveChangedObserver.IncludeSubdirectories = true;
          driveChangedObserver.InternalBufferSize = 4096 * 4;
          driveChangedObserver.EnableRaisingEvents = true;

          this.FileSystemRootChangeObservers.Add(rootItem.Info.FullName, driveChangedObserver);
        }
      }
      return rootItems;
    }

    private void OnFileSystemWatcherError(object sender, ErrorEventArgs e)
    {
      ;
    }

    private void OnFileSystemItemRenamed(object sender, RenamedEventArgs e)
    {
      if (!this.Index.TryGetValue(e.OldFullPath, out IFileSystemItemModel renamedFileSystemItem))
      {
        // File system item is not indexed/never visited before.
        // Changes will be visible the first time this folder or its child items are indexed/visited
        return;
      }

      RemoveFileSystemItemFromIndex(renamedFileSystemItem);
      renamedFileSystemItem.RefreshInfo(e.FullPath);
      IndexFileSystemItem(renamedFileSystemItem);
    }

    private void OnFileSystemItemCreated(object sender, FileSystemEventArgs e)
    {
      if (System.IO.Directory.Exists(e.FullPath))
      {
        var createdDirectoryInfo = new DirectoryInfo(e.FullPath);
        if (!this.Index.TryGetValue(createdDirectoryInfo.Parent.FullName, out IFileSystemItemModel parentFileSystemItem) 
          || parentFileSystemItem is not IDirectory parentDirectoryItem)
        {
          // File system item is not indexed/never visited before.
          // Changes will be visible the first time this folder or its child items are indexed/visited
          return;
        }
        var createdDirectoryItem = this.DirectoryFactory.Invoke(createdDirectoryInfo, parentDirectoryItem);
        AddNewFileSystemInfo(createdDirectoryItem, parentDirectoryItem);
      }
      else if (System.IO.File.Exists(e.FullPath))
      {
        var createdFileInfo = new FileInfo(e.FullPath);
        if (!this.Index.TryGetValue(createdFileInfo.DirectoryName, out IFileSystemItemModel parentFileSystemItem) 
          || parentFileSystemItem is not IDirectory parentDirectoryItem)
        {
          // File system item is not indexed/never visited before.
          // Changes will be visible the first time this folder or its child items are indexed/visited
          return;
        }
        var createdFileItem = this.FileFactory.Invoke(createdFileInfo, parentDirectoryItem);
        AddNewFileSystemInfo(createdFileItem, parentDirectoryItem);
      }
    }

    private void IndexFileSystemItem(IFileSystemItemModel fileSystemItem)
    {
      this.Index.TryAdd(fileSystemItem.Info.FullName.Trim(Path.DirectorySeparatorChar), fileSystemItem);

      if (fileSystemItem.IsHidden && !fileSystemItem.ParentFileSystemItem.IsHidden)
      {
        this.HiddenIndex.TryAdd(fileSystemItem.Info.FullName.Trim(Path.DirectorySeparatorChar), fileSystemItem);
      }

      if (fileSystemItem.IsSystem && !fileSystemItem.ParentFileSystemItem.IsSystem)
      {
        this.SystemIndex.TryAdd(fileSystemItem.Info.FullName.Trim(Path.DirectorySeparatorChar), fileSystemItem);
      }
    }

    private void RemoveFileSystemItemFromIndex(IFileSystemItemModel fileSystemItem)
    {
      this.Index.Remove(fileSystemItem.Info.FullName);
      this.HiddenIndex.Remove(fileSystemItem.Info.FullName);
      this.SystemIndex.Remove(fileSystemItem.Info.FullName);
    }

    private void RemoveFileSystemItemFromIndex(FileSystemInfo fileSystemInfo)
    {
      this.Index.Remove(fileSystemInfo.FullName);
      this.HiddenIndex.Remove(fileSystemInfo.FullName);
      this.SystemIndex.Remove(fileSystemInfo.FullName);
    }

    private void OnFileSystemItemChanged(object sender, FileSystemEventArgs e)
    {
      FileSystemInfo fileSystemInfo = System.IO.Directory.Exists(e.FullPath) 
        ? new DirectoryInfo(e.FullPath)
        : new FileInfo(e.FullPath);
      HandleFileSystemInfoChanged(fileSystemInfo, e.ChangeType); 
    }

    private void HandleFileSystemInfoChanged(FileSystemInfo fileSystemInfo, WatcherChangeTypes changeType)
    {
      if (!((fileSystemInfo is FileInfo fileInfo && this.Index.TryGetValue(fileInfo.DirectoryName, out IFileSystemItemModel parentFileSystemItem)
          || fileSystemInfo is DirectoryInfo directoryInfo && this.Index.TryGetValue(directoryInfo.Parent.FullName, out parentFileSystemItem)) 
          && parentFileSystemItem is IDirectory parentDirectoryItem && parentDirectoryItem.IsRealized))
      {
        // Parent folder is not indexed/never visited before.
        // Changes will be visible the first time this folder and its child items are indexed/visited
        return;
      }

      switch (changeType)
      {
        case WatcherChangeTypes.Deleted:
          if (this.Index.TryGetValue(fileSystemInfo.FullName, out IFileSystemItemModel fileItemToRemove))
          {
            parentDirectoryItem.ChildFileSystemItems.Remove(fileItemToRemove);
            RemoveFileSystemItemFromIndex(fileItemToRemove);
          }
          break;
        case WatcherChangeTypes.Changed:
          if (this.Index.TryGetValue(fileSystemInfo.FullName, out IFileSystemItemModel changedFileItem))
          {
            changedFileItem.RefreshInfo();
            IndexFileSystemItem(changedFileItem);
          }
          break;
        case WatcherChangeTypes.All:
          break;
        default:
          break;
      }
    }

    private void AddNewFileSystemInfo(IFileSystemItemModel fileSystemItem, IDirectory parentFolderItem)
    {
      parentFolderItem.ChildFileSystemItems.Add(fileSystemItem);
      IndexFileSystemItem(fileSystemItem);
    }

    public ReadOnlyObservableCollection<DriveInfo> Drives { get; }
    public ReadOnlyObservableCollection<IFileSystemItemModel> FileSystemRoot { get; }
    public IDirectory FileSystemMount { get; private set; }
    private EnumerationOptions FileSystemEnumerationOptions { get; }
    private Func<FileSystemInfo, IDirectory, bool, IFileSystemItemModel> FileSystemItemFactory { get; }
    private Func<FileInfo, IDirectory, IFile> FileFactory { get; }
    private Func<DirectoryInfo, IDirectory, IDirectory> DirectoryFactory { get; }
    private Func<DriveInfo, IDrive> DriveFactory { get; }
    private ObservableCollection<DriveInfo> InternalDrives { get; }
    private ObservableCollection<IFileSystemItemModel> InternalFileSystemRoot { get; }
    private Dictionary<string, FileSystemWatcher> FileSystemRootChangeObservers { get; }
    public Dictionary<string, IFileSystemItemModel> Index { get; }
    public Dictionary<string, IFileSystemItemModel> HiddenIndex { get; }
    public Dictionary<string, IFileSystemItemModel> SystemIndex { get; }
    private bool IsShowingFileExtensions { get; set; }
    private object SyncObject { get; set; }
    private const int defaultInitializationDepth = 2;
  }
}
