using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Data;

namespace BionicFileExplorer.Net.Wpf.FileSystemModel
{
  public class Drive : Directory, IDrive
  {
    public Drive(DriveInfo driveInfo, IDirectory parent) : this(driveInfo.RootDirectory, parent)
    {
      IsReady = driveInfo.IsReady;
    }

    public Drive(DirectoryInfo rootDirectoryInfo) : this(rootDirectoryInfo, null)
    {
    }

    public Drive(DirectoryInfo rootDirectoryInfo, IDirectory parent) : base(rootDirectoryInfo, parent)
    {
      IsDrive = true;
      IsReady = true;
    }

    private bool isReady;
    public bool IsReady
    {
      get => isReady;
      private set
      {
        isReady = value;
        OnPropertyChanged();
      }
    }
  }
}
