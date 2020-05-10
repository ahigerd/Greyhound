using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhilLibX.Imaging;
using PhilLibX.IO;

namespace Greyhound.Logic
{
    public static class ImageHelper
    {
        /// <summary>
        /// Supported IWI Versions
        /// </summary>
        private enum IWIVersion : byte
        {
            CoD2    = 0x5,
            CoD45   = 0x6,
            CoDMW23 = 0x8,
            CoDBo1  = 0xD,
            CoDBo2  = 0x1B,
        }

        /// <summary>
        /// Supported IWI Formats
        /// </summary>
        private enum IWIFormat : byte
        {
            Invalid = 0x0,
            RGBA    = 0x1,
            RGB     = 0x2,
            D16     = 0x3,
            LA8     = 0x4,
            A8      = 0x5,
            DXT1    = 0xB,
            DXT3    = 0xC,
            DXT5    = 0xD,
            BC5     = 0xE,
            RGBA16  = 0x13,
        }

        /// <summary>
        /// Supported IWI Flags
        /// </summary>
        private enum IWIFlags : byte
        {
            None = 0x0,
            Cube = 0x4,
        }

        /// <summary>
        /// 
        /// </summary>
        public static string[] Extensions =
        {
            "dds",
            "png",
            "tiff",
            "jpg",
            "bmp",
            "tga",
        };

        /// <summary>
        /// Gets all export paths by extension for the given path, if none are returned, then there are no images to export for this path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>Image formats that require exporting along with the last image enabled image</returns>
        public static Tuple<string, List<string>> GetExportPaths(string path)
        {
            var paths = new List<string>();
            var ext = "";

            // Determine if we should even bother loading
            foreach (var format in Extensions)
            {
                // PNG is default
                if (Instance.GetSettings["Export" + format.ToUpper(), format == "png" ? "Yes" : "No"] == "Yes")
                {
                    // Set our path to the last exported type
                    var result = path + "." + format;

                    // Add to list
                    ext = format;

                    if (Instance.SkipExistingImages == true)
                        if (File.Exists(result))
                            continue;
                    
                    paths.Add(result);
                }
            }

            return new Tuple<string, List<string>>(ext, paths);
        }

        /// <summary>
        /// Gets all export paths by extension for the given path, if none are returned, then there are no images to export for this path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>Image formats that require exporting</returns>
        public static string GetModelImagePath(string path)
        {
            foreach (var format in Extensions)
            {
                var result = path + "." + format;
                
                if (File.Exists(result))
                    return result;
            }

            return "";
        }

        public static void SaveScratchImage(ScratchImage image, string semantic, List<string> paths)
        {
            if (image == null)
                return;

            if(semantic == "normalmap")
            {
                SaveXYNormalMapScratchImage(image, paths);
            }
            else
            {
                foreach (var path in paths)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    // Everything besides DDS we need to decompress & convert
                    if (Path.GetExtension(path) != ".dds")
                        image.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                    image.Save(path);
                }
            }
        }

        /// <summary>
        /// Clamps the value to the given range
        /// </summary>
        public static T Clamp<T>(T value, T max, T min) where T : IComparable<T>
        {
            return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
        }

        /// <summary>
        /// Calculates the Z value based off the unit vector's X and Y values
        /// </summary>
        public static byte CalculateBlueValue(int R, int G)
        {
            double X = 2.0 * (R / 255.0000) - 1;
            double Y = 2.0 * (G / 255.0000) - 1;
            double Z = 0.0000000;

            if ((1 - (X * X) - (Y * Y)) > 0)
                Z = Math.Sqrt(1 - (X * X) - (Y * Y));

            return (byte)(Clamp((Z + 1.0) / 2.0, 1.0, 0.0) * 255);
        }

        public unsafe static void SaveXYNormalMapScratchImage(ScratchImage image, List<string> paths)
        {
            // Force the image to a standard format
            image.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

            var inputPixels = image.GetPixels();
            var outputPixels = new byte[inputPixels.Length];

            fixed(byte* a = inputPixels)
            fixed(byte* b = outputPixels)
            {
                for (int i = 0; i < inputPixels.Length; i += 4)
                {
                    b[i + 0] = a[i + 0];
                    b[i + 1] = a[i + 1];
                    b[i + 2] = CalculateBlueValue(a[i + 0], a[i + 1]);
                    b[i + 3] = 0xFF;
                }
            }

            using (var output = new ScratchImage(image.Metadata, outputPixels))
            {
                foreach (var path in paths)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    output.Save(path);
                }
            }
        }

        public static void LoadGenericImage(Asset asset)
        {
            var image = asset as ImageAsset;
            var buffer = Instance.ExtractPackageEntry(image.Data, -1);

            if (buffer == null)
                return;

            image.RawImage = ImageHelper.ConvertRawImage(buffer, image.Width, image.Height, image.Format, 0, image.CubeMap, null);
        }

        /// <summary>
        /// Converts a DDS to a Scratch Image
        /// </summary>
        public static ScratchImage ConvertDDS(byte[] buffer)
        {
            return new ScratchImage(buffer, ScratchImage.ImageFormat.DDS);
        }

        public static ScratchImage ConvertRawImage(byte[] buffer, int width, int height, ScratchImage.DXGIFormat format, int mips, bool cubeMap, string semantic)
        {
            return new ScratchImage(new ScratchImage.TexMetadata()
            {
                Width      = width,
                Height     = height,
                Depth      = 1,
                ArraySize  = cubeMap ? 6 : 1,
                MiscFlags  = cubeMap ? ScratchImage.TexMiscFlags.TEXTURECUBE : ScratchImage.TexMiscFlags.NONE,
                MiscFlags2 = ScratchImage.TexMiscFlags2.NONE,
                Dimension  = ScratchImage.TexDimension.TEXTURE2D,
                MipLevels  = 1,
                Format     = format
            }, buffer);
        }

        /// <summary>
        /// Attempts to resolve the mip map offsets and sizes for IWI
        /// </summary>
        /// <param path="offsets">Offsets to Mip Maps</param>
        /// <param path="firstMipOffset">Offset of the First Mip Map</param>
        /// <param path="size">Size of the entire IWI file</param>
        /// <returns>A list of tuples containing the offsets and sizes (Offset, Size)</returns>
        private static List<Tuple<int, int>> GetIWIMipMapSizes(int[] offsets, int firstMipOffset, int size)
        {
            List<Tuple<int, int>> results = new List<Tuple<int, int>>(offsets.Length);

            for(int i = 0; i < offsets.Length; i++)
            {
                if (i == 0)
                {
                    results.Add(new Tuple<int, int>(offsets[i], size - offsets[i]));
                }
                else if(i == offsets.Length - 1)
                {
                    results.Add(new Tuple<int, int>(firstMipOffset, offsets[i] - firstMipOffset));
                }
                else
                {
                    results.Add(new Tuple<int, int>(offsets[i], offsets[i - 1] - offsets[i]));
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the DXGI Format based off the IWI Format
        /// </summary>
        /// <param path="format">IWI Format</param>
        /// <returns>Resulting DXGI Format</returns>
        private static ScratchImage.DXGIFormat GetDXGIFormat(IWIFormat format)
        {
            switch(format)
            {
                case IWIFormat.RGBA:    return ScratchImage.DXGIFormat.R8G8B8A8UNORM;
                case IWIFormat.D16:     return ScratchImage.DXGIFormat.D16UNORM;
                case IWIFormat.LA8:     return ScratchImage.DXGIFormat.A8UNORM;
                case IWIFormat.A8:      return ScratchImage.DXGIFormat.A8UNORM;
                case IWIFormat.DXT1:    return ScratchImage.DXGIFormat.BC1UNORM;
                case IWIFormat.DXT3:    return ScratchImage.DXGIFormat.BC2UNORM;
                case IWIFormat.DXT5:    return ScratchImage.DXGIFormat.BC3UNORM;
                case IWIFormat.BC5:     return ScratchImage.DXGIFormat.BC5UNORM;
                case IWIFormat.RGBA16:  return ScratchImage.DXGIFormat.R16G16B16A16FLOAT;
            }

            // Failed
            return ScratchImage.DXGIFormat.FORCEUINT;
        }

        /// <summary>
        /// Converts an IWI to a DirectX Scratch Image
        /// </summary>
        /// <param path="stream">Stream containing Data</param>
        public static ScratchImage ConvertIWI(byte[] buffer)
        {
            using (var reader = new BinaryReader(new MemoryStream(buffer)))
            {
                var magic = reader.ReadBytes(4);

                // Validate the magic, we're expecting 'IWi'
                if (magic[0] != 0x49 && magic[1] != 0x57 && magic[2] != 0x69)
                    throw new Exception("IWI has invalid magic, expecting 'IWi'");

                // Store info here, since IWI's are different from game to game
                int[] offsets;

                var game = (IWIVersion)magic[3];

                if (!Enum.IsDefined(typeof(IWIVersion), game))
                    throw new Exception(string.Format("IWI version: 0x{0:X} is not supported", magic[3]));

                // We need to skip ahead for MW2 and MW3
                if (game == IWIVersion.CoDMW23)
                    reader.BaseStream.Position = 0x8;

                var iwiFormat = (IWIFormat)reader.ReadByte();
                var format    = GetDXGIFormat(iwiFormat);
                byte flags    = reader.ReadByte();
                ushort width  = reader.ReadUInt16();
                ushort height = reader.ReadUInt16();
                ushort depth  = reader.ReadUInt16();

                // Bo1 and Bo2 have more mips, along with them being further ahead
                if (game == IWIVersion.CoDBo1 || game == IWIVersion.CoDBo2)
                {
                    reader.BaseStream.Position = (game == IWIVersion.CoDBo1 ? 0x10 : 0x20);
                    offsets = reader.ReadArray<int>(8);
                }
                else
                {
                    offsets = reader.ReadArray<int>(4);
                }

                // Attempt to resolve mip and get the highest by size
                var mipMap = GetIWIMipMapSizes(offsets, (int)reader.BaseStream.Position, (int)reader.BaseStream.Length).OrderByDescending(x => x.Item2).ToArray();
                reader.BaseStream.Position = mipMap[0].Item1;

                byte[] data;

                // Check do we need to perform conversion for images DXTex does not support
                switch (iwiFormat)
                {
                    //case IWIFormat.RGB:
                    //    format = ScratchImage.DXGIFormat.R8G8B8A8UNORM;
                    //    data = ExpandRGB(reader.ReadBytes(mipMap[0].Item2), width * height);
                    //    break;
                    default:
                        data = reader.ReadBytes(mipMap[0].Item2);
                        break;
                }

                // We need to check that we support this format
                if (format != ScratchImage.DXGIFormat.FORCEUINT)
                {
                    return new ScratchImage(new ScratchImage.TexMetadata()
                    {
                        Width      = width,
                        Height     = height,
                        Depth      = depth,
                        ArraySize  = 1,
                        MipLevels  = 1,
                        MiscFlags  = ScratchImage.TexMiscFlags.NONE,
                        MiscFlags2 = ScratchImage.TexMiscFlags2.NONE,
                        Format     = format,
                        Dimension  = ScratchImage.TexDimension.TEXTURE2D
                    }, data);
                }
                else
                {
                    throw new Exception(string.Format("IWI format: 0x{0:X} is not supported", iwiFormat));
                }
            }
        }
    }
}
