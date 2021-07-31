using BionicCode.Utilities.Net.Wpf.Extensions;
using BionicFileExplorer.Net.Wpf.FileSystemModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BionicFileExplorer.Net.Wpf
{

  public class FileSystemTreeView : TreeView
  {
    public IFileSystemIconSelector FileSystemIconSelector
    {
      get { return (IFileSystemIconSelector)GetValue(FileSystemTreeView.FileSystemIconSelectorProperty); }
      set => SetValue(FileSystemTreeView.FileSystemIconSelectorProperty, value);
    }

    public static readonly DependencyProperty FileSystemIconSelectorProperty =
        DependencyProperty.Register("FileSystemIconSelector", typeof(IFileSystemIconSelector), typeof(FileSystemTreeView), new PropertyMetadata(default(IFileSystemIconSelector), FileSystemTreeView.OnFileSystemIconSelectorChanged));

    static FileSystemTreeView()
    {
      FileSystemTreeView.FileSystemIconDefaultProvider = new FileSystemIconSelector();
      FileSystemTreeView.VisibilityProperty.OverrideMetadata(typeof(FileSystemTreeView), new FrameworkPropertyMetadata(default(Visibility), OnVisibilityChanged));
    }

    public FileSystemTreeView()
    {
      AddHandler(FileSystemTreeViewItem.PreviewMouseDoubleClickEvent, new MouseButtonEventHandler(OnFileSystemItemDoubleClicked));
      this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      if (this.TryFindVisualParentElement(out BionicFileExplorer fileExplorer))
      {
        this.ParentFileExplorer = fileExplorer;
      }
    }

    private void OnFileExplorerSelectedFileSystemItemChanged(object sender, RoutedEventArgs e)
    {
      var fileExplorer = sender as BionicFileExplorer;

    }

    private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var this_ = d as FileSystemTreeView;
      if (this_.Visibility == Visibility.Collapsed || this_.ParentFileExplorer.SelectedItem == null)
      {
        return;
      }

      var pathSegments = new Queue<string>(this_.ParentFileExplorer.SelectedItem.Info.FullName.Split(Path.DirectorySeparatorChar, System.StringSplitOptions.TrimEntries));
      if (this_.ParentFileExplorer.FileSystem.Index.TryGetValue(pathSegments.Dequeue(), out IFileSystemItemModel rootItem))
      {
        rootItem.IsNodeVisited = true;
        this_.ExpandToFolder(rootItem, pathSegments);
      }
    }

    private void ExpandToFolder(IFileSystemItemModel parentItem, Queue<string> pathSegments)
    {
      if (!pathSegments.TryDequeue(out string childPathSegment))
      {
        return;
      }

      if (this.ParentFileExplorer.FileSystem.Index.TryGetValue(Path.Combine(parentItem.Info.FullName, childPathSegment), out IFileSystemItemModel childItem))
      {
        childItem.IsNodeVisited = true;
        ExpandToFolder(childItem, pathSegments);
      }
    }

    internal static IFileSystemIconSelector GetFileSystemIconProvider() => FileSystemTreeView.FileSystemIconProvider ?? FileSystemTreeView.FileSystemIconDefaultProvider;

    private static void OnFileSystemIconSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      FileSystemTreeView.FileSystemIconProvider = e.NewValue as IFileSystemIconSelector;
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
      base.PrepareContainerForItemOverride(element, item);
      var itemContainer = element as FileSystemTreeViewItem;
      var dataModel = item as IFileSystemItemModel;
      var itemIconProvider = FileSystemTreeView.GetFileSystemIconProvider();
      itemContainer.IconSource = itemIconProvider.SelectIconSource(dataModel, dataModel.IsDirectory, dataModel.Info.FullName);
    }

    private void OnFileSystemItemDoubleClicked(object sender, MouseButtonEventArgs e)
    {
      var elementDataContext = (e.OriginalSource as FrameworkElement)?.DataContext;

      if (elementDataContext is IFile file && System.IO.File.Exists(file.Info.FullName))
      {
        OpenFileUsingSystemDefaultApplication(file);
        RaiseEvent(new RoutedEventArgs(FileSystemListView.DirectoryExpandedEvent, this));
      }
    }

    protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
    {
      base.OnSelectedItemChanged(e);
    }

    private static void OpenFileUsingSystemDefaultApplication(IFile file)
    {
      var processInfo = new ProcessStartInfo(file.Info.FullName)
      {
        UseShellExecute = true,
        Verb = "Open"
      };
      using var process = Process.Start(processInfo);
    }

    protected override DependencyObject GetContainerForItemOverride() => new FileSystemTreeViewItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is FileSystemTreeViewItem;

    private static IFileSystemIconSelector FileSystemIconDefaultProvider { get; }
    private static IFileSystemIconSelector FileSystemIconProvider { get; set; }
    private BionicFileExplorer ParentFileExplorer { get; set; }
  }
}
