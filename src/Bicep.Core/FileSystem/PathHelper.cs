// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.IO;

namespace Bicep.Core.FileSystem
{
    public static class PathHelper
    {
        private static readonly bool IsFileSystemCaseSensitive = CheckIfFileSystemIsCaseSensitive();

        private const string TemplateOutputExtension = ".json";

        private const string BicepExtension = ".bicep";

        public static StringComparer PathComparer => IsFileSystemCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        public static StringComparison PathComparison => IsFileSystemCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Converts relative paths to absolute paths relative to current directory. Fully qualified paths are returned as-is.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="baseDirectory">The base directory to use when resolving relative paths. Set to null to use CWD.</param>
        public static string ResolvePath(string path, string? baseDirectory = null)
        {
            if (Path.IsPathFullyQualified(path))
            {
                return path;
            }

            baseDirectory ??= Environment.CurrentDirectory;
            return Path.Combine(baseDirectory, path);
        }

        /// <summary>
        /// Returns a normalized absolute path. Relative paths are converted to absolute paths relative to current directory prior to normalization.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="baseDirectory">The base directory to use when resolving relative paths. Set to null to use CWD.</param>
        public static string ResolveAndNormalizePath(string path, string? baseDirectory = null)
        {
            var resolvedPath = ResolvePath(path, baseDirectory);

            return Path.GetFullPath(resolvedPath);
        }

        public static string GetDefaultBuildOutputPath(string path)
        {
            if (string.Equals(Path.GetExtension(path), TemplateOutputExtension, PathComparison))
            {
                // throwing because this could lead to us destroying the input file if extensions get mixed up.
                throw new ArgumentException($"The specified file already already has the '{TemplateOutputExtension}' extension.");
            }

            return Path.ChangeExtension(path, TemplateOutputExtension);
        }

        /// <summary>
        /// Returns a normalized absolute path. Relative paths are converted to absolute paths relative to current directory prior to normalization.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="baseDirectory">The base directory to use when resolving relative paths. Set to null to use CWD.</param>
        public static string GetDefaultDecompileOutputPath(string path)
        {
            if (string.Equals(Path.GetExtension(path), BicepExtension, PathComparison))
            {
                // throwing because this could lead to us destroying the input file if extensions get mixed up.
                throw new ArgumentException($"The specified file already already has the '{BicepExtension}' extension.");
            }

            return Path.ChangeExtension(path, BicepExtension);
        }

        /// <summary>
        /// Returns true if the current file system is case sensitive (most Linux and MacOS X file systems). Returns false if the file system is case insensitive (Windows file systems.)
        /// </summary>
        private static bool CheckIfFileSystemIsCaseSensitive()
        {
            string tempUpperCasePath = Path.Combine(Path.GetTempPath(), $"BICEPFILESYSTEMTEST{Guid.NewGuid():N}");

            // file will be automatically deleted when stream is closed, which the using declaration will do when the variable goes out of scope
            using var stream = new FileStream(tempUpperCasePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);

            return File.Exists(tempUpperCasePath.ToLowerInvariant()) == false;
        }

        public static Uri FilePathToFileUrl(string filePath)
        {
            filePath = filePath.Replace(Path.DirectorySeparatorChar, '/');
            if (!filePath.StartsWith('/'))
            {
                filePath = "/" + filePath;
            }

            var uriBuilder = new UriBuilder
            {
                Scheme = "file",
                Host = null,
                Path = filePath,
            };

            return uriBuilder.Uri;
        }

        public static Uri ChangeExtension(Uri uri, string? newExtension)
        {
            var uriString = uri.ToString();
            var finalDotIndex = uriString.LastIndexOf('.');

            newExtension = newExtension is null ? "" : NormalizeExtension(newExtension);
            uriString = (finalDotIndex >= 0 ? uriString.Substring(0, finalDotIndex) : uriString) + newExtension;

            return new Uri(uriString);
        }

        public static bool HasExtension(Uri uri, string extension)
        {
            extension = NormalizeExtension(extension);

            return uri.AbsolutePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }

        public static Uri RemoveExtension(Uri uri) => ChangeExtension(uri, null);

        public static Uri ChangeToBicepExtension(Uri uri) => ChangeExtension(uri, BicepExtension);

        public static bool HasBicepExtension(Uri uri) => HasExtension(uri, BicepExtension);

        private static string NormalizeExtension(string extension) =>
            extension.StartsWith(".") ? extension : $".{extension}";
    }
}
