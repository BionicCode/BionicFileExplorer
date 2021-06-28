using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Data;

namespace BionicFileExplorer.Net
{
  public class Drive : Directory, IDrive
  {
    public Drive(DriveInfo driveInfo, IDirectory parent) : this(driveInfo.RootDirectory, parent)
    {
      this.IsReady = driveInfo.IsReady;
    }

    public Drive(DirectoryInfo rootDirectoryInfo) : this(rootDirectoryInfo, null)
    {
    }

    public Drive(DirectoryInfo rootDirectoryInfo, IDirectory parent) : base(rootDirectoryInfo, parent)
    {
      this.IsDrive = true;
      this.IsReady = true;
    }

    private bool isReady;
    public bool IsReady
    {
      get => this.isReady;
      private set
      {
        this.isReady = value;
        OnPropertyChanged();
      }
    }
  }
}
