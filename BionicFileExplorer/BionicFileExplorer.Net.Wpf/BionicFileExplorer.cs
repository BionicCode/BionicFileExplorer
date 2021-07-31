using BionicCode.Utilities.Net.Wpf;
using BionicFileExplorer.Net.Wpf.FileSystemModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BionicFileExplorer.Net.Wpf
{

  [TemplatePart(Name = BionicFileExplorer.PartToolBarName, Type = typeof(Control))]
  [TemplatePart(Name = BionicFileExplorer.PartListViewName, Type = typeof(ItemsControl))]
  [TemplatePart(Name = BionicFileExplorer.PartTreeViewName, Type = typeof(ItemsControl))]
  public class BionicFileExplorer : Control
  {
    public const string PartToolBarName = "PART_ToolBar";
    public const string PartListViewName = "PART_ListView";
    public const string PartTreeViewName = "PART_TreeView";

    public static ResourceKey ControlBackgroundColorKey = new ComponentResourceKey(typeof(BionicFileExplorer), "ControlBackgroundColor");
    public static ResourceKey ControlBackgroundBrushKey = new ComponentResourceKey(typeof(BionicFileExplorer), "ControlBackgroundBrush");
    public static ResourceKey ForegroundColorKey = new ComponentResourceKey(typeof(BionicFileExplorer), "ForegroundColor");
    public static ResourceKey ForegroundBrushKey = new ComponentResourceKey(typeof(BionicFileExplorer), "ForegroundBrush");

    public static RoutedUICommand ShowListViewRoutedCommand => new RoutedUICommand("Show file system as list", nameof(BionicFileExplorer.ShowListViewRoutedCommand), typeof(BionicFileExplorer));

    public static RoutedUICommand ShowTilesViewRoutedCommand => new RoutedUICommand("Show file system as tiles", nameof(BionicFileExplorer.ShowTilesViewRoutedCommand), typeof(BionicFileExplorer));

    public static readonly RoutedEvent SelectedFileSystemItemChangedEvent = EventManager.RegisterRoutedEvent(
    "SelectedFileSystemItemChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(BionicFileExplorer));

    public event RoutedEventHandler SelectedFileSystemItemChanged
    {
      add => AddHandler(SelectedFileSystemItemChangedEvent, value);
      remove => RemoveHandler(SelectedFileSystemItemChangedEvent, value);
    }

    #region FileSystemTree read-only dependency property

    public IEnumerable FileSystemTree
    {
      get => (IEnumerable) GetValue(BionicFileExplorer.FileSystemTreeProperty); 
      protected set => SetValue(BionicFileExplorer.FileSystemTreePropertyKey, value); 
    }

    private static readonly DependencyPropertyKey FileSystemTreePropertyKey = DependencyProperty.RegisterReadOnly(
      "FileSystemTree", 
      typeof(IEnumerable), 
      typeof(BionicFileExplorer), 
      new PropertyMetadata(default));


    public static readonly DependencyProperty FileSystemTreeProperty = BionicFileExplorer.FileSystemTreePropertyKey.DependencyProperty;

    #endregion FileSystemTree read-only dependency property

    #region SelectedItem dependency property

    public IFileSystemItemModel SelectedItem
    {
      get { return (IFileSystemItemModel)GetValue(SelectedItemProperty); }
      set { SetValue(SelectedItemProperty, value); }
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register("SelectedItem", typeof(IFileSystemItemModel), typeof(BionicFileExplorer), new FrameworkPropertyMetadata(default(IFileSystemItemModel), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged, CoerceSelectedItemChanged));

    private static object CoerceSelectedItemChanged(DependencyObject d, object baseValue) => baseValue == null 
      ? (d as BionicFileExplorer).SelectedItem 
      : baseValue;

    #endregion SelectedItem dependency property

    #region SelectedFileSystemPath dependency property
    public string SelectedFileSystemPath
    {
      get { return (string)GetValue(SelectedFileSystemPathProperty); }
      set { SetValue(SelectedFileSystemPathProperty, value); }
    }

    public static readonly DependencyProperty SelectedFileSystemPathProperty =
        DependencyProperty.Register("SelectedFileSystemPath", typeof(string), typeof(BionicFileExplorer), new PropertyMetadata(default));
    #endregion

    #region IsAlternativeViewVisible dependency property


    public bool IsAlternativeViewVisible
    {
      get { return (bool)GetValue(IsAlternativeViewVisibleProperty); }
      set { SetValue(IsAlternativeViewVisibleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsAlternativeViewVisible.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAlternativeViewVisibleProperty =
        DependencyProperty.Register("IsAlternativeViewVisible", typeof(bool), typeof(BionicFileExplorer), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsAlternativeViewChanged));

    private static void OnIsAlternativeViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var this_ = d as BionicFileExplorer;
      this_.OnIsAlternativeViewChanged((bool)e.OldValue, (bool)e.NewValue);
    }


    #endregion

    #region IsTreeViewVisible dependency property
    public bool IsTreeViewVisible
    {
      get { return (bool)GetValue(IsTreeViewVisibleProperty); }
      set { SetValue(IsTreeViewVisibleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsTreeViewVisible.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsTreeViewVisibleProperty =
        DependencyProperty.Register("IsTreeViewVisible", typeof(bool), typeof(BionicFileExplorer), new PropertyMetadata(true, OnIsTreeViewVisibleChanged));

    private static void OnIsTreeViewVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      (d as BionicFileExplorer).OnIsTreeViewVisibleChanged((bool) e.OldValue, (bool) e.NewValue);
    }
    #endregion

    #region IsShowingFileExtensions dependency property

    public bool IsShowingFileExtensions
    {
      get { return (bool)GetValue(IsShowingFileExtensionsProperty); }
      set { SetValue(IsShowingFileExtensionsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsShowingFileExtensions.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsShowingFileExtensionsProperty =
        DependencyProperty.Register("IsShowingFileExtensions", typeof(bool), typeof(BionicFileExplorer), new PropertyMetadata(true, OnIsShowingFileExtensionsChanged));

    private static async void OnIsShowingFileExtensionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => await (d as BionicFileExplorer).OnIsShowingFileExtensionsChangedAsync((bool)e.OldValue, (bool)e.NewValue);

    #endregion IsShowingFileExtensions dependency property

    #region IsShowingSystemFiles dependency property
    public bool IsShowingSystemFiles
    {
      get { return (bool)GetValue(IsShowingSystemFilesProperty); }
      set { SetValue(IsShowingSystemFilesProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsShowingSystemFiles.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsShowingSystemFilesProperty =
        DependencyProperty.Register("IsShowingSystemFiles", typeof(bool), typeof(BionicFileExplorer), new PropertyMetadata(false)); 
    #endregion

    #region IsShowingHiddenFiles dependency property
    public bool IsShowingHiddenFiles
    {
      get { return (bool)GetValue(IsShowingHiddenFilesProperty); }
      set { SetValue(IsShowingHiddenFilesProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsShowingHiddenFiles.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsShowingHiddenFilesProperty =
        DependencyProperty.Register("IsShowingHiddenFiles", typeof(bool), typeof(BionicFileExplorer), new PropertyMetadata(false, OnIsShowingHiddenFilesChanged));

    private static void OnIsShowingHiddenFilesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      (d as BionicFileExplorer).OnIsShowingHiddenFilesChanged((bool)e.OldValue, (bool)e.NewValue);
    }
    #endregion

    #region ToolBarTemplate dependency property


    public ControlTemplate ToolBarTemplate
    {
      get { return (ControlTemplate)GetValue(ToolBarTemplateProperty); }
      set { SetValue(ToolBarTemplateProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ToolBarTemplate.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ToolBarTemplateProperty =
        DependencyProperty.Register("ToolBarTemplate", typeof(ControlTemplate), typeof(BionicFileExplorer), new PropertyMetadata(default, OnToolBarTemplateChanged));

    private static void OnToolBarTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      (d as BionicFileExplorer).OnToolBarTemplateChanged(e.OldValue as ControlTemplate, e.NewValue as ControlTemplate);
    }


    #endregion ToolBarTemplate dependency property

    static BionicFileExplorer()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(BionicFileExplorer), new FrameworkPropertyMetadata(typeof(BionicFileExplorer)));
    }

    public BionicFileExplorer()
    {
      this.FileSystem = new FileSystem();
      this.FileSystemTree = this.FileSystem.FileSystemRoot;
      this.FileSystem.FileSystemMount.Sort();

      AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(OnFileSystemNodeExpanded));
      AddHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(OnFileSystemNodeSelected));
      AddHandler(FileSystemListView.DirectoryExpandedEvent, new RoutedEventHandler(OnListViewDirectoryExpanded));

      this.CommandBindings.Add(new CommandBinding(BionicFileExplorer.ShowListViewRoutedCommand, ExecuteShowListViewRoutedCommand));
      this.CommandBindings.Add(new CommandBinding(BionicFileExplorer.ShowTilesViewRoutedCommand, ExecuteShowTilesViewRoutedCommand));

      this.Loaded += OnLoaded;
    }

    private void ExecuteShowListViewRoutedCommand(object sender, ExecutedRoutedEventArgs e)
    {
      if (this.ListView == null)
      {
        throw new TemplatePartNotFoundException($"Missing template part '{BionicFileExplorer.PartListViewName}'. To show a file system list view using the {nameof(BionicFileExplorer.ShowListViewRoutedCommand)}, there must be an element named '{BionicFileExplorer.PartListViewName}' defined in the ControlTemplate of {nameof(BionicFileExplorer)}.");
      }

      switch (this.ListView)
      {
        case FileSystemListView fileSystemListView: fileSystemListView.ShowListViewCommand.Execute(e.Parameter); break;
        case UIElement uIElement: uIElement.Visibility = Visibility.Visible; break;
        default:
          break;
      }
    }

    private void ExecuteShowTilesViewRoutedCommand(object sender, ExecutedRoutedEventArgs e)
    {
      if (this.ListView == null)
      {
        throw new TemplatePartNotFoundException($"Missing template part '{BionicFileExplorer.PartListViewName}'. To show a file system list view using the {nameof(BionicFileExplorer.ShowListViewRoutedCommand)}, there must be an element named '{BionicFileExplorer.PartListViewName}' defined in the ControlTemplate of {nameof(BionicFileExplorer)}.");
      }

      switch (this.ListView)
      {
        case FileSystemListView fileSystemListView: fileSystemListView.ShowTilesViewCommand.Execute(e.Parameter); break;
        case UIElement uIElement: uIElement.Visibility = Visibility.Visible; break;
        default:
          break;
      }
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
      {
        return;
      }
      (d as BionicFileExplorer).OnSelectedItemChanged(e.OldValue as IFileSystemItemModel, e.NewValue as IFileSystemItemModel);
    }

    private void OnListViewDirectoryExpanded(object sender, RoutedEventArgs e) => this.SelectedFileSystemPath = (e.OriginalSource as FileSystemListView)?.LastParentFolder?.Info?.FullName ?? string.Empty;

    protected virtual void OnSelectedItemChanged(IFileSystemItemModel oldValue, IFileSystemItemModel newValue)
    {
      RaiseEvent(new RoutedEventArgs(BionicFileExplorer.SelectedFileSystemItemChangedEvent, this));
    }

    protected override async void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);
      await this.FileSystem.InitializeFileSystemTreeAsync(4);
    }

    private  void OnLoaded(object sender, RoutedEventArgs e)
    {
    }

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();
      this.ToolBar = GetTemplateChild("PART_ToolBar") as Control;
      this.ListView = GetTemplateChild("PART_ListView") as ItemsControl;
      this.TreeView = GetTemplateChild("PART_TreeView") as ItemsControl;

      ;
    }

    private void OnFileSystemNodeSelected(object sender, RoutedEventArgs e)
    {
      var itemContainer = e.OriginalSource as FileSystemTreeViewItem;
      this.SelectedItem = itemContainer.DataContext as IFileSystemItemModel;
      if (itemContainer.IsExpanded)
      {
        SetSelectedFilePath(this.SelectedItem);
      }
    }

    private void SetSelectedFilePath(IFileSystemItemModel selectedItem)
    {
      if (selectedItem is IFile fileItem)
      {
        this.SelectedFileSystemPath = fileItem?.ParentFileSystemItem?.Info?.FullName ?? string.Empty;
      }
      else
      { 
        this.SelectedFileSystemPath = selectedItem?.Info?.FullName ?? string.Empty; 
      }
    }

    private async void OnFileSystemNodeExpanded(object sender, RoutedEventArgs e)
    {
      var treeViewItem = e.OriginalSource as TreeViewItem;
      if (treeViewItem.DataContext is IDirectory expandedNode)
      {
        expandedNode.Sort();
        var subdirectories = expandedNode.GetSubdirectories();
        await this.FileSystem.TryRealizeChildrenAsync(subdirectories, 2, true);
      }

      var selectedFileSystemItem = treeViewItem.DataContext as IFileSystemItemModel;
      SetSelectedFilePath(selectedFileSystemItem);
      this.SelectedItem = selectedFileSystemItem;
    }

    protected virtual void OnToolBarTemplateChanged(ControlTemplate oldValue, ControlTemplate newValue)
    {
      if (newValue != null && this.ToolBar != null)
      {
        this.ToolBar.Template = newValue;
      }
    }

    protected virtual async Task OnIsShowingFileExtensionsChangedAsync(bool oldValue, bool newValue)
    {
      switch (newValue)
      {
        case true: await this.FileSystem.ShowFileExtensionsAsync(); break;
        case false: await this.FileSystem.HideFileExtensionsAsync(); break;
      }
    }

    protected virtual void OnIsTreeViewVisibleChanged(bool oldValue, bool newValue)
    {
      if (this.IsAlternativeViewVisible != !newValue)
      {
        this.IsAlternativeViewVisible = !newValue;
      }
    }

    private void OnIsAlternativeViewChanged(bool oldValue, bool newValue)
    {
      RaiseEvent(new RoutedEventArgs(BionicFileExplorer.SelectedFileSystemItemChangedEvent, this)); 
      if (this.IsTreeViewVisible != !newValue)
      {
        this.IsTreeViewVisible = !newValue;
      }
    }

    private void OnIsShowingHiddenFilesChanged(bool oldValue, bool newValue)
    {
      
    }

    public FileSystem FileSystem { get; }
    protected Control ToolBar { get; private set; }
    protected ItemsControl ListView { get; private set; }
    protected ItemsControl TreeView { get; private set; }
  }
}
