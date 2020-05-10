using PhilLibX.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Greyhound.Logic
{
    /// <summary>
    /// A class to handle loading a IWD file
    /// </summary>
    public class IWDPackage : IPackage
    {
        /// <summary>
        /// Gets or Sets the Archive
        /// </summary>
        private ZipArchive Archive { get; set; }

        /// <summary>
        /// Gets the Package Name
        /// </summary>
        public string Name { get; } = "IWD File";

        /// <summary>
        /// Gets the Game this Package is from
        /// </summary>
        public string Game { get; set; }

        /// <summary>
        /// Gets or Sets the File Name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or Sets the Package Extensions
        /// </summary>
        public string[] Extensions { get; } = new string[]
        {
            ".iwd",
            ".pk3",
            ".zip"
        };

        /// <summary>
        /// Gets assets that can be exported directly from the package
        /// </summary>
        public List<Asset> Assets
        {
            get
            {
                var results = new List<Asset>(Entries.Count);
                var cod2 = Game == "Call of Duty 2";
                var cod1 = Game == "Call of Duty 1";

                foreach (var entry in Entries)
                {
                    if (entry.Value is IWDPackageEntry iwdPackage)
                    {
                        var name = entry.Value.Name;
                        var fileName = Path.GetFileNameWithoutExtension(name);
                        var folder = Path.GetDirectoryName(name);
                        var ext = Path.GetExtension(name);

                        if (fileName == "")
                            continue;

                        //if (ext == ".iwi" || ext == ".dds")
                        //{
                        //    results.Add(new ImageAsset()
                        //    {
                        //        Name = name,
                        //        Type = "image",
                        //        Information = string.Format("Size: 0x{0}", iwdPackage.ArchiveEntry.Length.ToString("X")),
                        //        Status = "Loaded",
                        //        LoadMethod = LoadIWDImage,
                        //        Data = entry.Value,
                        //        Game = Game
                        //    });
                        //}
                        if (ext == ".wav")
                        {
                            results.Add(new ImageAsset()
                            {
                                Name = name,
                                Type = "sound",
                                Information = string.Format("Size: 0x{0}", iwdPackage.ArchiveEntry.Length.ToString("X")),
                                Status = "Loaded",
                                LoadMethod = LoadIWDImage,
                                Data = entry.Value,
                                Game = Game
                            });
                        }
                        else if(folder == "materials" && cod2)
                        {
                            results.Add(new MaterialAsset()
                            {
                                Name        = name,
                                Type        = "material",
                                Information = string.Format("Size: 0x{0}", iwdPackage.ArchiveEntry.Length.ToString("X")),
                                Status      = "Loaded",
                                LoadMethod  = LoadIWDMaterial,
                                Data        = entry.Value,
                                Game        = Game
                            });
                        }
                        // CoD 1/2
                        else if (folder == "xmodel")
                        {
                            results.Add(new ModelAsset()
                            {
                                Name        = fileName,
                                Type        = "xmodel",
                                Information = "N/A",
                                Status      = "Loaded",
                                Data        = entry.Value,
                                Game = Game,
                                LoadMethod  = LoadIWDXModel
                            });
                        }
                        // CoD 1/2
                        else if (folder == "xanim")
                        {
                            results.Add(new AnimAsset()
                            {
                                Name = fileName,
                                Type = "xanim",
                                Information = "N/A",
                                Status = "Loaded",
                                LoadMethod = LoadIWDXAnim,
                                Game = Game,
                                Data = entry.Value
                            });
                        }
                    }
                }

                return results;
            }
        }

        /// <summary>
        /// Gets or Sets the Package Entries
        /// </summary>
        public Dictionary<string, PackageEntry> Entries { get; set; }

        /// <summary>
        /// Disposes of the Package
        /// </summary>
        public void Dispose()
        {
            Archive?.Dispose();
        }



        /// <summary>
        /// Loads data from the Package
        /// </summary>
        /// <param name="stream">Stream to load</param>
        public byte[] Extract(PackageEntry entry, int size)
        {
            lock(Archive)
            {
                if (!(entry is IWDPackageEntry iwdEntry))
                    throw new Exception("Entry is not an IWD Entry");

                using (var stream = new MemoryStream())
                {
                    using (var compressed = iwdEntry.ArchiveEntry.Open())
                        compressed.CopyTo(stream);

                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Loads data from the Package
        /// </summary>
        /// <param name="stream">Stream to load</param>
        public void Load(Stream stream, string name)
        {
            Entries = new Dictionary<string, PackageEntry>();
            Archive?.Dispose();
            Archive = new ZipArchive(stream);

            var toCompare = name.ToLower();

            // Check path to build where this is from
            if (toCompare.Contains("call of duty 2"))
                Game = "Call of Duty 2";
            else if (toCompare.Contains("black ops"))
                Game = "Call of Duty Black Ops";
            else if (toCompare.Contains("call of duty 4"))
                Game = "Call of Duty 4 Modern Warfare";
            else if (toCompare.Contains("modern warfare 2"))
                Game = "Call of Duty Modern Warfare 2";
            else if (toCompare.Contains("modern warfare 3"))
                Game = "Call of Duty Modern Warfare 3";
            else
                Game = "Call of Duty World at War";

            foreach (var file in Archive.Entries)
                Entries[file.FullName] = new IWDPackageEntry(file, this);
        }

        /// <summary>
        /// Checks if the given file is valid for this package type
        /// </summary>
        /// <param name="stream">Stream to load</param>
        /// <param name="initialBuffer">16-Bytes at the start of the file for further verification</param>
        /// <param name="extension">File extension</param>
        /// <returns>True if valid for this package, otherwise false</returns>
        public bool IsFileValid(Stream stream, byte[] initialBuffer, string extension)
        {
            return Extensions.Contains(extension);
        }

        /// <summary>
        /// Clones the Package
        /// </summary>
        /// <returns>Cloned Package</returns>
        public IPackage CreateNew()
        {
            return new IWDPackage();
        }

        /// <summary>
        /// Loads a material from an IWD, currently only for CoD 2
        /// </summary>
        public static void LoadIWDMaterial(Asset asset)
        {
            if(asset is MaterialAsset mtl)
            {
                using (var mtlReader = new BinaryReader(new MemoryStream(Instance.ExtractPackageEntry(asset.Data, -1))))
                {
                    var mtlNameOffset = mtlReader.ReadUInt32();
                    mtlReader.BaseStream.Position = 52;
                    var textureCount = mtlReader.ReadUInt16();
                    var constantCount = mtlReader.ReadUInt16();
                    var techSetOffset = mtlReader.ReadUInt32();
                    var texturesOffset = mtlReader.ReadUInt32();

                    mtlReader.BaseStream.Position = texturesOffset;

                    for (uint i = 0; i < textureCount; i++)
                    {
                        var nameOffset    = mtlReader.ReadUInt32();
                        var flags         = mtlReader.ReadUInt32();
                        var imgNameOffset = mtlReader.ReadUInt32();

                        var pos = mtlReader.BaseStream.Position;

                        mtlReader.BaseStream.Position = nameOffset;

                        var name = mtlReader.ReadNullTerminatedString();
                        mtlReader.BaseStream.Position = imgNameOffset;
                        var imgName = mtlReader.ReadNullTerminatedString();
                        mtl.ImageSlots[name] = new ImageAsset()
                        {
                            Name = imgName,
                            Data = "images" + "/" + imgName + ".iwi",
                            LoadMethod = LoadIWDImage,
                        };

                        mtlReader.BaseStream.Position = pos;
                    }
                }
            }
        }

        /// <summary>
        /// Loads an IWD Image
        /// </summary>
        /// <param name="asset"></param>
        public static void LoadIWDImage(Asset asset)
        {
            if(asset is ImageAsset imageAsset)
            {
                var buffer = Instance.ExtractPackageEntry(asset.Data, -1);

                if (buffer == null)
                    return;

                // Use extension to determine
                if(asset.Name.EndsWith(".dds"))
                    imageAsset.RawImage = ImageHelper.ConvertDDS(buffer);
                else
                    imageAsset.RawImage = ImageHelper.ConvertIWI(buffer);
            }
        }

        public void LoadIWDXModel(Asset asset)
        {
            var model = asset as ModelAsset;
            var buffer = Instance.ExtractPackageEntry(asset.Data, -1);

            XModelFileHelper.Convert(buffer, model);
        }

        public void LoadIWDXAnim(Asset asset)
        {
            var model = asset as AnimAsset;
            var buffer = Instance.ExtractPackageEntry(asset.Data, -1);
            XAnimFileHelper.Convert(buffer, model);
        }
    }

    /// <summary>
    /// A class for IWD/ZIP Package Entries
    /// </summary>
    class IWDPackageEntry : PackageEntry
    {
        /// <summary>
        /// Gets the Name of the Package Entry
        /// </summary>
        public override string Name
        {
            get
            {
                return ArchiveEntry.FullName;
            }
            set
            {
                throw new NotImplementedException("Setting the name of an IWD Package is not supported");
            }
        }

        /// <summary>
        /// Gets or Sets the Archive Entry
        /// </summary>
        public ZipArchiveEntry ArchiveEntry { get; set; }

        /// <summary>
        /// Createsa new IWD Package Entry
        /// </summary>
        /// <param name="entry"></param>
        public IWDPackageEntry(ZipArchiveEntry entry, IPackage package)
        {
            ArchiveEntry = entry;
            BasePackage = package;
        }
    }
}
