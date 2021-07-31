using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace BionicFileExplorer.Net.Wpf.FileSystemModel
{
  public class SpecialDirectory : Directory
  {
    protected SpecialDirectory(IDirectory parentFolder) : base(null, parentFolder)
    {
      IsSpecial = true;
      IsDrive = false;
      IsNavigationalDirectory = true;
      ParentFileSystemItem = parentFolder;
    }

    public static IDirectory Create(IDirectory parentFolder) => new SpecialDirectory(parentFolder);
  }

  public class Directory : FileSystemItemModel, IDirectory
  {
    public Directory(string directoryPath) : this(new DirectoryInfo(directoryPath))
    {
    }

    public Directory(DirectoryInfo directoryInfo) : this(directoryInfo, null)
    {
    }

    public Directory(DirectoryInfo directoryInfo, IDirectory parentDirectoryItem) : base(directoryInfo, parentDirectoryItem)
    {
      Info = directoryInfo;
      IsDirectory = true;
      SyncObject = new object();
      ChildFileSystemItems = new ObservableCollection<IFileSystemItemModel>();
      Application.Current.Dispatcher.InvokeAsync(() =>
      {
        BindingOperations.EnableCollectionSynchronization(ChildFileSystemItems, SyncObject);
      }, System.Windows.Threading.DispatcherPriority.Normal);
    }

    public void ApplyActionOnSubTree(Action<IFileSystemItemModel> action, Predicate<IFileSystemItemModel> predicate)
    {
      foreach (IFileSystemItemModel childItem in ChildFileSystemItems)
      {
        if (predicate?.Invoke(childItem) ?? true)
        {
          action?.Invoke(childItem);
        }

        if (childItem is IDirectory subdirectory && subdirectory.ChildFileSystemItems.Any())
        {
          subdirectory.ApplyActionOnSubTree(action, predicate);
        }
      }
    }

    public IEnumerable<IDirectory> GetSubdirectories() => ChildFileSystemItems.OfType<IDirectory>();

    public IEnumerable<IFile> GetFiles() => ChildFileSystemItems.OfType<IFile>();

    public void Sort(Comparison<IFileSystemItemModel> comparison) => (CollectionViewSource.GetDefaultView(ChildFileSystemItems) as ListCollectionView).CustomSort = Comparer<IFileSystemItemModel>.Create(comparison);

    public void Sort() => (CollectionViewSource.GetDefaultView(ChildFileSystemItems) as ListCollectionView).CustomSort = this;

    private int FileSystemTreeSortComparison(IFileSystemItemModel itemModelA, IFileSystemItemModel itemModelB)
    {
      if (itemModelA is SpecialDirectory)
      {
        return -1;
      }

      if (itemModelB is SpecialDirectory)
      {
        return 1;
      }

      //if (itemModelA.IsSystem && itemModelA.IsDirectory && !itemModelB.IsSystem && !itemModelB.IsDirectory)
      //{
      //  return -1;
      //}

      //if (!itemModelA.IsSystem && !itemModelA.IsDirectory && itemModelB.IsSystem && itemModelB.IsDirectory)
      //{
      //  return 1;
      //}

      if (itemModelA.IsDirectory && !itemModelB.IsDirectory)
      {
        return -1;
      }

      if (!itemModelA.IsDirectory && itemModelB.IsDirectory)
      {
        return 1;
      }

      return itemModelA.DisplayName.CompareTo(itemModelB.DisplayName);

      //int fileNameAFirstDotIndex = fileNameAWhithoutExtension.IndexOf(".", StringComparison.OrdinalIgnoreCase);
      //int fileNameBFirstDotIndex = fileNameBWhithoutExtension.IndexOf(".", StringComparison.OrdinalIgnoreCase);

      //// Both file names doesn't contain a dot separator (normal case)
      //if (fileNameAFirstDotIndex.Equals(-1) && fileNameBFirstDotIndex.Equals(-1))
      //{
      //  return CompareAlphabetically(itemModelA.Info.Name, itemModelB.Info.Name);
      //}

      //if (fileNameAFirstDotIndex.Equals(-1) || fileNameBFirstDotIndex.Equals(-1))
      //{
      //  return CompareAlphabetically(fileNameAWhithoutExtension, fileNameBWhithoutExtension);
      //}

      //// If both names contain a dot prefix separator --> compare prefixes
      //// File without this separator have precedence over those that contain one (on matching prefix)
      //string prefixFileNameA = fileNameAWhithoutExtension.Substring(0, fileNameAFirstDotIndex);
      //string prefixFileNameB = fileNameBWhithoutExtension.Substring(0, fileNameBFirstDotIndex);

      //int prefixCompareResult = CompareAlphabetically(prefixFileNameA, prefixFileNameB);

      //// Prefixes are equal --> compare suffix
      //if (prefixCompareResult.Equals(0))
      //{
      //  string suffixFileNameA = fileNameAWhithoutExtension.Substring(fileNameAFirstDotIndex + 1);
      //  string suffixFileNameB = fileNameBWhithoutExtension.Substring(fileNameBFirstDotIndex + 1);

      //  // Suffix is numeric
      //  if (
      //    int.TryParse(
      //      suffixFileNameA,
      //      NumberStyles.Integer | NumberStyles.Number,
      //      NumberFormatInfo.InvariantInfo,
      //      out int numberA)
      //    && int.TryParse(
      //      suffixFileNameB,
      //      NumberStyles.Integer | NumberStyles.Number,
      //      NumberFormatInfo.InvariantInfo,
      //      out int numberB))
      //  {
      //    return numberA.CompareTo(numberB);
      //  }

      //  return CompareAlphabetically(itemModelA.Info.Name, itemModelB.Info.Name);
      //}

      //return prefixCompareResult;
    }

    private int CompareAlphabetically(string treeElementA, string treeElementB)
    {
      char punctuationA = char.IsPunctuation(treeElementA, 0) ? treeElementA[0] : ' ';
      char punctuationB = char.IsPunctuation(treeElementB, 0) ? treeElementA[0] : ' ';
      if (!char.IsWhiteSpace(punctuationA) && !char.IsWhiteSpace(punctuationB))
      {
        return punctuationA.CompareTo(punctuationB);
      }
      if (char.IsWhiteSpace(punctuationA) && char.IsWhiteSpace(punctuationB))
      {
        return string.Compare(treeElementA, treeElementB, StringComparison.OrdinalIgnoreCase);
      }

      return char.IsWhiteSpace(punctuationB) ? -1 : 1;
    }

    public int Compare(IFileSystemItemModel x, IFileSystemItemModel y) => FileSystemTreeSortComparison(x, y);

    public int Compare(object x, object y) =>
      x is IFileSystemItemModel fileSystemItemModelX && y is IFileSystemItemModel fileSystemItemModelY
      ? Compare(fileSystemItemModelX, fileSystemItemModelY)
      : Comparer<object>.Default.Compare(x, y);

    public int CompareTo(IFileSystemItemModel other)
    {
      if (ReferenceEquals(this, other))
      {
        return 0;
      }

      if (ReferenceEquals(null, other))
      {
        return 1;
      }

      return FileSystemTreeSortComparison(this, other);
    }

    new public DirectoryInfo Info
    {
      get => info;
      set
      {
        info = value;
        OnPropertyChanged();
      }
    }
    public bool IsRealized => ChildFileSystemItems.Any();

    public ObservableCollection<IFileSystemItemModel> ChildFileSystemItems { get; set; }

    protected object SyncObject { get; set; }
    public bool IsNavigationalDirectory { get; init; }

    private DirectoryInfo info;
  }
}
