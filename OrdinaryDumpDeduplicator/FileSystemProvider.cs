using System;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    internal class FileSystemProvider : IFileSystemProvider
    {
        public Boolean CheckPathValid(String path)
        {
            return true; // TODO: check path provided.
        }

        public Directory GetDirectoryInfo(String directoryPath, Directory parentDirectory)
        {
            var directoryInfo = new System.IO.DirectoryInfo(directoryPath);
            var directory = new Directory(directoryInfo.Name, parentDirectory, directoryPath);
            return directory;
        }

        public System.IO.FileInfo GetFileInfo(File file)
        {
            var fileInfo = new System.IO.FileInfo(file.Path);
            return fileInfo;
        }

        public System.IO.FileStream GetFileStream(String filePath)
        {
            System.IO.FileStream fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            return fileStream;
        }

        public void MoveFile(File fileToMove, String destinationFilePath)
        {
            try
            {
                System.IO.File.Move(fileToMove.Path, destinationFilePath);
            }
            catch (System.IO.FileNotFoundException fileNotFoundEx)
            {
                var exceptionString = fileNotFoundEx.ToString();
                throw new Exception("", fileNotFoundEx); // TODO
            }
            catch (Exception exception)
            {
                var exceptionString = exception.ToString();
                throw exception;
            }
        }

        public void DeleteFile(File fileToDelete)
        {
            try
            {
                System.IO.File.Delete(fileToDelete.Path);
            }
            catch (System.IO.FileNotFoundException fileNotFoundEx)
            {
                var exceptionString = fileNotFoundEx.ToString();
                throw new Exception("", fileNotFoundEx); // TODO
            }
            catch (Exception exception)
            {
                var exceptionString = exception.ToString();
                throw exception;
            }
        }
    }
}
