using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhilLibX;
using PhilLibX.Imaging;

namespace Greyhound.Logic
{
    public class AnimAsset : Asset
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
        /// Gets or Sets the Anim Object
        /// </summary>
        public AnimationTemp Anim { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public override void ClearData()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override int CompareTo(object obj)
        {
            if (obj is AnimAsset anim)
            {
                return 1;
            }
            else if (obj is Asset asset)
            {
                return Type.CompareTo(asset.Type);
            }

            return 1;
        }

        public override void Save(string basePath)
        {
            LoadMethod(this);
        }
    }
}
