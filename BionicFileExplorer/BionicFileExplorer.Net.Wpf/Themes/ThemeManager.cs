using BionicCode.Utilities.Net.Core.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using System.Reflection;

namespace BionicFileExplorer.Net.Wpf.Themes
{
  public class ThemeManager
  {
    //public ICommand LoadDarkThemeCommand => new AsyncRelayCommand(ExecuteLoadDarkTheme);
    private static HashSet<string> FilePaths { get; }
    private static string ThemeFolder { get; set; }
    private static FileSystemWatcher ThemeFolderWatcher { get; }

    static ThemeManager()
    {
      ThemeManager.FilePaths = new HashSet<string>();
      ThemeManager.ThemeFolderWatcher = new FileSystemWatcher()
      {
        IncludeSubdirectories = true,
        Filter = "*.dll",
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.LastAccess
      };
      ThemeManager.ThemeFolderWatcher.Changed += OnContentChanged;
      ThemeManager.ThemeFolderWatcher.Created += OnContentCreated;
      ThemeManager.ThemeFolderWatcher.Renamed += OnContentRenamed;
      ThemeManager.ThemeFolderWatcher.Deleted += OnContentDeleted;
    }

    public void SetThemeFolder(string folderPath)
    {
      if (!System.IO.Directory.Exists(folderPath))
      {
        throw new DirectoryNotFoundException($"Directory not found: {folderPath}");
      }
      ThemeManager.ThemeFolder = folderPath;
      ThemeManager.ThemeFolderWatcher.Path = folderPath;
    }

    public bool TryLoadTheme(string themeFilePath)
    {
      if (!ThemeManager.FilePaths.Contains(themeFilePath))
      {
        ThemeManager.FilePaths.Add(themeFilePath);
      }

      var themeFiles = new List<string>();

      switch (themeFilePath)
      {
        case string filePath when System.IO.File.Exists(filePath): themeFiles.Add(filePath); break;
        case string directoryPath when System.IO.Directory.Exists(directoryPath): 
          themeFiles.AddRange(System.IO.Directory.EnumerateFiles(directoryPath, "*.*", new EnumerationOptions() { RecurseSubdirectories = true, IgnoreInaccessible = true })
            .Where(path => Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".xaml", StringComparison.OrdinalIgnoreCase))); break;
        default:
          throw new System.IO.FileNotFoundException($"Path not found: {themeFilePath}. Path must point to a .dll or .xaml file or to a director containing such files.");
      }

      return true;
    }

    private static void OnContentChanged(object sender, FileSystemEventArgs e)
    {
      throw new NotImplementedException();
    }

    private static void OnContentCreated(object sender, FileSystemEventArgs e)
    {
      ThemeManager.FilePaths.Add(e.FullPath);
    }

    private static void OnContentRenamed(object sender, RenamedEventArgs e)
    {
      throw new NotImplementedException();
    }

    private static void OnContentDeleted(object sender, FileSystemEventArgs e)
    {
      ThemeManager.FilePaths.Remove(e.FullPath);
    }

    private void ExecuteLoadDarkTheme(IEnumerable<string> resourcePaths)
    {
      foreach (string filePath in resourcePaths)
      {
        if (Path.GetExtension(filePath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
          IEnumerable<string> filePaths = GetFilesFromAssembly(filePath);
        }
      }
    }

    private IEnumerable<string> GetFilesFromAssembly(string filePath)
    {
      return new List<string>();
      //var xamlFilenames = Assembly.LoadFrom(filePath).GetManifestResourceNames().Where(resourceName => Path.GetExtension(resourceName).Equals(".xaml";
    }
  }
}
