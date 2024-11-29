using System;
using System.Collections.Generic;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    internal sealed class DuplicatesProcessor
    {
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

            SeparateFoundDuplicatesIntoCategories(duplicates, isolatedDuplicates, out HashSet<SameContentFilesInfo> unprocessedDuplicates,
                out HashSet<SameContentFilesInfo> allDuplicatesIsolated, out HashSet<SameContentFilesInfo> uniqueIsolatedFiles);

            return new DuplicateReport(dataLocations, unprocessedDuplicates, allDuplicatesIsolated, uniqueIsolatedFiles);
        }

        public void MoveKnownDuplicatesToSpecialFolder(DuplicateReport duplicateReport, HierarchicalObject[] hierarchicalObjects)
        {
            String dataLocationPath;
            if (duplicateReport.DataLocations.Count == 1)
            {
                DataLocation currentDataLocation = System.Linq.Enumerable.First(duplicateReport.DataLocations);
                dataLocationPath = currentDataLocation.Path;
            }
            else
            {
                throw new NotImplementedException(""); // TODO
            }

            String folderForDuplicates = System.IO.Path.Combine(dataLocationPath, FOLDER_NAME_FOR_DUPLICATES);

            // Note: На данном этапе в виде сущности HierarchicalObjectInReport может быть передан объект следующих типов:
            // Directory или File.

            var fileObjectsToProcess = new HashSet<HierarchicalObject>();
            foreach (HierarchicalObject hierarchicalObject in hierarchicalObjects)
            {
                // Определить, это папка или файл.

                File file = hierarchicalObject.Object as File;
                Directory directory = hierarchicalObject.Object as Directory;
                if (directory != null)
                {
                    AddAllSubObjects(hierarchicalObject, fileObjectsToProcess);
                }
                else if (file != null)
                {
                    fileObjectsToProcess.Add(hierarchicalObject);
                }
                else
                {
                    throw new ArgumentException($"Unknown type of wrapped object found '{hierarchicalObject}'");
                }
            }

            var filesToProcess = new HashSet<File>();
            foreach (var fileObject in fileObjectsToProcess)
            {
                filesToProcess.Add(fileObject.Object as File);
            }

            // Собрать пути в папке для дубликатов для каждого файла.
            var patchesForDuplicates = new Dictionary<Directory, String>();
            foreach (File file in filesToProcess)
            {
                Directory parentDirectory = file.ParentDirectory;
                if (!patchesForDuplicates.ContainsKey(parentDirectory))
                {
                    String relativeDirectoyPath = FileSystemHelper.GetRelativePath(dataLocationPath, parentDirectory.Path);
                    String folderForDuplicate = FileSystemHelper.GetCombinedPath(folderForDuplicates, relativeDirectoyPath);

                    patchesForDuplicates.Add(parentDirectory, folderForDuplicate);
                }
            }

            // Подготовить папки для дубликатов.
            foreach (String patchForDuplicates in patchesForDuplicates.Values)
            {
                if (!System.IO.Directory.Exists(patchForDuplicates))
                {
                    System.IO.Directory.CreateDirectory(patchForDuplicates);
                }
            }

            foreach (File file in filesToProcess)
            {
                String folderPathForDuplicate = patchesForDuplicates[file.ParentDirectory];

                String destinationFilePath = FileSystemHelper.GetCombinedPath(folderPathForDuplicate, file.Name);
                try
                {
                    _fileSystemProvider.MoveFile(file, destinationFilePath);
                }
                catch (Exception exception)
                {
                    var exceptionString = exception.ToString();
                }
            }
        }

        public void DeleteDuplicate(DuplicateReport duplicateReport, File[] filesToDelete)
        {
            IReadOnlyCollection<DataLocation> currentDataLocations = duplicateReport.DataLocations;
            HashSet<Directory> directoriesForIsolatedDuplicates = DataStructureHelper.GetDirectoriesForIsolatedDuplicates(currentDataLocations);

            // Собираем только те файлы, которые находятся в папках 'isolated duplicates'.
            var filesSuitableForDeletion = new HashSet<File>();
            foreach (File file in filesToDelete)
            {
                Boolean isFileFromIsolatedDuplicatesDir = false;
                foreach (Directory isolatedDuplicatesDir in directoriesForIsolatedDuplicates)
                {
                    if (_dataController.IsFileFromDirectory(isolatedDuplicatesDir, file))
                    {
                        isFileFromIsolatedDuplicatesDir = true;
                        break;
                    }
                }

                if (isFileFromIsolatedDuplicatesDir)
                {
                    filesSuitableForDeletion.Add(file);
                }
                else
                {
                    throw new Exception("");
                }
            }

            foreach (File file in filesSuitableForDeletion)
            {
                try
                {
                    _fileSystemProvider.DeleteFile(file);
                }
                catch (Exception exception)
                {
                    var exceptionString = exception.ToString();
                }
            }
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

        #region Private static methods

        private static void AddAllSubObjects(HierarchicalObject hierarchicalObject, ICollection<HierarchicalObject> subObjects)
        {
            foreach (var childObject in hierarchicalObject.ChildObjects)
            {
                switch (childObject.Object)
                {
                    case Directory directory:
                        AddAllSubObjects(childObject, subObjects);
                        break;

                    case File file:
                        subObjects.Add(childObject);
                        break;

                    default:
                        throw new ArgumentException($"Unknown type of wrapped object found '{childObject}'");
                }
            }
        }

        private static void SeparateFoundDuplicatesIntoCategories(
            IReadOnlyCollection<SameContentFilesInfo> duplicatesByHash,
            IReadOnlyCollection<SameContentFilesInfo> filesFromIsolatedDuplicatesFolder,
            out HashSet<SameContentFilesInfo> hasUnprocessedDuplicates,
            out HashSet<SameContentFilesInfo> allDuplicatesIsolated,
            out HashSet<SameContentFilesInfo> uniqueIsolatedFiles)
        {
            hasUnprocessedDuplicates = new HashSet<SameContentFilesInfo>();
            allDuplicatesIsolated = new HashSet<SameContentFilesInfo>();
            uniqueIsolatedFiles = new HashSet<SameContentFilesInfo>();

            var blobsOnOriginalLocation = new HashSet<BlobInfo>();
            foreach (SameContentFilesInfo sameContentFilesInfo in duplicatesByHash)
            {
                BlobInfo blobInfo = sameContentFilesInfo.BlobInfo;

                // Among such duplicates (blobs) there may be those that have no files in the original locations at all.
                IReadOnlyCollection<FileInfo> sameContentFiles = sameContentFilesInfo.Duplicates;

                Int32 originalLocationFilesCount = 0;
                Int32 isolatedDuplicatesCount = 0;
                foreach (FileInfo fileInfo in sameContentFiles)
                {
                    switch (fileInfo.Sort)
                    {
                        case DuplicateSort.InOriginalLocation:
                            originalLocationFilesCount++;
                            break;
                        case DuplicateSort.IsolatedDuplicate:
                            isolatedDuplicatesCount++;
                            break;
                    }
                }

                if (originalLocationFilesCount > 0)
                {
                    blobsOnOriginalLocation.Add(blobInfo);

                    if (originalLocationFilesCount == 1)
                    {
                        allDuplicatesIsolated.Add(sameContentFilesInfo);
                    }
                    else
                    {
                        hasUnprocessedDuplicates.Add(sameContentFilesInfo);
                    }
                }
                else if (isolatedDuplicatesCount > 0)
                {
                    uniqueIsolatedFiles.Add(sameContentFilesInfo);
                }
                else
                {
                    throw new Exception(""); // TODO
                }
            }

            foreach (SameContentFilesInfo sameContentIsolatedFiles in filesFromIsolatedDuplicatesFolder)
            {
                if (!blobsOnOriginalLocation.Contains(sameContentIsolatedFiles.BlobInfo))
                {
                    uniqueIsolatedFiles.Add(sameContentIsolatedFiles);
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
