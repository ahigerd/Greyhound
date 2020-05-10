using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class AWFFPackage : IPackage
    {
        #region Structures
        /// <summary>
        /// Image Asset
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ImageHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x18)]
            public byte[] Padding;
            public ScratchImage.DXGIFormat ImageFormat;
            public byte MapType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
            public byte[] Padding3;
            public int DataSize;
            public int BufferSize;
            public short Width;
            public short Height;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Padding4;
            public ulong DataPointer;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Padding5;
            public ulong NamePointer;
        }
        #endregion

        /// <summary>
        /// DXGI Formats
        /// </summary>
        private readonly Dictionary<int, ScratchImage.DXGIFormat> Formats = new Dictionary<int, ScratchImage.DXGIFormat>()
        {
            { 7,  ScratchImage.DXGIFormat.R8G8B8A8UNORM },
            { 6,  ScratchImage.DXGIFormat.R8G8B8G8UNORM },
            { 33, ScratchImage.DXGIFormat.BC1UNORM },
            { 34, ScratchImage.DXGIFormat.BC1UNORM },
            { 37, ScratchImage.DXGIFormat.BC3UNORM },
            { 38, ScratchImage.DXGIFormat.BC3UNORM },
            { 39, ScratchImage.DXGIFormat.BC4UNORM },
            { 40, ScratchImage.DXGIFormat.BC5UNORM },
            { 42, ScratchImage.DXGIFormat.BC6HUF16 },
            { 44, ScratchImage.DXGIFormat.BC7UNORM },
            { 45, ScratchImage.DXGIFormat.BC7UNORM },
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
                var results = new List<Asset>(Entries.Count);

                foreach (var entry in Entries)
                {
                    var ffEntry = entry.Value as FFPackageEntry;

                    if(ffEntry.Type == "image")
                    {
                        var header = (ImageHeader)ffEntry.AssetHeader;

                        results.Add(new ImageAsset()
                        {
                            Name        = ffEntry.Name,
                            Type        = ffEntry.Type,
                            Width       = header.Width,
                            Height      = header.Height,
                            Format      = header.ImageFormat,
                            CubeMap     = header.MapType == 0x5,
                            Information = string.Format("Width: {0} Height: {1}", header.Width, header.Height),
                            Status      = "Loaded",
                            Data        = ffEntry,
                            Game        = Game,
                            LoadMethod  = LoadFFImage
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
            if(entry is FFPackageEntry ffEntry)
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
        /// Checks if the buffer is null
        /// </summary>
        public static unsafe bool BufferIsNull(byte* buffer, int size)
        {
            for (int i = 0; i < size; i++)
                if (buffer[i] != 0)
                    return false;

            return true;
        }

        /// <summary>
        /// Loads data from the Package
        /// </summary>
        /// <param name="stream">Stream to load</param>
        public unsafe void Load(Stream stream, string name)
        {
            Entries = new Dictionary<string, PackageEntry>();

            // We need the magic to check if this is signed
            var magicBuffer = new byte[12];
            stream.Read(magicBuffer, 0, 12);
            var version = BitConverter.ToUInt32(magicBuffer, 8);
            var isSigned = BitConverter.ToUInt64(magicBuffer, 0) == 0x3030313066663153;

            stream.Position = 24;
            var imageCountBuffer = new byte[4];
            stream.Read(imageCountBuffer, 0, 4);

            // Skip image blocks
            stream.Position += (24 * BitConverter.ToInt32(imageCountBuffer, 0)) + 16;

            using (var reader = new BinaryReader(RemoveHashBlocks(stream, 0x8000, 0x800000, 0x4000, isSigned)))
            {
                var decompressedSize = reader.ReadInt64();
                var blockCount = (decompressedSize >> 16) + 1;
                reader.BaseStream.Position += 4;

                TempFilePath = Path.Combine("Temp", Path.GetFileNameWithoutExtension(name) + ".temp");
                Directory.CreateDirectory("Temp");

                // Raw buffer for decompressing

                using (var writer = File.Create(TempFilePath))
                {
                    for (int i = 0; i < blockCount; i++)
                    {
                        var blockSize = reader.ReadInt32();
                        var rawSize = reader.ReadInt32();
                        var buffer = new byte[blockSize];
                        reader.BaseStream.Read(buffer, 0, blockSize);

                        writer.Write(LZ4.Decompress(buffer, rawSize), 0, rawSize);

                        reader.BaseStream.Position += ((blockSize + 0x3) & 0xFFFFFFFFFFFFFFC) - blockSize;
                    }
                }

                Reader = new BinaryReader(File.OpenRead(TempFilePath));
            }

            if (version == 0x82)
                Game = "Call of Duty Modern Warfare 2 Remastered";
            else if(version == 0x42)
                Game = "Call of Duty Modern Warfare Remastered";
            else
                Game = "Call of Duty Advanced Warfare";


            Reader.BaseStream.Position = 72;

            // Skip FF String Pointers
            var stringCount = Reader.ReadInt32();
            Reader.BaseStream.Position = (112 + (8 * stringCount));

            var allThingThatHurtsMyBrain = Stopwatch.StartNew();

            foreach (var offset in Reader.FindBytes(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFD, 0xFD, 0xFD }))
            {
                try
                {
                    Reader.BaseStream.Position = offset - 96;

                    var imageHeader = Reader.ReadStruct<ImageHeader>();

                    // Check name pointer and size (if > 32MB or 0 don't even bother)
                    if (
                        imageHeader.DataSize < 0x3000000 &&
                        imageHeader.DataSize != 0 &&
                        imageHeader.DataPointer == 0xFDFDFDFFFFFFFFFE &&
                        imageHeader.Width < 8192 && imageHeader.Height < 8192)
                    {
                        var imgName = string.Format("gfximage_{0:X}", offset);

                        // Read Name and Buffer
                        if (imageHeader.NamePointer == 0xFDFDFDFFFFFFFFFF)
                            imgName = Reader.ReadNullTerminatedString();

                        Entries[imgName] = new FFPackageEntry(Path.GetFileNameWithoutExtension(imgName.Replace("*", "")), "image", imageHeader.BufferSize, imageHeader, Reader.BaseStream.Position, this);
                    }
                }
                catch { }
            }

            Console.WriteLine(allThingThatHurtsMyBrain.ElapsedMilliseconds / 1000.0f);
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

            if (magic != 0x3030313066663153 && magic != 0x3030317566663153)
                return false;
            if (version != 0x72E && version != 0x42 && version != 0x82)
                return false;

            return true;
        }

        /// <summary>
        /// Clones the Package
        /// </summary>
        /// <returns>Cloned Package</returns>
        public IPackage CreateNew()
        {
            return new AWFFPackage();
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
            input.Dispose();

            return output;
        }

        public void LoadFFImage(Asset asset)
        {
            var image = asset as ImageAsset;
            var entry = image.Data as FFPackageEntry;

            var buffer = entry.BasePackage.Extract(entry, -1);

            if (buffer == null)
                return;

            image.RawImage = ImageHelper.ConvertRawImage(buffer, image.Width, image.Height, image.Format, 0, image.CubeMap, null);
        }
    }

    /// <summary>
    /// A class for FF from MW4/ZIP Package Entries
    /// </summary>
    class FFPackageEntry : PackageEntry
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
        public FFPackageEntry(string name, string type, int size, object header, long dataOffset, IPackage package)
        {
            Name = name;
            Type = type;
            Size = size;
            AssetHeader = header;
            DataOffset = dataOffset;
            BasePackage = package;
        }
    }
}
