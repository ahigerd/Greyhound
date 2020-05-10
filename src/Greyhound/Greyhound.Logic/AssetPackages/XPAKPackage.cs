using PhilLibX.Compression;
using PhilLibX.Imaging;
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
    /// A class to handle loading a XPAK file
    /// </summary>
    public class XPAKPackage : IPackage
    {
        /// <summary>
        /// XPAK Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Size = 0x200)]
        struct Header
        {
            public uint Magic;
            public ushort MinorVersion;
            public ushort MajorVersion;
            public long Unknown2;
            public long Size;
            public long FileCount;
            public long DataOffset;
            public long DataSize;
            public long IndexCount;
            public long IndexOffset;
            public long IndexSize;
            public long ReferencesCount;
            public long ReferencesOffset;
            public long ReferencesSize;
            public long MetaCount;
            public long MetaOffset;
            public long MetaSize;
        }

        /// <summary>
        /// XPAK Index Entry
        /// </summary>
        public struct IndexEntry
        {
            public ulong Key;
            public long Offset;
            public long Size;
        }

        /// <summary>
        /// XPAK Data Chunk Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct DataChunkHeader
        {
            public int Count;
            public int Offset;
            public fixed int Commands[30];
        }

        /// <summary>
        /// Gets or Sets the File Reader
        /// </summary>
        private BinaryReader Reader { get; set; }

        /// <summary>
        /// Gets the Package Name
        /// </summary>
        public string Name { get; } = "XPAK File";

        /// <summary>
        /// Gets the Game this Package is from
        /// </summary>
        public string Game { get; set; }

        /// <summary>
        /// Gets or Sets the Package Extensions
        /// </summary>
        public string[] Extensions { get; } = new string[]
        {
            ".xpak",
        };

        /// <summary>
        /// Gets or Sets the File Name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets assets that can be exported directly from the package
        /// </summary>
        public List<Asset> Assets
        {
            get
            {
                var results = new Dictionary<string, Asset>();

                foreach (var entry in Entries)
                {
                    var xpakEntry = entry.Value as XPAKPackageEntry;

                    if (xpakEntry.Type == "image")
                    {
                        if (results.TryGetValue(xpakEntry.Name, out var asset) && asset.Data is XPAKPackageEntry compare)
                        {
                            if (compare.GetKVPNumber("height") < xpakEntry.GetKVPNumber("height"))
                            {
                                results[xpakEntry.Name] = new ImageAsset()
                                {
                                    Name        = entry.Value.Name,
                                    Type        = "image",
                                    Width       = (int)xpakEntry.GetKVPNumber("height"),
                                    Height      = (int)xpakEntry.GetKVPNumber("width"),
                                    Format      = Formats[xpakEntry.GetKVPString("format")],
                                    Information = string.Format("Width: {0} Height: {1}", xpakEntry.GetKVPNumber("height"), xpakEntry.GetKVPNumber("width")),
                                    Status      = "Loaded",
                                    Data        = xpakEntry,
                                    LoadMethod  = ImageHelper.LoadGenericImage,
                                    CubeMap     = false,
                                    Game        = Game
                                };
                            }
                        }
                        else
                        {
                            results[xpakEntry.Name] = new ImageAsset()
                            {
                                Name        = entry.Value.Name,
                                Type        = "image",
                                Width       = (int)xpakEntry.GetKVPNumber("height"),
                                Height      = (int)xpakEntry.GetKVPNumber("width"),
                                Format      = Formats[xpakEntry.GetKVPString("format")],
                                Information = string.Format("Width: {0} Height: {1}", xpakEntry.GetKVPNumber("height"), xpakEntry.GetKVPNumber("width")),
                                Status      = "Loaded",
                                Data        = xpakEntry,
                                CubeMap     = false,
                                LoadMethod  = ImageHelper.LoadGenericImage,
                                Game        = Game
                            };
                        }
                    }
                }

                return results.Values.ToList();
            }
        }

        /// <summary>
        /// XPAK Formats and matching DXGI Format
        /// </summary>
        public static Dictionary<string, ScratchImage.DXGIFormat> Formats = new Dictionary<string, ScratchImage.DXGIFormat>()
        {
            { "BC1",                ScratchImage.DXGIFormat.BC1UNORM  },
            { "BC1_SRGB",           ScratchImage.DXGIFormat.BC1UNORM  },
            { "BC2",                ScratchImage.DXGIFormat.BC2UNORM  },
            { "BC2_SRGB",           ScratchImage.DXGIFormat.BC2UNORM  },
            { "BC3",                ScratchImage.DXGIFormat.BC3UNORM  },
            { "BC3_SRGB",           ScratchImage.DXGIFormat.BC3UNORM  },
            { "BC4",                ScratchImage.DXGIFormat.BC4UNORM  },
            { "BC4_SNORM",          ScratchImage.DXGIFormat.BC4UNORM  },
            { "BC5",                ScratchImage.DXGIFormat.BC5UNORM  },
            { "BC5_SNORM",          ScratchImage.DXGIFormat.BC5SNORM  },
            { "BC7",                ScratchImage.DXGIFormat.BC7UNORM  },
            { "BC7_SRGB",           ScratchImage.DXGIFormat.BC7UNORM  },
            { "BC6_UH",             ScratchImage.DXGIFormat.BC6HUF16  },
            { "BC6_SH",             ScratchImage.DXGIFormat.BC6HSF16  },
            { "R9G9B9E5",           ScratchImage.DXGIFormat.R9G9B9E5SHAREDEXP  },
            { "R16G16B16A16F",      ScratchImage.DXGIFormat.R16G16B16A16FLOAT  },
            { "R16F",               ScratchImage.DXGIFormat.R16FLOAT  },
            { "R8G8B8A8_SRGB",      ScratchImage.DXGIFormat.R8G8B8A8UNORM  },
            { "A8R8G8B8_SRGB",      ScratchImage.DXGIFormat.R8G8B8A8UNORM  },
            { "R8G8B8A8",           ScratchImage.DXGIFormat.R8G8B8A8UNORM  },
            { "R8_UN",              ScratchImage.DXGIFormat.R8UNORM  },
        };

        /// <summary>
        /// Gets or Sets the Package Entries
        /// </summary>
        public Dictionary<string, PackageEntry> Entries { get; set; }

        /// <summary>
        /// Disposes of the Package
        /// </summary>
        public void Dispose()
        {
            Reader?.Dispose();
        }

        /// <summary>
        /// Extracts the given entry from the package
        /// </summary>
        /// <param name="entry">Entry to extract</param>
        /// <param name="size">Output size of the package, in some cases, it is acceptable to pass a higher value or -1 and allow retrieval from the entry data.</param>
        public unsafe byte[] Extract(PackageEntry entry, int size)
        {
            if (!(entry is XPAKPackageEntry xpakEntry))
                return null;

            // Read the entire buffer, and then process
            byte[] buffer = new byte[xpakEntry.Size];

            lock (Reader)
            {
                Reader.BaseStream.Position = xpakEntry.Offset;
                Reader.BaseStream.Read(buffer, 0, buffer.Length);
            }

            using (var chunkReader = new BinaryReader(new MemoryStream(buffer)))
            {
                var remaining = size == -1 ? (int)xpakEntry.GetKVPNumber("size0") : size;
                byte[] output = new byte[remaining];

                while (chunkReader.BaseStream.Position < chunkReader.BaseStream.Length)
                {
                    var header = chunkReader.ReadStruct<DataChunkHeader>();

                    if (header.Count > 30)
                        throw new Exception("Invalid XPAK Data Chunk Header");

                    var offset = header.Offset;

                    for (int i = 0; i < header.Count; i++)
                    {
                        var chunkSize = header.Commands[i] & 0xFFFFFF;
                        var chunkComp = header.Commands[i] >> 24;

                        if (chunkComp == 0x3)
                        {
                            var rawSize = remaining < 262112 ? remaining : 262112;
                            var decompressed = LZ4.Decompress(chunkReader.ReadBytes(chunkSize), rawSize);
                            Buffer.BlockCopy(decompressed, 0, output, offset, decompressed.Length);
                            remaining -= decompressed.Length;
                            offset += decompressed.Length;
                        }
                        else if (chunkComp == 0x6)
                        {
                            var rawSize = remaining < 262112 ? remaining : 262112;
                            Buffer.BlockCopy(Oodle.Decompress(chunkReader.ReadBytes(chunkSize), rawSize), 0, output, offset, rawSize);
                            remaining -= rawSize;
                            offset += rawSize;
                        }
                        else if (chunkComp == 0x0)
                        {
                            chunkReader.BaseStream.Read(output, offset, chunkSize);
                            remaining -= chunkSize;
                            offset += chunkSize;
                        }
                        else
                        {
                            chunkReader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                        }
                    }

                    chunkReader.BaseStream.Position = (chunkReader.BaseStream.Position + 0x7F) & 0xFFFFFF80;
                }

                return output;
            }
        }

        /// <summary>
        /// Loads data from the Package
        /// </summary>
        /// <param name="stream">Stream to load</param>
        public unsafe void Load(Stream stream, string name)
        {
            Entries = new Dictionary<string, PackageEntry>();

            Reader?.Dispose();
            Reader = new BinaryReader(stream);
            Reader.BaseStream.Position = 0;
            var header = Reader.ReadStruct<Header>();

            if(header.MajorVersion == 0xD)
            {
                Reader.BaseStream.Position = 0;

                var headerBuffer = new byte[sizeof(Header)];

                Reader.Read(headerBuffer, 0, 24);
                Reader.BaseStream.Position += 288;
                Reader.Read(headerBuffer, 24, 96);

                fixed (byte* p = headerBuffer)
                    header = *(Header*)p;
            }


            Reader.BaseStream.Position = header.IndexOffset;

            var indexTable = new Dictionary<ulong, IndexEntry>();

            foreach (var index in Reader.ReadArray<IndexEntry>((int)header.IndexCount))
                indexTable[index.Key] = index;

            Reader.BaseStream.Position = header.MetaOffset;

            Game = "Call of Duty Black Ops III";

            for (int i = 0; i < header.MetaCount; i++)
            {
                var key = Reader.ReadUInt64();
                var size = Reader.ReadInt64();
                var buf = Reader.ReadBytes((int)size);

                if (indexTable.TryGetValue(key, out var index))
                {
                    var x = new XPAKPackageEntry()
                    {
                        Key = key,
                        KVPBuffer = buf,
                        Size = index.Size & 0xFFFFFFFFFFFFFFF,
                        Offset = index.Offset + header.DataOffset,
                        BasePackage = this
                    };

                    Entries.Add(key.ToString(), x);
                }
            }
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
            return new XPAKPackage();
        }
    }

    /// <summary>
    /// A class for XPAK Package Entries
    /// </summary>
    class XPAKPackageEntry : PackageEntry
    {
        /// <summary>
        /// Gets the Name of the Package Entry
        /// </summary>
        public override string Name
        {
            get
            {
                return GetKVPString("name");
            }
            set
            {
                throw new NotImplementedException("Setting the name of an XPAK Entry is not supported");
            }
        }

        /// <summary>
        /// Gets the Name of the Package Entry
        /// </summary>
        public string Type => GetKVPString("type");

        /// <summary>
        /// Gets or Sets the Hash Key
        /// </summary>
        public ulong Key { get; set; }

        /// <summary>
        /// Gets or Sets the total size of the data
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets the offset to the data
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// Gets or Set the KVP Buffer
        /// </summary>
        public byte[] KVPBuffer { get; set; }

        /// <summary>
        /// Gets or Set the KVP List
        /// </summary>
        public Dictionary<string, string> KVPs { get; set; }

        /// <summary>
        /// Gets a KVP by key
        /// </summary>
        /// <param name="k">Key to get</param>
        /// <returns>Resulting Value</returns>
        public string GetKVPString(string k)
        {
            if (KVPs == null)
                BuildKVPs();

            return KVPs.TryGetValue(k, out var r) ? r : "";
        }

        /// <summary>
        /// Gets a KVP by key
        /// </summary>
        /// <param name="k">Key to get</param>
        /// <returns>Resulting Value</returns>
        public double GetKVPNumber(string k)
        {
            if (KVPs == null)
                BuildKVPs();

            return KVPs.TryGetValue(k, out var r) ? (double.TryParse(r, out var f) ? f : -1) : -1;
        }

        /// <summary>
        /// Builds the KVP Buffer
        /// </summary>
        private void BuildKVPs()
        {
            KVPs = new Dictionary<string, string>();
            var split = Encoding.ASCII.GetString(KVPBuffer).Split('\n');

            foreach (var line in split)
            {
                var lineSplit = line.Split(':');

                if (lineSplit.Length <= 1)
                    continue;

                KVPs[lineSplit[0].Trim()] = lineSplit[1].Trim();
            }
        }
    }
}
