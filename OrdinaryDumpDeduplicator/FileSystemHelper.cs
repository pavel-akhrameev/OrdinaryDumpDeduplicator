using System;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    internal static class FileSystemHelper
    {
        /// <summary>
        /// Creates a relative path from <paramref name="relativeTo"/> directory to file or another directory with <paramref name="path"/>.
        /// </summary>
        /// <param name="relativeTo">Contains the directory that defines the start of the relative path.</param>
        /// <param name="path">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from <paramref name="relativeTo"/> directory to the end <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="relativeTo"/> or <paramref name="path"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>System.IO.Path.GetRelativePath(..) is not implemented on .Net Standard and .Net Framework.</remarks>
        public static String GetRelativePath(String relativeTo, String path)
        {
            if (String.IsNullOrEmpty(relativeTo))
            {
                throw new ArgumentNullException(nameof(relativeTo));
            }

            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (String.Equals(relativeTo, path, StringComparison.Ordinal))
            {
                return String.Empty;
            }

            String correctRelativeTo;
            if (relativeTo.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
            {
                correctRelativeTo = relativeTo;
            }
            else
            {
                correctRelativeTo = $"{relativeTo}{System.IO.Path.DirectorySeparatorChar}";
            }

            Uri relativeToUri = new Uri(correctRelativeTo);
            Uri pathUri = new Uri(path);

            if (relativeToUri.Scheme != pathUri.Scheme)
            {
                throw new ArgumentException("Unable to get relative path."); // TODO
            }

            Uri relativeUri = relativeToUri.MakeRelativeUri(pathUri);
            String relativePathString = Uri.UnescapeDataString(relativeUri.ToString());

            String relativePathWithCorrectDelimiters;
            if (String.Equals(pathUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePathWithCorrectDelimiters = relativePathString.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
            }
            else
            {
                throw new ArgumentException("Unable to get relative path."); // TODO
            }

            return relativePathWithCorrectDelimiters;
        }

        public static String GetCombinedPath(String firstPathPart, String secondPathPart)
        {
            if (String.IsNullOrWhiteSpace(firstPathPart))
            {
                throw new ArgumentNullException(nameof(firstPathPart));
            }

            if (secondPathPart == null)
            {
                throw new ArgumentNullException(nameof(secondPathPart));
            }

            String combinedPath = System.IO.Path.Combine(firstPathPart, secondPathPart);
            return combinedPath;
        }

        public static System.Collections.Generic.IReadOnlyDictionary<String, String> GetChainOfNestedDirectories(DataLocation dataLocation, String directoryRelativePath)
        {
            String rootDirectoryPath = dataLocation.Directory.Path;
            String[] pathElements = directoryRelativePath.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

            var nestedDirectories = new System.Collections.Generic.Dictionary<String, String>(pathElements.Length);
            String currentDirectoryPath = rootDirectoryPath;
            for (Int16 index = 0; index < pathElements.Length; index++)
            {
                String pathElement = pathElements[index]; // Directory name.
                String newDirectoryPath = GetCombinedPath(currentDirectoryPath, pathElement);

                nestedDirectories.Add(newDirectoryPath, pathElement);
                currentDirectoryPath = newDirectoryPath;
            }

            return nestedDirectories;
        }
    }
}
