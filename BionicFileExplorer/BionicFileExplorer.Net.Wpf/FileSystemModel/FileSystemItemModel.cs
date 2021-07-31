using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace BionicFileExplorer.Net.Wpf.FileSystemModel
{

  public class FileSystemItemModel : IFileSystemItemModel
  {
    public FileSystemItemModel(FileSystemInfo info, IDirectory parentDirectoryItem) : this(info, parentDirectoryItem, false)
    {
    }

    public FileSystemItemModel(FileSystemInfo info, IDirectory parentDirectoryItem, bool isDrive)
    {
      Info = info;
      ParentFileSystemItem = parentDirectoryItem;
      IsDrive = isDrive;
      Id = Guid.NewGuid();
      InitializeDisplayName();
    }

    private void InitializeDisplayName()
    {
      DisplayName = Info == null
        ? string.Empty
        : !IsDirectory && !IsDrive
          ? IsHidingExtension
            ? Path.GetFileNameWithoutExtension(Info.Name)
            : Info.Name
          : Info.Name;
    }

    public override string ToString() => Info?.FullName ?? DisplayName;

    public void RefreshInfo()
    {
      if (Info == null)
      {
        return;
      }

      Info = IsDirectory
        ? new DirectoryInfo(this.Info.FullName)
        : new FileInfo(this.Info.FullName);
      InitializeDisplayName();
    }

    public void RefreshInfo(string path)
    {
      if (Info == null)
      {
        return;
      }

      if (IsDirectory)
      {
        if (!System.IO.Directory.Exists(path))
        {
          //throw new DirectoryNotFoundException("The directory path is not found or the path is not a directory.");
          return;
        }
        Info = new DirectoryInfo(path);
      }
      else
      {
        if (!System.IO.File.Exists(path))
        {
          throw new FileNotFoundException("The file path is not found or the path is not a file.");
        }

        Info = new FileInfo(path);
      }
      InitializeDisplayName();
    }

    public IFileSystemItemModel CreateModel(FileSystemInfo info, IDirectory parent) => new FileSystemItemModel(info, parent);

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected virtual void OnClassChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

    public event PropertyChangedEventHandler PropertyChanged;

    public IDirectory ParentFileSystemItem
    {
      get => parentFileSystemItem;
      set
      {
        parentFileSystemItem = value;
        OnPropertyChanged();
      }
    }

    public bool IsHidden => Info?.Attributes.HasFlag(FileAttributes.Hidden) ?? false;
    public bool IsSystem => Info?.Attributes.HasFlag(FileAttributes.System) ?? false;
    public Guid Id { get; }
    public bool IsArchive => Info?.Attributes.HasFlag(FileAttributes.Archive) ?? false;
    public bool IsDirectory { get; init; }
    public bool IsDrive { get; protected init; }

    public string DisplayName
    {
      get => displayName;
      set
      {
        displayName = value;
        OnPropertyChanged();
      }
    }

    public FileSystemInfo Info
    {
      get => info;
      set
      {
        info = value;
        OnPropertyChanged();
      }
    }

    public bool IsHidingExtension
    {
      get => isHidingExtension;
      set
      {
        isHidingExtension = value;
        OnPropertyChanged();
        InitializeDisplayName();
      }
    }

    public bool IsSpecial { get; init; }

    public bool IsNodeVisited
    {
      get => isNodeVisited;
      set
      {
        isNodeVisited = value;
        OnPropertyChanged();
      }
    }

    private FileSystemInfo info;
    private IDirectory parentFileSystemItem;
    private DirectoryInfo parentInfo;
    private string displayName;
    private bool isHidingExtension;
    private bool isNodeVisited;
  }
}
