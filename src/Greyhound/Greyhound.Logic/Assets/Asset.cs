using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Greyhound.Logic
{
    /// <summary>
    /// A class to hold a generic asset
    /// </summary>
    public class Asset : Notifiable, IDisposable, IComparable
    {
        /// <summary>
        /// Gets or Sets the Asset Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the Asset Display Name
        /// </summary>
        public string DisplayName { get { return Path.GetFileNameWithoutExtension(Name); } }

        /// <summary>
        /// Gets or Sets the Asset Type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or Sets the Asset Status
        /// </summary>
        public string Status
        {
            get
            {
                return GetValue<string>("Status");
            }
            set
            {
                SetValue(value, "Status");
                // Update Foreground
                NotifyPropertyChanged("ForegroundColor");
            }
        }

        /// <summary>
        /// Gets or Sets the Asset Information
        /// </summary>
        public string Information { get; set; }

        public Brush ForegroundColor
        {
            get
            {
                switch(Status)
                {
                    case "Loaded":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5EBEFF"));
                    case "Placeholder":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFD83D"));
                    case "Exported":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF63ff5e"));
                    case "Error":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFff6666"));
                    default:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
                }
            }
        }

        /// <summary>
        /// Gets or Sets the Game this Asset is from
        /// </summary>
        public string Game { get; set; }

        /// <summary>
        /// Gets or Sets the Asset Data
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Gets or Sets the method to load the Asset data
        /// </summary>
        public Action<Asset> LoadMethod { get; set; }

        /// <summary>
        /// Saves the Asset
        /// </summary>
        public virtual void Save(string basePath)
        {
        }

        /// <summary>
        /// Clears loaded asset data
        /// </summary>
        public virtual void ClearData()
        {
        }

        /// <summary>
        /// Disposes of the Asset
        /// </summary>
        public void Dispose()
        {
            ClearData();
        }

        /// <summary>
        /// Compares the Assets
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>A value that indicates the relative order of the objects being compared.</returns>
        public virtual int CompareTo(object obj)
        {
            if (obj is Asset asset)
            {
                return Type.CompareTo(asset.Type);
            }

            return 1;
        }

        /// <summary>
        /// Clones the Asset
        /// </summary>
        /// <returns>Cloned Asset</returns>
        public Asset Clone() => (Asset)MemberwiseClone();

        /// <summary>
        /// Che
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Asset asset)
            {
                return Name.Equals(asset.Name);
            }

            return base.Equals(obj);
        }

        /// <summary>
        /// Returns the Name of this Asset
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}
