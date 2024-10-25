using System;
using System.Collections.Generic;

namespace OrdinaryDumpDeduplicator.Common
{
    public static class FsUtils
    {
        public static void IndexSubDirectoriesAndFiles(
            String folderPath,
            out Directory rootDirectory,
            out IReadOnlyCollection<Directory> folders,
            out IReadOnlyCollection<File> files)
        {
            var isExists = System.IO.Directory.Exists(folderPath);
            if (!isExists)
            {
                throw new ArgumentException("Folder doesn't not exist by given path.", "folderPath");
            }

            const Directory parentDirectoryForRoot = null;
            var allFolders = new List<Directory>();
            var allFiles = new List<File>();

            rootDirectory = AddFolderWithFilesAndSubfolders(folderPath, parentDirectoryForRoot, allFolders, allFiles);
            folders = allFolders;
            files = allFiles;
        }

        public static void AddSubDirectory(Directory subDirectory, Directory parentDirectory)
        {
            parentDirectory.AddSubDirectory(subDirectory);
        }

        public static void AddFile(File file, Directory directory)
        {
            directory.AddFile(file);
        }

        public static Byte[] ComputeSha1Hash(String filePath, out Int64 fileLength)
        {
            Byte[] hashSum;

            using (System.IO.FileStream fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open))
            using (System.IO.BufferedStream bufferedStream = new System.IO.BufferedStream(fileStream))
            {
                using (System.Security.Cryptography.SHA1 sha1Computer = System.Security.Cryptography.SHA1.Create())
                {
                    hashSum = sha1Computer.ComputeHash(bufferedStream);
                }

                fileLength = fileStream.Length;
            }

            return hashSum;
        }

        private static Directory AddFolderWithFilesAndSubfolders(
            String folderPath,
            Directory parentFolder,
            ICollection<Directory> allFolders,
            ICollection<File> allFiles)
        {
            var directoryInfo = new System.IO.DirectoryInfo(folderPath);
            var directory = new Directory(directoryInfo.Name, parentFolder, folderPath);
            allFolders.Add(directory);

            try
            {
                var subDirectoriesPath = System.IO.Directory.GetDirectories(folderPath);
                foreach (var subDirectoryPath in subDirectoriesPath)
                {
                    // Пока исходим из предположения, что максимальный уровень вложенности папок
                    // количественно не привысит максимальную глубину стека вызовов.
                    var subDirectory = AddFolderWithFilesAndSubfolders(subDirectoryPath, directory, allFolders, allFiles);
                    directory.AddSubDirectory(subDirectory);
                }
            }
            catch (System.IO.DirectoryNotFoundException directoryNotFoundEx)
            {
                // TODO: Log this case.
            }
            catch (UnauthorizedAccessException)
            {
                // TODO: Log access restriction.
            }

            try
            {
                var filePaths = System.IO.Directory.GetFiles(folderPath);
                foreach (var filePath in filePaths)
                {
                    var file = MakeFile(filePath, directory);
                    allFiles.Add(file);
                    directory.AddFile(file);
                }
            }
            catch (NotSupportedException notSupportedEx)
            {
                // Например: данный формат пути не поддерживается.
                // TODO: Log this case.
            }
            catch (ArgumentException argumentEx)
            {
                // Например: путь содержит недопустимые знаки.
                // TODO: Log this case.
            }
            catch (UnauthorizedAccessException)
            {
                // TODO: Log access restriction.
            }

            return directory;
        }

        private static File MakeFile(String filePath, Directory parentDirectory)
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            var file = new File(fileInfo.Name, parentDirectory);

            return file;
        }
    }
}
