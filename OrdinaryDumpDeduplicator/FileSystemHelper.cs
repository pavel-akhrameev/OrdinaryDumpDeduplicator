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
    }
}
