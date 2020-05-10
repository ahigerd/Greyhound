// ------------------------------------------------------------------------
// Rottweiler - Fast File Image/Sound Extractor for CoD
// Copyright (C) 2018 Philip/Scobalula
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ------------------------------------------------------------------------
using System.ComponentModel;
using System.Windows;

namespace Greyhound
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private ProgressWindowViewModel ViewModel { get; } = new ProgressWindowViewModel();

        /// <summary>
        /// Gets or Sets the Background Worker
        /// </summary>
        public BackgroundWorker Worker { get; set; }

        /// <summary>
        /// Gets or Sets the Worker Args
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Initializes Progress Window
        /// </summary>
        public ProgressWindow(DoWorkEventHandler work, ProgressChangedEventHandler changed, RunWorkerCompletedEventHandler complete, string message, object data, double count, Window owner)
        {
            InitializeComponent();

            // Init Worker
            Worker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true,
            };

            Worker.ProgressChanged += ProgressChanged;
            Worker.RunWorkerCompleted += ProgressComplete;

            if(work != null)
                Worker.DoWork += work;
            if (changed != null)
                Worker.ProgressChanged += changed;
            if (complete != null)
                Worker.RunWorkerCompleted += complete;

            // Set up initial data
            DataContext     = ViewModel;
            Owner           = owner;
            Data            = data;
            ViewModel.Count = count;
            ViewModel.Value = 0;
            ViewModel.Text  = message;

            // We only need to run the worker once loaded
            Loaded += StartWorker;
        }

        /// <summary>
        /// Starts the worker 
        /// </summary>
        private void StartWorker(object sender, RoutedEventArgs e)
        {
            Worker.RunWorkerAsync(this);
        }

        /// <summary>
        /// Handles on progress complete
        /// </summary>
        private void ProgressComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Updates bar on progress changed
        /// </summary>
        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if(!Worker.CancellationPending)
            {
                if(e.UserState != null)
                    ViewModel.Text = e.UserState?.ToString();
                ViewModel.Indeterminate = e.ProgressPercentage < 0;
                ViewModel.Value++;
            }
        }

        /// <summary>
        /// Sets Cancelled to true to update current task
        /// </summary>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = Worker.IsBusy;
        }

        /// <summary>
        /// Closes Window on Cancel click
        /// </summary>
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Text = "Cancelling task...";
            Worker?.CancelAsync();
        }
    }
}
