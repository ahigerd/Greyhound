using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Greyhound.Logic
{
    /// <summary>
    /// A class for Game Packages
    /// </summary>
    public interface IPackage : IDisposable
    {
        /// <summary>
        /// Gets or Sets the Package Name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or Sets the Package Extensions
        /// </summary>
        string[] Extensions { get; }

        /// <summary>
        /// Gets or Sets the File Name
        /// </summary>
        string FileName { get; set; }

        /// <summary>
        /// Gets assets that can be exported directly from the package
        /// </summary>
        List<Asset> Assets { get; }

        /// <summary>
        /// Gets or Sets the Package Entries
        /// </summary>
        Dictionary<string, PackageEntry> Entries { get; set; }

        /// <summary>
        /// Extracts the given entry from the package
        /// </summary>
        /// <param name="entry">Entry to extract</param>
        /// <param name="size">Output size of the package, in some cases, it is acceptable to pass a higher value or -1 and allow retrieval from the entry data.</param>
        byte[] Extract(PackageEntry entry, int size);

        /// <summary>
        /// Checks if the given file is valid for this package type
        /// </summary>
        /// <param name="stream">Stream to load</param>
        /// <param name="initialBuffer">16-Bytes at the start of the file for further verification</param>
        /// <param name="extension">File extension</param>
        /// <returns>True if valid for this package, otherwise false</returns>
        bool IsFileValid(Stream stream, byte[] initialBuffer, string extension);

        /// <summary>
        /// Loads data from the Package
        /// </summary>
        /// <param name="stream">Stream to load</param>
        /// <param name="name">Package Name</param>
        void Load(Stream stream, string name);

        /// <summary>
        /// Creates a new instance of this package type
        /// </summary>
        /// <returns>Cloned Package</returns>
        IPackage CreateNew();
    }

    /// <summary>
    /// A class for Game Package Entries
    /// </summary>
    public abstract class PackageEntry
    {
        /// <summary>
        /// Gets the Name of the Package Entry
        /// </summary>
        public abstract string Name { get; set; }

        /// <summary>
        /// Gets or Sets the Package this entry belongs to
        /// </summary>
        public IPackage BasePackage { get; set; }
    }
}
