using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using Bionic.FileExplorer.ViewModel;
using BionicUtilities.Net.Extensions;
using BionicUtilities.NetStandard.Generic;

namespace Bionic.FileExplorer
{
  /// <summary>
  ///   Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
  ///   Step 1a) Using this custom control in a XAML file that exists in the current project.
  ///   Add this XmlNamespace attribute to the root element of the markup file where it is
  ///   to be used:
  ///   xmlns:MyNamespace="clr-namespace:BionicFileExplorer"
  ///   Step 1b) Using this custom control in a XAML file that exists in a different project.
  ///   Add this XmlNamespace attribute to the root element of the markup file where it is
  ///   to be used:
  ///   xmlns:MyNamespace="clr-namespace:BionicFileExplorer;assembly=BionicFileExplorer"
  ///   You will also need to add a project reference from the project where the XAML file lives
  ///   to this project and Rebuild to avoid compilation errors:
  ///   Right click on the target project in the Solution Explorer and
  ///   "Add Reference"->"Projects"->[Select this project]
  ///   Step 2)
  ///   Go ahead and use your control in the XAML file.
  ///   <MyNamespace:BionicFileExplorer />
  /// </summary>
  public class BionicFileExplorer : TreeView
  {
    public static readonly DependencyProperty PreloadLevelDepthProperty = DependencyProperty.Register(
      "PreloadLevelDepth",
      typeof(int),
      typeof(BionicFileExplorer),
      new PropertyMetadata(2, null, BionicFileExplorer.CoercePrefetchDepthOnValueChanged));

    private static object CoercePrefetchDepthOnValueChanged(DependencyObject d, object baseValue) =>
      Math.Max(2, (int) baseValue);

    public int PreloadLevelDepth
    {
      get => (int) GetValue(BionicFileExplorer.PreloadLevelDepthProperty);
      set => SetValue(BionicFileExplorer.PreloadLevelDepthProperty, value);
    }

    public static readonly DependencyProperty FileSystemItemIconSelectorProperty = DependencyProperty.Register(
      "FileSystemItemIconSelector",
      typeof(IFileSystemItemIconSelector),
      typeof(BionicFileExplorer),
      new PropertyMetadata(new FileSystemItemIconSelector(), BionicFileExplorer.OnFileSystemItemIconSelectorChanged));

    private static void OnFileSystemItemIconSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      throw new NotImplementedException();
    }

    public IFileSystemItemIconSelector FileSystemItemIconSelector
    {
      get => (IFileSystemItemIconSelector) GetValue(BionicFileExplorer.FileSystemItemIconSelectorProperty);
      set => SetValue(BionicFileExplorer.FileSystemItemIconSelectorProperty, value);
    }

    protected static readonly DependencyPropertyKey ExplorerFiltersPropertyKey = DependencyProperty.RegisterReadOnly(
      "ExplorerFilterValues",
      typeof(ObservableCollection<ExplorerFilters>),
      typeof(BionicFileExplorer),
      new PropertyMetadata(new ObservableCollection<ExplorerFilters>()));

    public static readonly DependencyProperty ExplorerFilterValuesProperty = BionicFileExplorer.ExplorerFiltersPropertyKey.DependencyProperty;

    public ObservableCollection<ExplorerFilters> ExplorerFilterValues
    {
      get => (ObservableCollection<ExplorerFilters>) GetValue(BionicFileExplorer.ExplorerFilterValuesProperty);
      private set => SetValue(BionicFileExplorer.ExplorerFiltersPropertyKey, value);
    }

    public static readonly DependencyProperty SelectedExplorerFiltersProperty = DependencyProperty.Register(
      "SelectedExplorerFilters",
      typeof(ExplorerFilters),
      typeof(BionicFileExplorer),
      new PropertyMetadata(ExplorerFilters.None, BionicFileExplorer.OnSelectedFiltersChanged));

    public ExplorerFilters SelectedExplorerFilters { get => (ExplorerFilters) GetValue(BionicFileExplorer.SelectedExplorerFiltersProperty); set => SetValue(BionicFileExplorer.SelectedExplorerFiltersProperty, value); }

    static BionicFileExplorer()
    {
      FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(
        typeof(BionicFileExplorer),
        new FrameworkPropertyMetadata(typeof(BionicFileExplorer)));
    }

    public BionicFileExplorer()
    {
      this.ItemContainerActionTable = new Dictionary<object, Action<TreeViewItem>>();
      this.ExpandedExplorerItemModels = new Dictionary<Guid, IFileSystemItemModel>();
      this.FileSystemHandler = new FileSystemHandler();
      this.Loaded += OnLoaded;
      this.ExplorerFilterValues = new ObservableCollection<ExplorerFilters>(Enum.GetValues(typeof(ExplorerFilters)).Cast<ExplorerFilters>().Where(filter => Math.Log((int) filter, 2) % 1 == 0));
    }

    #region Overrides of ItemsControl

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
      base.PrepareContainerForItemOverride(element, item);
      Application.Current.Dispatcher?.InvokeAsync(
        () =>
        {
          SetItemIconPresenterContent(element as FrameworkElement);
        },
        DispatcherPriority.Loaded);
    }

    #endregion

    #region Overrides of TreeView

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
      base.OnItemsChanged(e);
      if (this.IsItemsChangeInternal || !(e.Action == NotifyCollectionChangedAction.Add  || e.Action ==  NotifyCollectionChangedAction.Replace))
      {
        return;
      }

      foreach (IFileSystemItemModel fileSystemItemModel in e.NewItems.OfType<IFileSystemItemModel>())
      {
        this.FileSystemHandler.AddFileSystemItemModelToExplorerTree(fileSystemItemModel, this.PreloadLevelDepth);
      }
    }

    #endregion

    private void ApplyItemIconsOnExpandedItem(TreeViewItem treeViewItem) => SetItemIconPresenterContent(treeViewItem, true);

    private void ApplyItemIconsOnCollapsedItem(TreeViewItem treeViewItem) => SetItemIconPresenterContent(treeViewItem);

    protected virtual void PreloadChildItems(IFileSystemItemModel fileSystemItem)
    {
      int preloadLevelDepth = this.PreloadLevelDepth;
      Task.Run(() => { this.FileSystemHandler.PreloadChildTree(fileSystemItem, preloadLevelDepth); })
        .ConfigureAwait(false);
    }

    private bool TryApplyActionOnChildItems(TreeViewItem treeViewItem, Action<TreeViewItem> childItemAction)
    {
      if (treeViewItem.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
      {
        if (!this.ItemContainerActionTable.ContainsKey(treeViewItem.ItemContainerGenerator))
        {
          this.ItemContainerActionTable.Add(treeViewItem.ItemContainerGenerator, childItemAction);
        }
        treeViewItem.ItemContainerGenerator.StatusChanged += ApplyActionOnItemContainerGeneratorStatusChanged;
        return true;
      }

      return false;
    }

    private void ApplyActionOnItemContainerGeneratorStatusChanged(object sender, EventArgs eventArgs)
    {
      var itemContainerGenerator = sender as ItemContainerGenerator;
      if (itemContainerGenerator.Status == GeneratorStatus.ContainersGenerated && this.ItemContainerActionTable.TryGetValue(itemContainerGenerator, out Action<TreeViewItem> itemContainerAction))
      {
        itemContainerGenerator.StatusChanged -= ApplyActionOnItemContainerGeneratorStatusChanged;

        this.ItemContainerActionTable.Remove(itemContainerGenerator);

        foreach (TreeViewItem itemContainer in itemContainerGenerator.Items
          .OfType<IFileSystemItemModel>()
          .Select(itemContainerGenerator.ContainerFromItem)
          .Cast<TreeViewItem>()
          .Where(element => !element.IsLoaded))
        {
          this.ItemContainerActionTable.Add(itemContainer, itemContainerAction);
          itemContainer.Loaded += ExecuteActionOnItemContainerLoaded;
        }
      }
    }

    private void ExecuteActionOnItemContainerLoaded(object sender, RoutedEventArgs e)
    {
      var itemContainer = sender as TreeViewItem;
      itemContainer.Loaded -= ExecuteActionOnItemContainerLoaded;

      if (this.ItemContainerActionTable.TryGetValue(itemContainer, out Action<TreeViewItem> itemContainerAction))
      {
        this.ItemContainerActionTable.Remove(itemContainer);
        itemContainerAction.Invoke(itemContainer);
      }
    }

    private void SetItemIconPresenterContent(FrameworkElement itemContainer, bool isContainerExpanded = false)
    {
      if (itemContainer.TryFindVisualChildElementByName(
        "PART_ItemIconPresenter",
        out FrameworkElement iconPresenter))
      {
        if (iconPresenter is ContentControl contentControl &&
            itemContainer.DataContext is IFileSystemItemModel fileSystemItemModel)
        {
          contentControl.Content = fileSystemItemModel.IsDirectory && isContainerExpanded
            ? this.FileSystemItemIconSelector.SelectOpenedFolderIconSource(fileSystemItemModel)
            : fileSystemItemModel.IsDirectory
              ? this.FileSystemItemIconSelector.SelectClosedFolderIconSource(fileSystemItemModel)
              : this.FileSystemItemIconSelector.SelectFileIconSource(fileSystemItemModel);
        }
      }
    }

    protected virtual async void OnLoaded(object sender, RoutedEventArgs e)
    {
      this.FileSystemHandler.FileSystemTreeChanged += LoadItemsOnFileSystemTreeCreated;
      AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(OnItemContainerExpanded));
      AddHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler(OnItemContainerCollapsed));
      await this.FileSystemHandler.InitializeRootFolderAsync(this.PreloadLevelDepth);
    }

    protected virtual void OnItemContainerExpanded(object sender, RoutedEventArgs e)
    {
      if (!(e.OriginalSource is TreeViewItem treeViewItem &&
            treeViewItem.DataContext is IFileSystemItemModel fileSystemItem))
      {
        return;
      }

      if (!this.ExpandedExplorerItemModels.TryGetValue(fileSystemItem.Id, out IFileSystemItemModel fileSystemItemClone))
      {
        fileSystemItemClone = fileSystemItem.CreateModel();
        fileSystemItemClone.Id = fileSystemItem.Id;
        this.ExpandedExplorerItemModels.Add(fileSystemItemClone.Id, fileSystemItemClone);
      }
      if (this.ExpandedExplorerItemModels.TryGetValue(fileSystemItem.ParentFileSystemItem.Id, out IFileSystemItemModel fileSystemItemParentClone))
      {
        fileSystemItemClone.ParentFileSystemItem = fileSystemItemParentClone;
        fileSystemItemParentClone.ChildFileSystemItems.Add(fileSystemItemClone);
      }

      CollectionViewSource.GetDefaultView(fileSystemItem.ChildFileSystemItems).Filter = ExplorerItemsFilter;
      ApplyItemIconsOnExpandedItem(treeViewItem);
      PreloadChildItems(fileSystemItem);
      if (!TryApplyActionOnChildItems(treeViewItem, itemContainer => SetItemIconPresenterContent(itemContainer)))
      {
        ;
      }
      RestorePreviouslyExpandedPath(fileSystemItem);
    }

    protected virtual void OnItemContainerCollapsed(object sender, RoutedEventArgs e)
    {
      if (!(e.OriginalSource is TreeViewItem treeViewItem &&
            treeViewItem.DataContext is IFileSystemItemModel fileSystemItem))
      {
        return;
      }

      if (fileSystemItem.ParentFileSystemItem == null || fileSystemItem.ParentFileSystemItem ==
          this.FileSystemHandler.VirtualExplorerRootDirectory)
      {
        this.ExpandedExplorerItemModels.Remove(fileSystemItem.Id);
      }
      else if (this.ExpandedExplorerItemModels.TryGetValue(fileSystemItem.ParentFileSystemItem.Id, out IFileSystemItemModel parentClone))
      {
        IFileSystemItemModel fileSystemItemModelClone = parentClone.ChildFileSystemItems.First(item => item.Id.Equals(fileSystemItem.Id));
        parentClone.ChildFileSystemItems.Remove(fileSystemItemModelClone);
        if (!fileSystemItemModelClone.ChildFileSystemItems.Any())
        {
          this.ExpandedExplorerItemModels.Remove(fileSystemItem.Id);
        }
      }

      ApplyItemIconsOnCollapsedItem(treeViewItem);
    }

    private static void OnSelectedFiltersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var _this = d as BionicFileExplorer;
      var filters = (ExplorerFilters) e.NewValue;
      _this.Items.Filter = _this.ExplorerItemsFilter;
      _this.Items
        .OfType<IFileSystemItemModel>()
        .Select(itemModel => _this.ItemContainerGenerator.ContainerFromItem(itemModel))
        .Cast<TreeViewItem>()
        .Where(itemModelContainer => itemModelContainer.IsExpanded)
        .ToList()
        .ForEach(itemModelContainer => itemModelContainer.IsExpanded = false);
      foreach (IFileSystemItemModel explorerItemModel in _this.Items.OfType<IFileSystemItemModel>())
      {
        _this.RestorePreviouslyExpandedPath(explorerItemModel);
      }
    }

    private void RestorePreviouslyExpandedPath(IFileSystemItemModel explorerItemModel)
    {
      IFileSystemItemModel pathRoot = explorerItemModel;
      while (pathRoot.ParentFileSystemItem != null && pathRoot.ParentFileSystemItem != this.FileSystemHandler.VirtualExplorerRootDirectory)
      {
        pathRoot = pathRoot.ParentFileSystemItem;
      }

      if (this.Items.Contains(pathRoot) && this.ExpandedExplorerItemModels.TryGetValue(
            pathRoot.Id,
            out IFileSystemItemModel rootModelClone))
      {
        var rootModelItemContainer = this.ItemContainerGenerator.ContainerFromItem(pathRoot) as TreeViewItem;
        ExpandedChildren(rootModelItemContainer, rootModelClone);
      }
    }

    private void ExpandedChildren(TreeViewItem explorerItemModelContainer, IFileSystemItemModel explorerItemModelClone)
    {
      var explorerItemModel = explorerItemModelContainer.DataContext as IFileSystemItemModel;
      foreach (IFileSystemItemModel childFileSystemItemClone in explorerItemModelClone.ChildFileSystemItems)
      {
        IFileSystemItemModel childItem =
          explorerItemModel.ChildFileSystemItems.FirstOrDefault(
            itemModel => itemModel.Id.Equals(childFileSystemItemClone.Id));
        if (childItem != null)
        {
          TryApplyActionOnChildItems(explorerItemModelContainer, itemContainer => RestorePreviouslyExpandedPath(childFileSystemItemClone));
        }

      }
      explorerItemModelContainer.IsExpanded = true;
    }

    private bool ExplorerItemsFilter(object explorerItem)
    {
        var fileSystemModel = explorerItem as IFileSystemItemModel;
        if (fileSystemModel.IsDrive)
        {
          return true;
        }
        if (this.SelectedExplorerFilters.HasFlag(ExplorerFilters.HiddenAndSystem) && fileSystemModel.IsHidden && fileSystemModel.IsSystem)
        {
          return false;
        }
        if (this.SelectedExplorerFilters.HasFlag(ExplorerFilters.HiddenDirectories) && fileSystemModel.IsHidden && fileSystemModel.IsDirectory)
        {
          return false;
        }
        if (this.SelectedExplorerFilters.HasFlag(ExplorerFilters.SystemDirectories) && fileSystemModel.IsSystem && fileSystemModel.IsDirectory)
        {
          return false;
        }
        if (this.SelectedExplorerFilters.HasFlag(ExplorerFilters.System) && fileSystemModel.IsSystem)
        {
          return false;
        }
        if (this.SelectedExplorerFilters.HasFlag(ExplorerFilters.Hidden) && fileSystemModel.IsHidden)
        {
          return false;
        }
        if (this.SelectedExplorerFilters.HasFlag(ExplorerFilters.Directory) && fileSystemModel.IsDirectory)
        {
          return false;
        }
        if (this.SelectedExplorerFilters.HasFlag(ExplorerFilters.Archive) && fileSystemModel.IsArchive)
        {
          return false;
        }
        return true;
    }

    private async void LoadItemsOnFileSystemTreeCreated(object sender, ValueEventArgs<FileSystemItemModel> e)
    {
      await Application.Current.Dispatcher.InvokeAsync(
        () =>
        {
          this.IsItemsChangeInternal = true;
          this.ItemsSource = e.Value.ChildFileSystemItems;
          this.IsItemsChangeInternal = false;
        },
        DispatcherPriority.Background);
    }

    protected IFileSystemHandler FileSystemHandler { get; set; }
    private bool IsItemsChangeInternal { get; set; } 
    private Dictionary<Guid, IFileSystemItemModel> ExpandedExplorerItemModels { get; }
    private Dictionary<object, Action<TreeViewItem>> ItemContainerActionTable { get; }
  }
}