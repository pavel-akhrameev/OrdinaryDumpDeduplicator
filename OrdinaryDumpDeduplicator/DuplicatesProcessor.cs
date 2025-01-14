using System;
using System.Collections.Generic;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    internal sealed class DuplicatesProcessor
    {
        #region Nested struct

        private struct DirectoryForIsolatedDuplicates
        {
            public readonly DataLocation DataLocation;
            private readonly String _originalRelativePath;

            private String _relativePath;
            private String _fullPath;

            public DirectoryForIsolatedDuplicates(DataLocation dataLocation, String directoryRelativePath)
            {
                this.DataLocation = dataLocation;
                this._originalRelativePath = directoryRelativePath;

                this._relativePath = null;
                this._fullPath = null;
            }

            public String RelativePath
            {
                get
                {
                    if (_relativePath == null)
                    {
                        _relativePath = FileSystemHelper.GetCombinedPath(FOLDER_NAME_FOR_DUPLICATES, _originalRelativePath);
                    }

                    return _relativePath;
                }
            }

            public String FullPath
            {
                get
                {
                    if (_fullPath == null)
                    {
                        _fullPath = FileSystemHelper.GetCombinedPath(DataLocation.Path, RelativePath);
                    }

                    return _fullPath;
                }
            }
        }

        #endregion

        public const String FOLDER_NAME_FOR_DUPLICATES = "isolated duplicates";

        private readonly IDataController _dataController;
        private readonly IFileSystemProvider _fileSystemProvider;

        #region Constructor

        public DuplicatesProcessor(IDataController dataController, IFileSystemProvider fileSystemProvider)
        {
            this._dataController = dataController;
            this._fileSystemProvider = fileSystemProvider;
        }

        #endregion

        #region Public methods

        public DuplicateReport GetDuplicatesFound(IReadOnlyCollection<DataLocation> dataLocations)
        {
            if (dataLocations == null || dataLocations.Count == 0)
            {
                throw new ArgumentNullException(nameof(dataLocations), "A null or empty collection is not allowed."); // TODO: exception
            }

            IReadOnlyCollection<FileInfo> duplicatesFound = _dataController.GetDuplicates(dataLocations);
            Dictionary<DataLocation, Directory> directoriesForIsolatedDuplicates = GetDirectoriesForIsolatedDuplicates(dataLocations);
            var existingDirectoriesForIsolatedDuplicates = new HashSet<Directory>(directoriesForIsolatedDuplicates.Values);
            SetDuplicateSortToFileInfo(duplicatesFound, directoriesForIsolatedDuplicates);
            IReadOnlyCollection<SameContentFilesInfo> duplicates = GroupFilesWithSameContent(duplicatesFound);

            // Current files from 'isolated duplicates' directories.
            IReadOnlyCollection<FileInfo> filesFromIsolatedDuplicatesFolders = _dataController.GetDirectoryCurrentFiles(existingDirectoriesForIsolatedDuplicates, includeSubDirectories: true);
            SetIsolatedDuplicateSortToFileInfo(filesFromIsolatedDuplicatesFolders);
            IReadOnlyCollection<SameContentFilesInfo> isolatedDuplicates = GroupFilesWithSameContent(filesFromIsolatedDuplicatesFolders);

            GetFilesToReport(duplicates, isolatedDuplicates, out HashSet<SameContentFilesInfo> filesToReport);

            return new DuplicateReport(filesToReport, dataLocations);
        }

        public IDictionary<FileInfo, FileInfo> MoveDuplicatesToSpecialFolder(DuplicateReport duplicateReport, IReadOnlyCollection<FileInfo> duplicatesToMove)
        {
            // Собрать пути в папке изолированных дубликатов для каждого файла.
            var isolatedDuplicatesDirectories = new Dictionary<Directory, DirectoryForIsolatedDuplicates>();
            foreach (FileInfo duplicate in duplicatesToMove)
            {
                Directory parentDirectory = duplicate.File.ParentDirectory;
                if (!isolatedDuplicatesDirectories.TryGetValue(parentDirectory, out DirectoryForIsolatedDuplicates directoryForIsolatedDuplicates))
                {
                    DataLocation dataLocation = duplicate.DataLocation;
                    String directoryRelativePath = FileSystemHelper.GetRelativePath(dataLocation.Path, parentDirectory.Path);

                    directoryForIsolatedDuplicates = new DirectoryForIsolatedDuplicates(dataLocation, directoryRelativePath);
                    isolatedDuplicatesDirectories.Add(parentDirectory, directoryForIsolatedDuplicates);
                }
            }

            // Подготовить папки для дубликатов. Если папки нет, то она создаётся.
            var originalAndIsolatedDuplicateDirectories = new Dictionary<Directory, Directory>();
            foreach (KeyValuePair<Directory, DirectoryForIsolatedDuplicates> isolatedDuplicatesDirectory in isolatedDuplicatesDirectories)
            {
                DirectoryForIsolatedDuplicates isolatedDuplicatesDirectoryInfo = isolatedDuplicatesDirectory.Value;
                String destinationDirectoryPath = isolatedDuplicatesDirectoryInfo.FullPath;
                if (!System.IO.Directory.Exists(destinationDirectoryPath))
                {
                    System.IO.Directory.CreateDirectory(destinationDirectoryPath);
                }

                Directory directoryForIsolatedDuplicates = _dataController.FindDirectory(destinationDirectoryPath);
                if (directoryForIsolatedDuplicates == null)
                {
                    directoryForIsolatedDuplicates = AddDirectoryForIsolatedDuplicates(isolatedDuplicatesDirectoryInfo);
                }

                originalAndIsolatedDuplicateDirectories.Add(isolatedDuplicatesDirectory.Key, directoryForIsolatedDuplicates);
            }

            var movedFiles = new Dictionary<FileInfo, FileInfo>();

            // Физическое перемещение файлов.
            foreach (FileInfo duplicate in duplicatesToMove)
            {
                File fileToMove = duplicate.File;
                Directory destinationDirectory = originalAndIsolatedDuplicateDirectories[fileToMove.ParentDirectory];
                DirectoryForIsolatedDuplicates isolatedDuplicatesDirectoryInfo = isolatedDuplicatesDirectories[fileToMove.ParentDirectory];
                String destinationDirectoryPath = isolatedDuplicatesDirectoryInfo.FullPath;
                String destinationFilePath = FileSystemHelper.GetCombinedPath(destinationDirectoryPath, fileToMove.Name);

                Boolean isMoved = false;
                try
                {
                    _fileSystemProvider.MoveFile(fileToMove, destinationFilePath);
                    isMoved = true;
                }
                catch (Exception exception)
                {
                    var exceptionString = exception.ToString();
                }

                if (isMoved)
                {
                    // Обновление информации в БД.
                    _dataController.RemoveFile(fileToMove);
                    File movedFile = new File(fileToMove.Name, destinationDirectory);
                    _dataController.AddFile(movedFile);

                    FileState lastFileState = _dataController.GetLastFileState(fileToMove);
                    Inspection inspection = lastFileState.Inspection;
                    lastFileState.SetStatusAndBlobInfo(FileStatus.Removed, BlobInfo.BrokenBlobInfo); // Information of the previous FileState has been corrected.

                    BlobInfo blobInfo = duplicate.BlobInfo;
                    FileState updatedFileState = new FileState(movedFile, inspection, previousState: lastFileState, lastFileState.Size, lastFileState.Status, lastFileState.DateOfCreation, lastFileState.DateOfLastModification, blobInfo); // TODO: Обновить датуВремя с ФС.
                    _dataController.AddFileState(updatedFileState);

                    // Обновление информации в DuplicateReport.
                    duplicateReport.RemoveFileInfo(duplicate);
                    FileInfo updatedFileInfo = new FileInfo(blobInfo, movedFile, duplicate.DataLocation);
                    updatedFileInfo.SetDuplicateSort(DuplicateSort.IsolatedDuplicate);
                    duplicateReport.AddFileInfo(updatedFileInfo);

                    movedFiles.Add(duplicate, updatedFileInfo);
                }
            }

            return movedFiles;
        }

        public IReadOnlyCollection<FileInfo> DeleteDuplicates(DuplicateReport duplicateReport, IReadOnlyCollection<FileInfo> filesToDelete)
        {
            IReadOnlyCollection<DataLocation> currentDataLocations = duplicateReport.DataLocations;
            HashSet<Directory> directoriesForIsolatedDuplicates = DataStructureHelper.GetDirectoriesForIsolatedDuplicates(currentDataLocations);

            // Собираем только те файлы, которые находятся в папках 'isolated duplicates'.
            var filesSuitableForDeletion = new HashSet<FileInfo>();
            foreach (FileInfo duplicate in filesToDelete)
            {
                Boolean isFileFromIsolatedDuplicatesDir = false;
                foreach (Directory isolatedDuplicatesDir in directoriesForIsolatedDuplicates)
                {
                    if (_dataController.IsFileFromDirectory(isolatedDuplicatesDir, duplicate.File))
                    {
                        isFileFromIsolatedDuplicatesDir = true;
                        break;
                    }
                }

                if (isFileFromIsolatedDuplicatesDir)
                {
                    filesSuitableForDeletion.Add(duplicate);
                }
                else
                {
                    throw new Exception("");
                }
            }

            var removedDuplicatesInfo = new List<FileInfo>();

            foreach (FileInfo duplicateInfo in filesSuitableForDeletion)
            {
                File fileToRemove = duplicateInfo.File;
                Boolean isRemoved = false;

                try
                {
                    _fileSystemProvider.DeleteFile(fileToRemove);
                    isRemoved = true;

                }
                catch (Exception exception)
                {
                    var exceptionString = exception.ToString();
                }

                if (isRemoved)
                {
                    // Обновление информации в БД.
                    _dataController.RemoveFile(fileToRemove);

                    FileState lastFileState = _dataController.GetLastFileState(fileToRemove);
                    lastFileState.SetStatusAndBlobInfo(FileStatus.Removed, BlobInfo.BrokenBlobInfo);

                    // Обновление информации в DuplicateReport.
                    duplicateReport.RemoveFileInfo(duplicateInfo);

                    removedDuplicatesInfo.Add(duplicateInfo);
                }
            }

            return removedDuplicatesInfo;
        }

        #endregion

        #region Private methods

        private Dictionary<DataLocation, Directory> GetDirectoriesForIsolatedDuplicates(IEnumerable<DataLocation> dataLocations)
        {
            var existingDirectoriesForIsolatedDuplicates = new Dictionary<DataLocation, Directory>();
            foreach (var dataLocation in dataLocations)
            {
                Directory directoryForIsolatedDuplicates = _dataController.FindDirectory(dataLocation.Directory, FOLDER_NAME_FOR_DUPLICATES);
                if (directoryForIsolatedDuplicates == null)
                {
                    String duplicatesFolderPath = System.IO.Path.Combine(dataLocation.Path, DuplicatesProcessor.FOLDER_NAME_FOR_DUPLICATES);
                    directoryForIsolatedDuplicates = _dataController.FindDirectory(duplicatesFolderPath);
                }

                if (directoryForIsolatedDuplicates != null)
                {
                    existingDirectoriesForIsolatedDuplicates.Add(dataLocation, directoryForIsolatedDuplicates);
                }
            }

            return existingDirectoriesForIsolatedDuplicates;
        }

        private void SetDuplicateSortToFileInfo(IEnumerable<FileInfo> files, Dictionary<DataLocation, Directory> directoriesForIsolatedDuplicates)
        {
            foreach (FileInfo fileInfo in files)
            {
                DuplicateSort duplicateSort;
                if (directoriesForIsolatedDuplicates.TryGetValue(fileInfo.DataLocation, out Directory directoryForIsolatedDuplicates))
                {
                    Boolean isFileInIsolatedDuplicatesDirectory = _dataController.IsFileFromDirectory(directoryForIsolatedDuplicates, fileInfo.File);

                    duplicateSort = isFileInIsolatedDuplicatesDirectory
                        ? DuplicateSort.IsolatedDuplicate
                        : DuplicateSort.InOriginalLocation;
                }
                else
                {
                    duplicateSort = DuplicateSort.InOriginalLocation;
                }

                fileInfo.SetDuplicateSort(duplicateSort);
            }
        }

        private Directory AddDirectoryForIsolatedDuplicates(DirectoryForIsolatedDuplicates directoryForIsolatedDuplicates)
        {
            Directory currentDirectory = directoryForIsolatedDuplicates.DataLocation.Directory;
            IReadOnlyDictionary<String, String> chainOfSubDirectories = FileSystemHelper.GetChainOfNestedDirectories(directoryForIsolatedDuplicates.DataLocation, directoryForIsolatedDuplicates.RelativePath);
            foreach (KeyValuePair<String, String> subDirectoryInfo in chainOfSubDirectories)
            {
                String subDirectoryPath = subDirectoryInfo.Key;
                Directory subDirectory = _dataController.FindDirectory(subDirectoryPath);
                if (subDirectory == null)
                {
                    String directoryName = subDirectoryInfo.Value;
                    subDirectory = new Directory(directoryName, currentDirectory);
                    FsUtils.AddSubDirectory(subDirectory, currentDirectory);

                    _dataController.AddDirectory(subDirectory);
                }

                currentDirectory = subDirectory;
            }

            return currentDirectory;
        }

        #region Private static methods

        private static void GetFilesToReport(
            IReadOnlyCollection<SameContentFilesInfo> duplicatesByHash,
            IReadOnlyCollection<SameContentFilesInfo> filesFromIsolatedDuplicatesFolder,
            out HashSet<SameContentFilesInfo> filesToReport)
        {
            filesToReport = new HashSet<SameContentFilesInfo>(duplicatesByHash);
            foreach (SameContentFilesInfo sameContentIsolatedFiles in filesFromIsolatedDuplicatesFolder)
            {
                if (!filesToReport.Contains(sameContentIsolatedFiles))
                {
                    filesToReport.Add(sameContentIsolatedFiles);
                }
            }
        }

        private static IReadOnlyCollection<SameContentFilesInfo> GroupFilesWithSameContent(IReadOnlyCollection<FileInfo> files)
        {
            var blobGroups = new Dictionary<BlobInfo, HashSet<FileInfo>>();
            foreach (FileInfo fileInfo in files)
            {
                BlobInfo blobInfo = fileInfo.BlobInfo;
                if (!blobGroups.ContainsKey(blobInfo))
                {
                    blobGroups[blobInfo] = new HashSet<FileInfo>();
                }

                var duplicateFiles = blobGroups[blobInfo];
                duplicateFiles.Add(fileInfo);

            }

            var result = new HashSet<SameContentFilesInfo>();
            foreach (KeyValuePair<BlobInfo, HashSet<FileInfo>> blobGroup in blobGroups)
            {
                BlobInfo blobInfo = blobGroup.Key;
                HashSet<FileInfo> duplicatesSet = blobGroup.Value;
                var duplicatesInfo = new SameContentFilesInfo(blobInfo, duplicatesSet);

                result.Add(duplicatesInfo);
            }

            return result;
        }

        private static void SetIsolatedDuplicateSortToFileInfo(IEnumerable<FileInfo> files)
        {
            foreach (var fileInfo in files)
            {
                fileInfo.SetDuplicateSort(DuplicateSort.IsolatedDuplicate);
            }
        }

        #endregion

        #endregion
    }
}
