using BionicCode.Utilities.Net.Wpf;
using BionicCode.Utilities.Net.Wpf.Extensions;
using BionicFileExplorer.Net.Wpf.FileSystemModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;

namespace BionicFileExplorer.Net.Wpf
{
  public enum ViewMode
  {
    Default = 0,
    Details,
    Gallery,
    List
  }

  public class FileSystemListView : ListView
  {

    public ICommand ShowTilesViewCommand => new AsyncRelayCommand(ExecuteShowTilesViewCommand);
    public ICommand ShowListViewCommand => new AsyncRelayCommand(ExecuteShowListViewCommand);

    private void ExecuteShowListViewCommand(object obj)
    {
      var itemsPanelXaml = @"
      <ItemsPanelTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
        <VirtualizingStackPanel />
      </ItemsPanelTemplate>";
      var itemsPanelTemplate = XamlReader.Parse(itemsPanelXaml) as ItemsPanelTemplate;
      this.ItemsPanel = itemsPanelTemplate;
      SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Visible);
    }

    private void ExecuteShowTilesViewCommand(object obj)
    {
      var itemsPanelXaml = @"
      <ItemsPanelTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
        <WrapPanel />
      </ItemsPanelTemplate>";
      var itemsPanelTemplate = XamlReader.Parse(itemsPanelXaml) as ItemsPanelTemplate;
      this.ItemsPanel = itemsPanelTemplate;
      SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
    }

    public static readonly RoutedEvent DirectoryExpandedEvent = EventManager.RegisterRoutedEvent(
    "DirectoryExpanded", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FileSystemListView));

    public event RoutedEventHandler DirectoryExpanded
    {
      add => AddHandler(DirectoryExpandedEvent, value); 
      remove => RemoveHandler(DirectoryExpandedEvent, value); 
    }



    public ViewMode SelectedViewMode
    {
      get { return (ViewMode)GetValue(SelectedViewModeProperty); }
      set { SetValue(SelectedViewModeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for SelectedViewMode.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedViewModeProperty =
        DependencyProperty.Register("SelectedViewMode", typeof(ViewMode), typeof(FileSystemListView), new PropertyMetadata(ViewMode.Details));


    public IFileSystemIconSelector FileSystemIconSelector
    {
      get => (IFileSystemIconSelector)GetValue(FileSystemListView.FileSystemIconSelectorProperty);
      set => SetValue(FileSystemListView.FileSystemIconSelectorProperty, value);
    }

    public static readonly DependencyProperty FileSystemIconSelectorProperty =
        DependencyProperty.Register("FileSystemIconSelector", typeof(IFileSystemIconSelector), typeof(FileSystemListView), new PropertyMetadata(default(IFileSystemIconSelector), FileSystemListView.OnFileSystemIconSelectorChanged));

    static FileSystemListView()
    {
      FileSystemListView.FileSystemIconDefaultProvider = new FileSystemIconSelector();
      FileSystemListView.VisibilityProperty.OverrideMetadata(typeof(FileSystemListView), new FrameworkPropertyMetadata(default(Visibility), OnVisibilityChanged));
    }

    private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var this_ = d as FileSystemListView;
      if (this_.Visibility == Visibility.Collapsed)
      {
        return;
      }

      if (this_.ParentFileExplorer.SelectedItem is not IDirectory directory)
      {
        directory = this_.ParentFileExplorer.SelectedItem.ParentFileSystemItem;
        if (directory == null)
        {
          return;
        }
      }

      this_.ShowFolderContent(directory);
    }

    public FileSystemListView()
    {
      AddHandler(FileSystemListBoxItem.PreviewMouseDoubleClickEvent, new MouseButtonEventHandler(OnFileSystemItemDoubleClicked));
      this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      if (this.TryFindVisualParentElement(out BionicFileExplorer fileExplorer))
      {
        this.ParentFileExplorer = fileExplorer;
        //fileExplorer.SelectedFileSystemItemChanged += OnFileExplorerSelectedFileSystemItemChanged;
      }
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
      base.OnSelectionChanged(e);
    }

    private void OnFileSystemItemDoubleClicked(object sender, MouseButtonEventArgs e)
    {
      var elementDataContext = (e.OriginalSource as FrameworkElement)?.DataContext;

      if (elementDataContext is IDirectory directory)
      {
        ShowFolderContent(directory);
      }
      else if (elementDataContext is IFile file && System.IO.File.Exists(file.Info.FullName))
      {
        OpenFileUsingSystemDefaultApplication(file);
      }

      RaiseEvent(new RoutedEventArgs(FileSystemListView.DirectoryExpandedEvent, this));
    }

    private void ShowFolderContent(IDirectory directory)
    {
      if (directory is SpecialDirectory && this.LastParentFolder != null)
      {
        directory = this.LastParentFolder.ParentFileSystemItem;
      }
      this.LastParentFolder = directory;
      var newItemsSource = new ObservableCollection<IFileSystemItemModel>(directory.ChildFileSystemItems);
      (CollectionViewSource.GetDefaultView(newItemsSource) as ListCollectionView).CustomSort = (IComparer)directory;
      this.ItemsSource = newItemsSource;

      if (this.LastParentFolder.ParentFileSystemItem != null)
      {
        var returnToParentDirectoryDummy = SpecialDirectory.Create(directory);
        returnToParentDirectoryDummy.DisplayName = ". .";
        newItemsSource.Insert(0, returnToParentDirectoryDummy);
      }
    }

    private static void OpenFileUsingSystemDefaultApplication(IFile file)
    {
      var processInfo = new ProcessStartInfo(file.Info.FullName)
      {
        UseShellExecute = true
      };
      using var process = Process.Start(processInfo);
    }

    internal static IFileSystemIconSelector GetFileSystemIconProvider() => FileSystemListView.FileSystemIconProvider ?? FileSystemListView.FileSystemIconDefaultProvider;

    private static void OnFileSystemIconSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      FileSystemListView.FileSystemIconProvider = e.NewValue as IFileSystemIconSelector;
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
      base.PrepareContainerForItemOverride(element, item);
      var itemContainer = element as FileSystemListBoxItem;
      var dataModel = item as IFileSystemItemModel;
      var itemIconProvider = FileSystemListView.GetFileSystemIconProvider();
      itemContainer.IconSource = itemIconProvider.SelectIconSource(dataModel, dataModel.IsDirectory, dataModel.Info?.FullName ?? string.Empty);
    }

    protected override DependencyObject GetContainerForItemOverride() => new FileSystemListBoxItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is FileSystemListBoxItem;

    private static IFileSystemIconSelector FileSystemIconDefaultProvider { get; }
    private static IFileSystemIconSelector FileSystemIconProvider { get; set; }
    private BionicFileExplorer ParentFileExplorer { get; set; }
    public IDirectory LastParentFolder { get; private set; }
  }
}
