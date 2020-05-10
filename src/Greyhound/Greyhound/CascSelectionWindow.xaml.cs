using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Greyhound
{
    /// <summary>
    /// Interaction logic for CascSelectionWindow.xaml
    /// </summary>
    public partial class CascSelectionWindow : Window
    {
        /// <summary>
        /// Gets the ViewModel
        /// </summary>
        public CascViewModel ViewModel { get; } = new CascViewModel();

        /// <summary>
        /// Creates a new Selection Window
        /// </summary>
        public CascSelectionWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        /// <summary>
        /// Closes the window on load click
        /// </summary>
        private void LoadClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
