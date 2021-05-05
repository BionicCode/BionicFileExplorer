using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Bionic.FileExplorer
{
  /// <summary>
  /// A fileSystemInfo object in the directory tree that describes a folder and its associated files
  /// <para/>
  /// and can be used as a model of several view items e.g. TreeViewItem or ListViewItem, since it supports binding properties like <see cref="IsExpanded"/> or <see cref="IsSelected"/>
  /// <para/>
  /// According to the invoked constructor the tree is automatically created from the single <see cref="Info"/> argument or explicitly constructed.
  /// </summary>
  public class FileSystemItemModel : IFileSystemItemModel
  {
    public FileSystemItemModel() : this(null, null, new List<IFileSystemItemModel>())
    {
    }

    /// <summary>
    /// Default constructor that gives full control over the tree srtucture
    /// </summary>
    /// <param name="parentFileSystemItem">Parent FileSystemItemModel</param>
    /// <param name="node">A fileSystemInfo object in the directory tree that describes a folder and its associated files</param>
    /// <param name="childElements">A collection of <see cref="FileSystemItemModel"/>s describing the subdirectories contained by this fileSystemInfo object</param>
    public FileSystemItemModel(FileSystemItemModel parentFileSystemItem, FileSystemInfo fileSystemElementInfo, IEnumerable<IFileSystemItemModel> childElements)
    {
      this.ParentFileSystemItem = parentFileSystemItem;
      this.Info = fileSystemElementInfo;
      this.DisplayName = this.Info?.Name ?? string.Empty;
      this.ChildFileSystemItems = new ObservableCollection<IFileSystemItemModel>(childElements);
      this.IsArchive = false;
      this.Id = Guid.NewGuid();
    }


    /// <summary>
    /// Full automatic constructor that creates the full tree where <param name="fileSystemInfo"></param> is the root. <para/>
    /// It populates the <see cref="SubdirectoryInfos"/> and the <see cref="ChildFileSystemItems"/> property which describes the files contained by the current fileSystemInfo directory.
    /// </summary>
    /// <param name="fileSystemInfo">A fileSystemInfo object in the directory tree that describes a folder and its associated files. Subdirectory and file info are created automatically from the fileSystemInfo where this fileSystemInfo is the root.</param>
    /// <param name="fileExtensionFilter">A flagged enum to filter the files to collect. Use FileExtensions.Any to collect all file types.</param>
    public FileSystemItemModel(FileSystemInfo fileSystemInfo) : this(null, null, new List<IFileSystemItemModel>())
    {
      this.Info = fileSystemInfo;
    }

    public virtual void SortChildrenAsync()
    {
      if (!this.ChildFileSystemItems.Any())
      {
        return;
      }

      this.ChildFileSystemItems = new ObservableCollection<IFileSystemItemModel>(this.ChildFileSystemItems.OrderBy(item => item, this).ToList());
      //(CollectionViewSource.GetDefaultView(this.ChildFileSystemItems) as ListCollectionView).CustomSort = this;

      //if ((CollectionViewSource.GetDefaultView(this.ChildFileSystemItems) as ListCollectionView).CustomSort == null)
      //{
      //}
    }

    public virtual IFileSystemItemModel CreateModel() => new FileSystemItemModel();

    // Uses preorder traversal
    public static bool TryGetDirectoryElementOf(IFileSystemItemModel currentItemModel, out IFileSystemItemModel directoryItemModel)
    {
      directoryItemModel = null;

      if (currentItemModel.IsDirectory)
      {
        directoryItemModel = currentItemModel;
        return true;
      }

      return FileSystemItemModel.TryGetDirectoryElementOf(currentItemModel.ParentFileSystemItem, out directoryItemModel);
    }

    public void ApplyActionOnSubTree(Action<IFileSystemItemModel> action, Predicate<IFileSystemItemModel> predicate)
    {
      if (!predicate(this))
      {
        return;
      }

      action(this);

      this.ChildFileSystemItems
        .ToList()
        .ForEach(childElement => childElement?.ApplyActionOnSubTree(action, predicate));

      //this.ChildFileSystemItems = new ObservableCollection<FileSystemItemModel>(this.ChildFileSystemItems.ToList());
    }

    /// <summary>
    /// Sorts alphabetically ignoring the file exetension.
    /// System directories have precedence over directories and directories have precedence over files.
    /// </summary>
    /// <param name="itemModelA">First comparand</param>
    /// <param name="itemModelB">Second Comparand</param>
    /// <returns>'-1' when <paramref name="itemModelA"/> is before <paramref name="itemModelB"/>,  '0' for euality and '1' when  <paramref name="itemModelA"/> is after <paramref name="itemModelB"/></returns>
    private int FileSystemTreeSortComparison(IFileSystemItemModel itemModelA, IFileSystemItemModel itemModelB)
    {
      if (itemModelA.IsSystem && itemModelA.IsDirectory && !itemModelB.IsSystem && !itemModelB.IsDirectory)
      {
        return -1;
      }

      if (!itemModelA.IsSystem && !itemModelA.IsDirectory && itemModelB.IsSystem && itemModelB.IsDirectory)
      {
        return 1;
      }

      if (itemModelA.IsDirectory && !itemModelB.IsDirectory)
      {
        return -1;
      }

      if (!itemModelA.IsDirectory && itemModelB.IsDirectory)
      {
        return 1;
      }

      string nameItemA = !itemModelA.IsDirectory ? Path.GetFileNameWithoutExtension(itemModelA.Info.Name) : itemModelA.Info.Name;
      string nameItemB = !itemModelB.IsDirectory ? Path.GetFileNameWithoutExtension(itemModelB.Info.Name) : itemModelB.Info.Name;

      return nameItemA.CompareTo(nameItemB);

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
        return String.Compare(treeElementA, treeElementB, StringComparison.OrdinalIgnoreCase);
      }

      return char.IsWhiteSpace(punctuationB) ? -1 : 1;
    }

    private FileSystemInfo info;
    public FileSystemInfo Info
    {
      get => this.info;
      set
      {
        this.info = value; 
        this.IsDirectory = this.Info is DirectoryInfo;
      }
    }

    public IFileSystemItemModel ParentFileSystemItem { get; set; }

    public ObservableCollection<IFileSystemItemModel> ChildFileSystemItems { get; set; }

    public bool IsHidden { get; set; }

    public bool IsSystem { get; set; }

    public Guid Id { get; set; }

    public bool IsArchive { get; set; }

    public bool IsDirectory { get; set; }
    public bool IsDrive { get; set; }

    public string DisplayName { get; set; }


    public int CompareTo(IFileSystemItemModel other)
    {
      if (object.ReferenceEquals(this, other))
      {
        return 0;
      }

      if (object.ReferenceEquals(null, other))
      {
        return 1;
      }

      return FileSystemTreeSortComparison(this, other);
    }

    #region Implementation of IComparer<in FileSystemItemModel>

    public int Compare(IFileSystemItemModel x, IFileSystemItemModel y) => FileSystemTreeSortComparison(x, y);

    #endregion

    #region Implementation of IComparer

    public int Compare(object x, object y) => Compare(x as IFileSystemItemModel, y as IFileSystemItemModel);

    #endregion
  }
}
