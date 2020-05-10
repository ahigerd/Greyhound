using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhilLibX;
using PhilLibX.Imaging;

namespace Greyhound.Logic
{
    /// <summary>
    /// A class to hold a model asset
    /// </summary>
    public class ModelAsset : Asset
    {
        /// <summary>
        /// Gets or Sets the Bone Names
        /// </summary>
        public List<string> BoneNames { get; set; }

        /// <summary>
        /// Gets or Sets the number of Bones
        /// </summary>
        public int BoneCount { get; set; }

        /// <summary>
        /// Gets or Sets the number of LODs
        /// </summary>
        public int LODCount { get; set; }

        /// <summary>
        /// Gets or Sets the number of vertices in the highest LOD
        /// </summary>
        public int VertexCount { get; set; }

        /// <summary>
        /// Gets or Sets the number of faces in the highest LOD
        /// </summary>
        public int FaceCount { get; set; }

        /// <summary>
        /// Gets or Sets the number of meshes in the highest LOD
        /// </summary>
        public int MeshCount { get; set; }

        /// <summary>
        /// Gets or Sets the Materials
        /// </summary>
        public Dictionary<string, MaterialAsset> Materials { get; set; }

        /// <summary>
        /// Gets or Sets the LODs
        /// </summary>
        public List<ModelTemp> LODs { get; set; }

        public override void ClearData()
        {
            LODs = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override int CompareTo(object obj)
        {
            if (obj is ModelAsset model)
            {
                return Name.CompareTo(model.Name);
            }
            else if (obj is Asset asset)
            {
                return Type.CompareTo(asset.Type);
            }

            return 1;
        }

        public override void Save(string basePath)
        {
            Instance.SkipExistingImages = false;
            var dir = Path.Combine(basePath, Path.GetFileNameWithoutExtension(Name));
            var img = Path.Combine(dir, "_images");


            var thing = Stopwatch.StartNew();

            LoadMethod(this);

            foreach (var mtl in Materials)
            {
                mtl.Value.Save(img);
            }

            int lodIndex = 0;

            // Export each LOD, assuming highest first
            foreach(var lod in LODs)
            {
                // Fix up image slots from the og material
                // Use the last exported extension
                foreach(var mtl in lod.Materials)
                {
                    var mtlAsset = Materials[mtl.Name];

                    if (mtlAsset.ImageSlots.TryGetValue("colorMap", out var image))
                        mtl.Images["DiffuseMap"] = Path.Combine("_images", mtl.Name, image.DisplayName + "." + image.Extension ?? ".png");
                }

                var path = Path.Combine(dir, string.Format("{0}_LOD{1}.semodel", Name, lodIndex++));

                foreach (var format in Instance.ModelWriters)
                    format.Export(lod, path);
            }


            Instance.Log((thing.ElapsedMilliseconds / 1000.0).ToString(), "DEBUG");
            Console.WriteLine(thing.ElapsedMilliseconds / 1000.0);
        }
    }
}