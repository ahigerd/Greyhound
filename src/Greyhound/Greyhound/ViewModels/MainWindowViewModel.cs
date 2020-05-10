// ------------------------------------------------------------------------
// Tyrant - RE Engine Extractor
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
using System.Windows.Data;
using Greyhound.Logic;

namespace Greyhound
{
    public class MainWindowViewModel : Notifiable
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
                    FilterStrings = string.IsNullOrWhiteSpace(BackingFilterString) ? null : BackingFilterString.Split(' ');
                    AssetsView.Refresh();
                    NotifyPropertyChanged("FilterString");
                }
            }
        }

        /// <summary>
        /// Gets or Sets the Collection View for the Assets
        /// </summary>
        private ICollectionView AssetsView { get; set; }

        public bool IOButtonsEnabled
        {
            get
            {
                return GetValue<bool>("IOButtonsEnabled");
            }
            set
            {
                SetValue(value, "IOButtonsEnabled");
            }
        }

        /// <summary>
        /// Gets the observable collection of assets
        /// </summary>
        public UIItemList<Asset> Assets { get; } = new UIItemList<Asset>();

        public Visibility DimmerVisibility
        {
            get
            {
                return GetValue<Visibility>("DimmerVisibility");
            }
            set
            {
                SetValue(value, "DimmerVisibility");
            }
        }

        public MainWindowViewModel()
        {
            IOButtonsEnabled = true;
            DimmerVisibility = Visibility.Hidden;
            AssetsView = CollectionViewSource.GetDefaultView(Assets);
            AssetsView.Filter = delegate (object obj)
            {
                if(FilterStrings != null && FilterStrings.Length > 0 && obj is Asset asset)
                {
                    var assetName = asset.Name.ToLower();
                    var assetType = asset.Type.ToLower();
                    foreach(var filterString in FilterStrings)
                    {
                        if(filterString.StartsWith("type:") && assetType.Contains(filterString.Replace("type:", "").ToLower()))
                        {
                            return true;
                        }
                        
                        if(!string.IsNullOrWhiteSpace(filterString) && assetName.Contains(filterString.ToLower()))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                return true;
            };
        }
    }
}
