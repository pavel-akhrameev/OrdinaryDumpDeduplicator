using System;
using System.IO;

namespace OrdinaryDumpDeduplicator
{
    public class FileUtils
    {
        private readonly Common.IDirectoryProcessor _directoryProcessor;
        private readonly Common.IFileProcessor _fileProcessor;

        public FileUtils(Common.IDirectoryProcessor directoryProcessor, Common.IFileProcessor fileProcessor)
        {
            this._directoryProcessor = directoryProcessor;
            this._directoryProcessor.DirectoryProcessed += OnDirectoryProcessed;

            this._fileProcessor = fileProcessor;
            this._fileProcessor.FileProcessed += OnFileProcessed;
        }

        public Common.File MakeFileInfo(string filePath)
        {
            var isExists = File.Exists(filePath);
            if (!isExists)
            {
                throw new ArgumentException("File doesn't not exist by given path.", "filePath");
            }

            var fileName = Path.GetFileName(filePath);
            var fileInfo = new FileInfo(filePath);

            var parentDirectory = MakeDirectoryInfo(fileInfo.DirectoryName);

            var file = new Common.File(fileName, parentDirectory, fileInfo.CreationTime, fileInfo.LastWriteTime);
            _fileProcessor.AddFileToProcess(file);

            return file; // (?) А точно такой файл надо вернуть?
        }

        public Common.Directory MakeDirectoryInfo(string directoryPath)
        {
            var directory = MakeDirectory(directoryPath);

            _directoryProcessor.AddDirectoryToProcess(directory);

            return directory; // (?)
        }

        private Common.Directory MakeDirectory(string directoryPath)
        {
            var isExists = Directory.Exists(directoryPath);
            if (!isExists)
            {
                throw new ArgumentException("Directory doesn't not exist by given path.", "directoryPath");
            }

            var directoryName = Path.GetDirectoryName(directoryPath);
            var directoryInfo = new DirectoryInfo(directoryPath);

            var parentDirectory = MakeDirectoryInfo(directoryInfo.Parent.Name);

            var directory = new Common.Directory(directoryName, parentDirectory, directoryInfo.CreationTime, directoryInfo.LastWriteTime);
            return directory;
        }

        private void OnDirectoryProcessed(object sender, EventArgs e)
        {
            // TODO
        }

        private void OnFileProcessed(object sender, EventArgs e)
        {
            // TODO
        }
    }
}
