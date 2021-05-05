using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using JetBrains.Annotations;

namespace Bionic.FileExplorer
{
  internal class ExplorerFilterValuesDelegateBehavior
  {
    #region BindingSource attached property

    public static readonly DependencyProperty BionicFileExplorerProperty = DependencyProperty.RegisterAttached(
      "BionicFileExplorer", typeof(BionicFileExplorer), typeof(ExplorerFilterValuesDelegateBehavior), new PropertyMetadata(default(BionicFileExplorer), ExplorerFilterValuesDelegateBehavior.OnFilterSourceAttached));

    public static void SetBionicFileExplorer([NotNull] DependencyObject attachingElement, BionicFileExplorer value) => attachingElement.SetValue(ExplorerFilterValuesDelegateBehavior.BionicFileExplorerProperty, value);

    public static BionicFileExplorer GetBionicFileExplorer([NotNull] DependencyObject attachingElement) => (BionicFileExplorer) attachingElement.GetValue(ExplorerFilterValuesDelegateBehavior.BionicFileExplorerProperty);

    #endregion

    private static void OnFilterSourceAttached(DependencyObject attachingElement, DependencyPropertyChangedEventArgs e)
    {
      if (attachingElement is Selector selector)
      {
        selector.SelectionChanged += ExplorerFilterValuesDelegateBehavior.DelegateValue;
      }
    }

    private static void DelegateValue(object sender, SelectionChangedEventArgs e)
    {
      BionicFileExplorer delegationTarget;
      if (!(sender is DependencyObject attachedElement) || (delegationTarget = ExplorerFilterValuesDelegateBehavior.GetBionicFileExplorer(attachedElement)) == null)
      {
        return;
      }

      if (sender is Selector selector)
      {
        if (selector is ListBox listBox && listBox.SelectionMode != SelectionMode.Single)
        {
          delegationTarget.SelectedExplorerFilters = listBox.SelectedItems.Count == 0 
            ? ExplorerFilters.None 
            : listBox.SelectedItems.OfType<ExplorerFilters>()
            .Aggregate((combinedFilters, filter) => combinedFilters |= filter);
        }
        else
        {
          delegationTarget.SelectedExplorerFilters = selector.SelectedItem is ExplorerFilters filters 
            ? filters 
            : ExplorerFilters.None;
        }
      }
    }
  }
}
