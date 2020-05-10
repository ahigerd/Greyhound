using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhilLibX.Imaging;

namespace Greyhound.Logic
{
    public class MaterialAsset : Asset
    {
        /// <summary>
        /// Gets or Sets the Material Type/Techset
        /// </summary>
        public string MaterialType { get; set; }

        /// <summary>
        /// Gets or Sets the Image Slots
        /// </summary>
        public Dictionary<string, ImageAsset> ImageSlots { get; set; }

        /// <summary>
        /// Gets or Sets the Material Settings
        /// </summary>
        public Dictionary<string, string> Settings { get; set; }

        public MaterialAsset()
        {
            ImageSlots = new Dictionary<string, ImageAsset>();
            Settings = new Dictionary<string, string>();
        }

        /// <summary>
        /// Clears loaded asset data
        /// </summary>
        public override void ClearData()
        {
            if(ImageSlots != null)
            {
                foreach(var image in ImageSlots)
                {
                    image.Value?.ClearData();
                }

                ImageSlots?.Clear();
                Settings?.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override int CompareTo(object obj)
        {
            if (obj is Asset asset)
            {
                return Type.CompareTo(asset.Type);
            }

            return 1;
        }

        public override void Save(string basePath)
        {
            var dir = Path.Combine(basePath, Name);

            Directory.CreateDirectory(dir);

            LoadMethod?.Invoke(this);

            foreach(var image in ImageSlots)
            {
                try
                {
                    image.Value.Save(dir);
                }
                catch(Exception e)
                {
                    Instance.Log(e.ToString(), "Yohan");
                }
            }
        }
    }
}
