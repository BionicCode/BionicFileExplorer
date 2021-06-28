#region Info

// //  
// BionicFileExplorer

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace BionicFileExplorer.Net
{
  public interface IFileSystemItemModel : INotifyPropertyChanged
  {
    void RefreshInfo();
    void RefreshInfo(string path);
    IFileSystemItemModel CreateModel(FileSystemInfo info, IDirectory parent);
    FileSystemInfo Info { get; }
    IDirectory ParentFileSystemItem { get; set; }
    bool IsHidden { get; }
    bool IsHidingExtension { get; set; }
    bool IsSystem { get; }
    Guid Id { get; }
    bool IsArchive { get; }
    bool IsDirectory { get; }
    bool IsDrive { get; }
    bool IsSpecial { get; }
    bool IsNodeVisited { get; set; }
    string DisplayName { get; set; }
  }
}