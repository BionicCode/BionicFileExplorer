using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace BionicFileExplorer.Net
{

  public class FileSystemItemModel : IFileSystemItemModel
  {
    public FileSystemItemModel(FileSystemInfo info, IDirectory parentDirectoryItem) : this(info, parentDirectoryItem, false)
    {
    }

    public FileSystemItemModel(FileSystemInfo info, IDirectory parentDirectoryItem, bool isDrive)
    {
      this.Info = info;
      this.ParentFileSystemItem = parentDirectoryItem;
      this.IsDrive = isDrive;
      this.Id = Guid.NewGuid();
      InitializeDisplayName();
    }

    private void InitializeDisplayName()
    {
      this.DisplayName = this.Info == null 
        ? string.Empty 
        : !this.IsDirectory && !this.IsDrive
          ? this.IsHidingExtension 
            ? Path.GetFileNameWithoutExtension(this.Info.Name) 
            : this.Info.Name
          : this.Info.Name;
    }

    public override string ToString() => this.Info?.FullName ?? this.DisplayName;

    public void RefreshInfo()
    {
      if (this.Info == null)
      {
        return;
      }

      this.Info = this.IsDirectory
        ? new DirectoryInfo(this.Info.FullName)
        : new FileInfo(this.Info.FullName);
      InitializeDisplayName();
    }

    public void RefreshInfo(string path)
    {
      if (this.Info == null)
      {
        return;
      }

      if (this.IsDirectory)
      {
        if (!System.IO.Directory.Exists(path))
        {
          //throw new DirectoryNotFoundException("The directory path is not found or the path is not a directory.");
          return;
        }
        this.Info = new DirectoryInfo(path);
      }
      else
      {
        if (!System.IO.File.Exists(path))
        {
          throw new FileNotFoundException("The file path is not found or the path is not a file.");
        }

        this.Info = new FileInfo(path);
      }
      InitializeDisplayName();
    }

    public IFileSystemItemModel CreateModel(FileSystemInfo info, IDirectory parent) => new FileSystemItemModel(info, parent);

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected virtual void OnClassChanged() => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

    public event PropertyChangedEventHandler PropertyChanged;

    public IDirectory ParentFileSystemItem
    {
      get => this.parentFileSystemItem;
      set
      {
        this.parentFileSystemItem = value;
        OnPropertyChanged();
      }
    }

    public bool IsHidden => this.Info?.Attributes.HasFlag(FileAttributes.Hidden) ?? false;
    public bool IsSystem => this.Info?.Attributes.HasFlag(FileAttributes.System) ?? false;
    public Guid Id { get; }
    public bool IsArchive => this.Info?.Attributes.HasFlag(FileAttributes.Archive) ?? false;
    public bool IsDirectory { get; init; }
    public bool IsDrive { get; protected init; }

    public string DisplayName
    {
      get => this.displayName;
      set
      {
        this.displayName = value;
        OnPropertyChanged();
      }
    }

    public FileSystemInfo Info
    {
      get => this.info;
      set
      {
        this.info = value;
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
