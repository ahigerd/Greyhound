using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PhilLibX.Compression;
using PhilLibX.Imaging;
using PhilLibX.IO;

namespace Greyhound.Logic
{
    /// <summary>
    /// A class to handle loading an FF from MW4
    /// </summary>
    public class MW4FFPackage : IPackage
    {
        /// <summary>
        /// Image Mip Map
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct MipMap
        {
            public ulong Hash;
            public fixed byte Padding[24];
            public uint PixelSize;
            public ushort Width;
            public ushort Height;
        }

        /// <summary>
        /// Image Asset
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct ImageHeader
        {
            public long NamePointer;
            public fixed byte Unk00[12];
            public int ImageFormat;
            public int Unk01;
            public int BufferSize;
            public int Unk02;
            public ushort Width;
            public ushort Height;
            public ushort Depth;
            public ushort Levels;
            public int Unk03;
            public ushort Unk04;
            public ushort StreamedMipCount;
            public int Unk05;
            public fixed byte MipLevels[160];
            public long PrimedMipPointer;
            public long LoadedImagePointer;
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly ScratchImage.DXGIFormat[] Formats =
        {
            ScratchImage.DXGIFormat.FORCEUINT,
            ScratchImage.DXGIFormat.R8UNORM,
            ScratchImage.DXGIFormat.A8UNORM,
            ScratchImage.DXGIFormat.A8UNORM,
            ScratchImage.DXGIFormat.R8G8UNORM,
            ScratchImage.DXGIFormat.R8G8UNORM,
            ScratchImage.DXGIFormat.R8G8B8A8UNORM,
            ScratchImage.DXGIFormat.R8G8B8A8UNORM,
            ScratchImage.DXGIFormat.R8SNORM,
            ScratchImage.DXGIFormat.R8G8SNORM,
            ScratchImage.DXGIFormat.R16UNORM,
            ScratchImage.DXGIFormat.R16G16UNORM,
            ScratchImage.DXGIFormat.R16G16B16A16UNORM,
            ScratchImage.DXGIFormat.R16SNORM,
            ScratchImage.DXGIFormat.R16FLOAT,
            ScratchImage.DXGIFormat.R16G16FLOAT,
            ScratchImage.DXGIFormat.R16G16B16A16FLOAT,
            ScratchImage.DXGIFormat.R32FLOAT,
            ScratchImage.DXGIFormat.R32G32FLOAT,
            ScratchImage.DXGIFormat.R32G32B32A32FLOAT,
            ScratchImage.DXGIFormat.D32FLOAT,
            ScratchImage.DXGIFormat.D32FLOATS8X24UINT,
            ScratchImage.DXGIFormat.R8UINT,
            ScratchImage.DXGIFormat.R16UINT,
            ScratchImage.DXGIFormat.R32UINT,
            ScratchImage.DXGIFormat.R32G32UINT,
            ScratchImage.DXGIFormat.R32G32B32A32UINT,
            ScratchImage.DXGIFormat.R10G10B10A2UINT,
            ScratchImage.DXGIFormat.B5G6R5UNORM,
            ScratchImage.DXGIFormat.B5G6R5UNORM,
            ScratchImage.DXGIFormat.R10G10B10A2UNORM,
            ScratchImage.DXGIFormat.R9G9B9E5SHAREDEXP,
            ScratchImage.DXGIFormat.R11G11B10FLOAT,
            ScratchImage.DXGIFormat.BC1UNORM,
            ScratchImage.DXGIFormat.BC1UNORM,
            ScratchImage.DXGIFormat.BC2UNORM,
            ScratchImage.DXGIFormat.BC2UNORM,
            ScratchImage.DXGIFormat.BC3UNORM,
            ScratchImage.DXGIFormat.BC3UNORM,
            ScratchImage.DXGIFormat.BC4UNORM,
            ScratchImage.DXGIFormat.BC5UNORM,
            ScratchImage.DXGIFormat.BC5SNORM,
            ScratchImage.DXGIFormat.BC6HUF16,
            ScratchImage.DXGIFormat.BC6HSF16,
            ScratchImage.DXGIFormat.BC7UNORM,
            ScratchImage.DXGIFormat.BC7UNORM,
            ScratchImage.DXGIFormat.R8G8B8A8SNORM
        };

        /// <summary>
        /// Gets or Sets the Reader
        /// </summary>
        private BinaryReader Reader { get; set; }

        /// <summary>
        /// Gets the Package Name
        /// </summary>
        public string Name { get; } = "MW4 FF";

        /// <summary>
        /// Gets the Game this Package is from
        /// </summary>
        public string Game { get; set; }

        /// <summary>
        /// Gets or Sets the File Name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or Sets the Temp File Path
        /// </summary>
        public string TempFilePath { get; set; }

        /// <summary>
        /// Gets or Sets the Package Extensions
        /// </summary>
        public string[] Extensions { get; } = new string[]
        {
            ".ff",
        };

        /// <summary>
        /// Gets assets that can be exported directly from the package
        /// </summary>
        public List<Asset> Assets
        {
            get
            {
                var results = new List<Asset>(Entries.Count);

                foreach (var entry in Entries)
                {
                    if(entry.Value is MW4FFPackageEntry ffEntry)
                    {
                        var header = (ImageHeader)ffEntry.AssetHeader;

                        results.Add(new ImageAsset()
                        {
                            Name        = ffEntry.Name,
                            Type        = ffEntry.Type,
                            Width       = header.Width,
                            Height      = header.Height,
                            Format      = Formats[header.ImageFormat],
                            CubeMap     = false, // MW does not use cube maps, it uses octahedron for reflections/sky
                            Information = string.Format("Width: {0} Height: {1}", header.Width, header.Height),
                            Status      = "Loaded",
                            Data        = ffEntry,
                            Game        = Game,
                            LoadMethod  = ImageHelper.LoadGenericImage,
                        });
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
            Reader?.Dispose();
        }

        /// <summary>
        /// Loads data from the Package
        /// </summary>
        /// <param name="stream">Stream to load</param>
        public byte[] Extract(PackageEntry entry, int size)
        {
            if(entry is MW4FFPackageEntry ffEntry)
            {
                // Read the entire buffer, and then process
                byte[] buffer = new byte[ffEntry.Size];

                lock (Reader)
                {
                    Reader.BaseStream.Position = ffEntry.DataOffset;
                    Reader.BaseStream.Read(buffer, 0, buffer.Length);
                }

                return buffer;
            }

            return null;
        }

        /// <summary>
        /// Loads data from the Package
        /// </summary>
        /// <param name="stream">Stream to load</param>
        public unsafe void Load(Stream stream, string name)
        {
            Entries = new Dictionary<string, PackageEntry>();

            stream.Position = 0x88;

            using (var reader = new BinaryReader(RemoveHashBlocks(stream, 0x8000, 0x800000, 0x4000)))
            {
                reader.BaseStream.Position = 4;
                var decompressedSize = reader.ReadInt32();
                var blockCount = (decompressedSize >> 16) + 1;
                reader.BaseStream.Position += 4;

                TempFilePath = Path.Combine("Temp", Path.GetFileNameWithoutExtension(name) + ".temp");

                using (var writer = File.Create(TempFilePath))
                {
                    for (int i = 0; i < blockCount; i++)
                    {
                        var blockSize = reader.ReadInt32();
                        var rawSize = reader.ReadInt32();
                        reader.BaseStream.Position += 4;

                        var buffer = Oodle.Decompress(reader.ReadBytes(blockSize), rawSize);

                        writer.Write(buffer, 0, buffer.Length);

                        reader.BaseStream.Position += ((blockSize + 0x3) & 0xFFFFFFFFFFFFFFC) - blockSize;
                    }
                }

                Reader = new BinaryReader(File.OpenRead(TempFilePath));
            }

            Game = "Call of Duty Modern Warfare 2019";

            Reader.BaseStream.Position = 0;

            // Skip FF String Pointers
            var stringCount = Reader.ReadInt32();
            Reader.BaseStream.Position = (0x28 + (8 * stringCount));

            foreach (var offset in Reader.FindBytes(new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }))
            {
                try
                {
                    Reader.BaseStream.Position = offset;

                    var imageHeader = Reader.ReadStruct<ImageHeader>();

                    // All of the streamed mips should be 0 for a loaded image
                    if (!AWFFPackage.BufferIsNull(imageHeader.MipLevels, 160))
                        continue;

                    // Check name pointer and size (if > 32MB or 0 don't even bother)
                    if (
                        imageHeader.ImageFormat > 0 &&
                        imageHeader.ImageFormat < Formats.Length &&
                        imageHeader.BufferSize < 0x2000000 &&
                        imageHeader.BufferSize != 0 && 
                        imageHeader.LoadedImagePointer == -2 &&
                        imageHeader.PrimedMipPointer == 0)
                    {
                        var imgName = string.Format("gfximage_{0:X}", offset);

                        // Read Name and Buffer
                        if (imageHeader.NamePointer == -2)
                            imgName = Reader.ReadNullTerminatedString();

                        Entries[imgName] = new MW4FFPackageEntry(
                            Path.GetFileNameWithoutExtension(imgName).Replace("*", ""),
                            "image",
                            string.Format("Width: {0} Height: {1}", imageHeader.Width.ToString(), imageHeader.Height.ToString()),
                            imageHeader.BufferSize,
                            imageHeader,
                            Reader.BaseStream.Position,
                            this);
                    }
                }
                catch { }
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
            if (!Extensions.Contains(extension))
                return false;

            // Get magic and version for further checks since multiple package class are FF
            // We check multiple versions as AW covers AW, MWR, and MW2R
            var magic = BitConverter.ToUInt64(initialBuffer, 0);
            var version = BitConverter.ToUInt32(initialBuffer, 8);

            if (magic != 0x3030316166665749)
                return false;
            if (version != 0xB)
                return false;

            return true;
        }

        /// <summary>
        /// Clones the Package
        /// </summary>
        /// <returns>Cloned Package</returns>
        public IPackage CreateNew()
        {
            return new MW4FFPackage();
        }

        /// <summary>
        /// Removes hash blocks from the given fast file
        /// </summary>
        public static Stream RemoveHashBlocks(Stream input, int initialSize, int blockSize, int hashSize, bool isSigned = true)
        {
            // Override for files with specific indicators
            if (!isSigned)
                return input;

            MemoryStream output = new MemoryStream((int)input.Length);

            input.Position += initialSize;

            byte[] buffer = new byte[blockSize];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
                input.Seek(hashSize, SeekOrigin.Current);
            }

            output.Flush();
            output.Position = 0;

            return output;
        }
    }

    /// <summary>
    /// A class for FF from MW4/ZIP Package Entries
    /// </summary>
    class MW4FFPackageEntry : PackageEntry
    {
        /// <summary>
        /// Gets or Sets the Name of the Package Entry
        /// </summary>
        public override string Name { get; set; }

        /// <summary>
        /// Gets or Sets the Entry Type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or Sets the Entry Info
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// Gets or Sets the Size of the Data
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or Sets the Offset within the Package of the data
        /// </summary>
        public long DataOffset { get; set; }

        /// <summary>
        /// Gets or Sets the Asset Header
        /// </summary>
        public object AssetHeader { get; set; }

        /// <summary>
        /// Creates a new Package Entry
        /// </summary>
        public MW4FFPackageEntry(string name, string type, string info, int size, object header, long dataOffset, IPackage package)
        {
            Name = name;
            Type = type;
            Info = info;
            Size = size;
            AssetHeader = header;
            DataOffset = dataOffset;
            BasePackage = package;
        }
    }
}
