using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace BionicFileExplorer.Net
{
  public interface IDirectory : IFileSystemItemModel, IComparable<IFileSystemItemModel>, IComparer<IFileSystemItemModel>, IComparer
  {
    IEnumerable<IDirectory> GetSubdirectories();
    IEnumerable<IFile> GetFiles();
    void ApplyActionOnSubTree(Action<IFileSystemItemModel> action, Predicate<IFileSystemItemModel> predicate);
    void Sort();
    void Sort(Comparison<IFileSystemItemModel> comparison);
    bool IsRealized { get; }
    bool IsNavigationalDirectory { get; init; }
    new DirectoryInfo Info { get; set; }
    FileSystemInfo IFileSystemItemModel.Info => this.Info;

    ObservableCollection<IFileSystemItemModel> ChildFileSystemItems { get; set; }
  }
}