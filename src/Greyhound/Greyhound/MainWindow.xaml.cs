using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Greyhound.Logic;
using System.ComponentModel;
using System.Diagnostics;
using PhilLibX.IO;
using CASCLibNET;

namespace Greyhound
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Gets the ViewModel
        /// </summary>
        public MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();

        public MainWindow()
        {
            DataContext = ViewModel;
            InitializeComponent();
        }

        /// <summary>
        /// Opens file dialog to open a package
        /// </summary>
        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Greyhound | Open File",
                Filter = "All files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                // If we're provided with an exe, we open CASC
                if(Path.GetExtension(dialog.FileName) == ".exe")
                {
                    // LoadCASC(Path.GetDirectoryName(dialog.FileName));
                }
                else
                {
                    ViewModel.Assets.ClearAllItems();
                    Instance.ClearPackages();
                    ViewModel.DimmerVisibility = Visibility.Visible;
                    new ProgressWindow(LoadFiles, null, ProgressComplete, "Loading Files...", dialog.FileNames, dialog.FileNames.Length, this).ShowDialog();
                }
            }
        }

        private void LoadCASC(string cascDir)
        {
            var files = new List<CASCFileInfo>();

            using (var casc = new CASCStorage(cascDir))
            {
                ViewModel.DimmerVisibility = Visibility.Visible;
                var selectionWindow = new CascSelectionWindow
                {
                    Owner = this
                };
                selectionWindow.ViewModel.Files.AddRange(casc.Files);
                selectionWindow.ViewModel.Files.SendNotify();
                selectionWindow.ShowDialog();
                files = selectionWindow.FileList.SelectedItems.Cast<CASCFileInfo>().ToList();
                ViewModel.DimmerVisibility = Visibility.Hidden;
            }

            if (files.Count == 0)
                return;
        }

        /// <summary>
        /// Handles on progress complete
        /// </summary>
        private void ProgressComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            ViewModel.DimmerVisibility = Visibility.Hidden;
            ViewModel.Assets.SendNotify();
            Title = string.Format("Greyhound | {0} Assets Loaded", ViewModel.Assets.Count);
        }

        private void LoadFiles(object sender, DoWorkEventArgs e)
        {
            var window = e.Argument as ProgressWindow;
            var files = window.Data as string[];
            var index = 0;

            // Load every file provided, we'll use the package method to resolve what package type this is, so we don't actually care
            // about extensions
            foreach (var file in files)
            {
                index++;
                // Store here so we can dispose, but only if an error has occured
                Stream stream = null;
                IPackage package = null;

                // Report back
                window.Worker.ReportProgress(0, string.Format("Loading {0} ({1}/{2})....", Path.GetFileName(file), index, files.Length));

                try
                {
                    // Load in sharing since if we open it while game is running we'll be like Dave Brooks Marriage, a failure
                    stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    package = Instance.GetPackageClass(stream, Path.GetExtension(file));
                    // Add package and load it and its assets
                    Instance.LoadedPackages.Add(package);
                    package.Load(stream, file);
                    ViewModel.Assets.AddRange(package.Assets);
                }
                catch (Exception exception)
                {
                    // Only dispose if an error has occured
                    stream?.Dispose();
                    package?.Dispose();
                    // Log
                    Instance.Log(exception.ToString(), "ERROR");
                }

                // Validate if we're cancelling
                if (window.Worker.CancellationPending)
                    break;
            }

            // Once we're done, sort
            ViewModel.Assets.Sort();
        }

        private void ProgressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handles export on double click
        /// </summary>
        private void AssetListMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is Asset asset)
            {
                new ProgressWindow(ExportAssets, null, ProgressComplete, "Exporting Assets...", new List<Asset>() { asset }, 1, this).ShowDialog();
            }
        }

        private void AssetListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        public void OnExportComplete(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        public void ExportAssets(object sender, DoWorkEventArgs e)
        {
            var window = e.Argument as ProgressWindow;

            Parallel.ForEach(window.Data as List<Asset>, new ParallelOptions { MaxDegreeOfParallelism = Instance.ExportThreadCount }, (asset, loop) =>
            {
                window.Worker.ReportProgress(0);

                try
                {
                    Instance.Log(string.Format("Exporting {0} from Game {1} of Type {2}", asset.Name, asset.Game, asset.Type), "INFO");
                    asset.Save(Path.Combine(Instance.ExportDirectory, asset.Game, asset.Type));
                    asset.Status = "Exported";
                    Instance.Log(string.Format("Exported {0} successfully", asset.Name), "INFO");
                }
                catch (Exception exception)
                {
                    Instance.Log(string.Format("Failed to export {0} from Game {1} of Type {2}. Error:\n{3}", asset.Name, asset.Game, asset.Type, exception.ToString()), "INFO");
                    asset.Status = "Error";
                }
                finally
                {
                    asset.ClearData();
                }

                if (window.Worker.CancellationPending)
                    loop.Break();
            });
        }

        /// <summary>
        /// Exports all loaded assets
        /// </summary>
        private void ExportAllClick(object sender, RoutedEventArgs e)
        {
            var assets = AssetList.Items.Cast<Asset>().ToList();

            if (assets.Count == 0)
            {
                ViewModel.DimmerVisibility = Visibility.Visible;
                MessageBox.Show("There are no assets listed to export.", "Greyhound | Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ViewModel.DimmerVisibility = Visibility.Hidden;
                return;
            }

            new ProgressWindow(ExportAssets, null, ProgressComplete, "Exporting Assets...", assets, assets.Count, this).ShowDialog();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Not exactly required, but just clean up streams before exiting
            Instance.ClearPackages();
        }

        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsWindow()
            {
                Owner = this
            };

            ViewModel.DimmerVisibility = Visibility.Visible;
            settings.SetUIFromSettings(Instance.GetSettings);
            settings.ShowDialog();
            settings.SetSettingsFromUI(Instance.GetSettings);
            ViewModel.DimmerVisibility = Visibility.Hidden;
        }

        private void PreviewButtonClick(object sender, RoutedEventArgs e)
        {
            ViewModel.DimmerVisibility = Visibility.Visible;
            MessageBox.Show("Asset Previewer is still in development.", "Greyhound | Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ViewModel.DimmerVisibility = Visibility.Hidden;
        }

        private void ClearDataClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Assets.ClearAllItems();
            Instance.ClearPackages();
            GC.Collect();
        }

        private void OpenMemoryClick(object sender, RoutedEventArgs e)
        {
            ViewModel.DimmerVisibility = Visibility.Visible;
            MessageBox.Show("In-Game Asset Loading is still in development.", "Greyhound | Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ViewModel.DimmerVisibility = Visibility.Hidden;
        }
    }
}
