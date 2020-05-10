// ------------------------------------------------------------------------
// Rottweiler - Black Ops III File Decompiler
// Copyright (C) 2019 Philip/Scobalula
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
using CASCLibNET;
using Greyhound;
using System.ComponentModel;
using System.Windows.Data;

namespace Greyhound
{
    /// <summary>
    /// Casc View Model Class
    /// </summary>
    public class CascViewModel : INotifyPropertyChanged
    {
        #region BackingVariables
        /// <summary>
        /// Gets or Sets the Filter String
        /// </summary>
        private string BackingFilterString { get; set; }

        /// <summary>
        /// Gets or Sets the Filter Strings
        /// </summary>
        private string[] FilterStrings { get; set; }
        #endregion

        /// <summary>
        /// Gets or Sets the filter string
        /// </summary>
        public string FilterString
        {
            get
            {
                return BackingFilterString;
            }
            set
            {
                if (value != BackingFilterString)
                {
                    BackingFilterString = value;
                    FilterStrings = string.IsNullOrWhiteSpace(BackingFilterString) ? null : BackingFilterString.Split(',');
                    FilesView.Refresh();
                    OnPropertyChanged("FilterString");
                }
            }
        }

        /// <summary>
        /// Gets or Sets the Collection View for the Files
        /// </summary>
        private ICollectionView FilesView { get; set; }

        /// <summary>
        /// Gets the observable collection of files
        /// </summary>
        public UIItemList<CASCFileInfo> Files { get; } = new UIItemList<CASCFileInfo>();

        /// <summary>
        /// Property Changed Event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Creates a new Viewmodel
        /// </summary>
        public CascViewModel()
        {
            FilesView = CollectionViewSource.GetDefaultView(Files);
            FilesView.Filter = delegate (object obj)
            {
                if (FilterStrings != null && FilterStrings.Length > 0 && obj is CASCFileInfo file)
                {
                    var fileName = file.FileName.ToLower();

                    foreach (var filterString in FilterStrings)
                    {
                        if (!string.IsNullOrWhiteSpace(filterString) && fileName.Contains(filterString.ToLower()))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                return true;
            };
        }

        /// <summary>
        /// Updates the Property on Change
        /// </summary>
        /// <param name="name">Property Name</param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}