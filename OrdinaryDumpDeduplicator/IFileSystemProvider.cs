using System;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    internal interface IFileSystemProvider
    {
        Boolean CheckPathValid(String path);

        Directory GetDirectoryInfo(String directoryPath, Directory parentDirectory);

        System.IO.FileInfo GetFileInfo(File file);

        System.IO.FileStream GetFileStream(String filePath);

        void MoveFile(File fileToMove, String destinationFilePath);

        void DeleteFile(File fileToDelete);
    }
}
