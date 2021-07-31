using BionicFileExplorer.Net.Wpf.FileSystemModel;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace BionicFileExplorer.Net.Wpf
{
  public class FileSystemIconSelector : IFileSystemIconSelector
  {
    /// <summary>Maximal Length of unmanaged Windows-Path-strings</summary>
    private const int MAX_PATH = 260;
    /// <summary>Maximal Length of unmanaged Typename</summary>
    private const int MAX_TYPE = 80;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
      public SHFILEINFO(bool b)
      {
        hIcon = IntPtr.Zero;
        iIcon = 0;
        dwAttributes = 0;
        szDisplayName = "";
        szTypeName = "";
      }
      public IntPtr hIcon;
      public int iIcon;
      public uint dwAttributes;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
      public string szDisplayName;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_TYPE)]
      public string szTypeName;
    };

    [Flags]
    enum SHGFI : int
    {
      /// <summary>get icon</summary>
      Icon = 0x000000100,
      /// <summary>get display name</summary>
      DisplayName = 0x000000200,
      /// <summary>get type name</summary>
      TypeName = 0x000000400,
      /// <summary>get attributes</summary>
      Attributes = 0x000000800,
      /// <summary>get icon location</summary>
      IconLocation = 0x000001000,
      /// <summary>return exe type</summary>
      ExeType = 0x000002000,
      /// <summary>get system icon index</summary>
      SysIconIndex = 0x000004000,
      /// <summary>put a link overlay on icon</summary>
      LinkOverlay = 0x000008000,
      /// <summary>show icon in selected state</summary>
      Selected = 0x000010000,
      /// <summary>get only specified attributes</summary>
      Attr_Specified = 0x000020000,
      /// <summary>get large icon</summary>
      LargeIcon = 0x000000000,
      /// <summary>get small icon</summary>
      SmallIcon = 0x000000001,
      /// <summary>get open icon</summary>
      OpenIcon = 0x000000002,
      /// <summary>get shell size icon</summary>
      ShellIconSize = 0x000000004,
      /// <summary>pszPath is a pidl</summary>
      PIDL = 0x000000008,
      /// <summary>use passed dwFileAttribute</summary>
      UseFileAttributes = 0x000000010,
      /// <summary>apply the appropriate overlays</summary>
      AddOverlays = 0x000000020,
      /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
      OverlayIndex = 0x000000040,
    }

    // https://docs.microsoft.com/en-us/windows/win32/fileio/file-attribute-constants#example
    [Flags]
    private enum FileAttributeConstants 
    {
      DEFAULT = 0,

     // The handle that identifies a directory.
      FILE_ATTRIBUTE_DIRECTORY = 16,

      // A file or directory that is an archive file or directory. Applications typically use this attribute to mark files for backup or removal . 
      FILE_ATTRIBUTE_ARCHIVE = 32,

      // A file that does not have other attributes set.This attribute is valid only when used alone.
      FILE_ATTRIBUTE_NORMAL = 128, 

      // A file or directory that is compressed.For a file, all of the data in the file is compressed.For a directory, compression is the default for newly created files and subdirectories.
      FILE_ATTRIBUTE_COMPRESSED = 2048
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, SHGFI uFlags); 
    
    private static ImageSource GetFileSystemItemIcon(string filePath, bool isSmall, FileAttributeConstants iconType)
    {
      SHFILEINFO info = new SHFILEINFO(true);
      int cbFileInfo = Marshal.SizeOf(info);
      SHGFI flags;
      if (isSmall)
        flags = SHGFI.Icon | SHGFI.SmallIcon | SHGFI.UseFileAttributes;
      else
        flags = SHGFI.Icon | SHGFI.LargeIcon | SHGFI.UseFileAttributes;

      SHGetFileInfo(filePath, (uint) iconType, ref info, (uint)cbFileInfo, flags);

      using var defaultAssociatedIconWinFormsIcon = Icon.FromHandle(info.hIcon);
      return ConvertIconToImageSource(defaultAssociatedIconWinFormsIcon);
    }

    private static ImageSource ConvertIconToImageSource(Icon defaultAssociatedIconWinFormsIcon) => System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                defaultAssociatedIconWinFormsIcon.Handle,
                System.Windows.Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

    public virtual ImageSource SelectIconSource(IFileSystemItemModel itemModel, bool isDirectory, string fileSystemItemFullName)
    {
      FileAttributeConstants iconType = System.IO.File.Exists(fileSystemItemFullName) ? FileAttributeConstants.FILE_ATTRIBUTE_NORMAL : System.IO.Directory.Exists(fileSystemItemFullName) ? FileAttributeConstants.FILE_ATTRIBUTE_DIRECTORY : FileAttributeConstants.DEFAULT;

      if (iconType == FileAttributeConstants.DEFAULT || itemModel is SpecialDirectory)
      {
        return null;
      }

      return GetFileSystemItemIcon(fileSystemItemFullName, true, iconType);
    }
  }
}
