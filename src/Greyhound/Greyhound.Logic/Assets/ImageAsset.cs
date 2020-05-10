using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhilLibX.Imaging;

namespace Greyhound.Logic
{
    public class ImageAsset : Asset
    {
        /// <summary>
        /// Gets or Sets the Image Width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or Sets the Image Width
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets the Pixel Count
        /// </summary>
        public int PixelCount { get { return Width * Height; } }

        /// <summary>
        /// Gets or Sets the Image Depth
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Gets or Sets if the Image is a cube map
        /// </summary>
        public bool CubeMap { get; set; }

        /// <summary>
        /// Gets or Sets the Image Format
        /// </summary>
        public ScratchImage.DXGIFormat Format { get; set; }

        /// <summary>
        /// Gets or Sets the Raw DirectX Image
        /// </summary>
        public ScratchImage RawImage { get; set; }
        
        /// <summary>
        /// Gets or Sets the Image Semantic
        /// </summary>
        public string Semantic { get; set; }

        /// <summary>
        /// Gets or Sets the Image Path
        /// </summary>
        public string Extension { get; set; }

        public override void ClearData()
        {
            RawImage?.Dispose();
            RawImage = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override int CompareTo(object obj)
        {
            if (obj is ImageAsset image)
            {
                // return -1 * PixelCount.CompareTo(image.PixelCount);
                return image.Name.CompareTo(Name) * -1;
            }
            else if(obj is Asset asset)
            {
                return Type.CompareTo(asset.Type);
            }

            return 1;
        }

        public override void Save(string basePath)
        {
            var dir = Path.Combine(basePath, Path.GetFileNameWithoutExtension(Name));

            var pathInfo = ImageHelper.GetExportPaths(Path.Combine(basePath, Path.GetFileNameWithoutExtension(Name)));

            // Mark Default Path
            Extension = pathInfo.Item1;

            if (pathInfo.Item2.Count == 0)
                return;

            LoadMethod(this);

            ImageHelper.SaveScratchImage(RawImage, Semantic, pathInfo.Item2);
        }
    }
}
