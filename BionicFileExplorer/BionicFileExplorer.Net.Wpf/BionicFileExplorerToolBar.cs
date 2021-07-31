using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BionicFileExplorer.Net.Wpf
{
  public class BionicFileExplorerToolBar : Control
  {
    static BionicFileExplorerToolBar()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(BionicFileExplorerToolBar), new FrameworkPropertyMetadata(typeof(BionicFileExplorerToolBar)));
    }
  }
}
