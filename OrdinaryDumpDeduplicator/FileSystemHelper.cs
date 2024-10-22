using System;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    /// <summary>
    /// Альтернативная реализация индексатора содержимого папки.
    /// </summary>
    internal static class FileSystemHelper
    {
        private static Directory MakeDirectoryStructure(System.IO.DirectoryInfo directoryInfo, Directory parentDirectory)
        {
            Directory directory = directoryInfo.MakeDirectory(parentDirectory);

            try // TODO: если заблокирован хотя бы один файл - все остальные не обрабатываются.
            {
                System.IO.FileInfo[] filesInfo = directoryInfo.GetFiles();
                foreach (System.IO.FileInfo fileInfo in filesInfo)
                {
                    var file = fileInfo.MakeFile(directory);
                    FsUtils.AddFile(file, directory);
                }
            }
            catch (Exception ex)
            {
                // TODO
            }

            try
            {
                System.IO.DirectoryInfo[] subDirectoriesInfo = directoryInfo.GetDirectories();
                foreach (var subDirectoryInfo in subDirectoriesInfo)
                {
                    var subDirectory = MakeDirectoryStructure(subDirectoryInfo, directory);
                    FsUtils.AddSubDirectory(subDirectory, directory);
                }
            }
            catch (Exception ex)
            {
                // TODO
            }

            return directory;
        }

        public static Directory MakeDirectory(this System.IO.DirectoryInfo directoryInfo, Directory parentDirectory)
        {
            var directory = new Directory(directoryInfo.Name, parentDirectory);
            return directory;
        }

        public static File MakeFile(this System.IO.FileInfo fileInfo, Directory parentDirectory)
        {
            var file = new File(fileInfo.Name, parentDirectory);
            return file;
        }

        /// <summary>
        /// Creates a relative path from <paramref name="relativeTo"/> directory to file or another directory with <paramref name="path"/>.
        /// </summary>
        /// <param name="relativeTo">Contains the directory that defines the start of the relative path.</param>
        /// <param name="path">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from <paramref name="relativeTo"/> directory to the end <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="relativeTo"/> or <paramref name="path"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static String GetRelativePath(String relativeTo, String path)
        {
            if (String.IsNullOrEmpty(relativeTo))
            {
                throw new ArgumentNullException("relativeTo");
            }

            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
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
    }
}
