#region Info

// //  
// BionicFileExplorer

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Bionic.FileExplorer
{
  public interface IFileSystemItemModel : IComparable<IFileSystemItemModel>, IComparer<IFileSystemItemModel>, IComparer
  {
    void ApplyActionOnSubTree(Action<IFileSystemItemModel> action, Predicate<IFileSystemItemModel> predicate);
    void SortChildrenAsync();
    IFileSystemItemModel CreateModel();
    FileSystemInfo Info { get; set; }
    ObservableCollection<IFileSystemItemModel> ChildFileSystemItems { get; set; }
    IFileSystemItemModel ParentFileSystemItem { get; set; }
    bool IsHidden { get; set; }
    bool IsSystem { get; set; }
    Guid Id { get; set; }
    bool IsArchive { get; set; }
    bool IsDirectory { get; set; }
    bool IsDrive { get; set; }
    string DisplayName { get; set; }
  }
}