using System;

namespace Bionic.FileExplorer
{
  [Flags]
  public enum ExplorerFilters
  {
    None = 0,
    Hidden = 1,
    System = 2,
    HiddenAndSystem = ExplorerFilters.Hidden | ExplorerFilters.System,
    Archive = 4,
    Directory = 8,
    HiddenDirectories = ExplorerFilters.Hidden | ExplorerFilters.Directory,
    SystemDirectories = ExplorerFilters.System| ExplorerFilters.Directory
  }
}
