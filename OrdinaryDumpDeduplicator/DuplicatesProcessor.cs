using System;
using System.Collections.Generic;
using System.Linq;

using OrdinaryDumpDeduplicator.Common;

using FilesWithSameContent = System.Collections.Generic.IDictionary<OrdinaryDumpDeduplicator.Common.BlobInfo, OrdinaryDumpDeduplicator.Common.File[]>;

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
            FilesWithSameContent duplicatesByHash;
            if (dataLocations != null && dataLocations.Count > 0)
            {
                // TODO: Если dataLocations задано несколько, или хотя бы одна, то надо искать по файлам из dataLocations.

                duplicatesByHash = _dataController.GetDuplicatesByHash(dataLocations); // TODO
            }
            else
            {
                duplicatesByHash = _dataController.GetDuplicatesByHash(dataLocations);
            }

            HashSet<Directory> directoriesForIsolatedDuplicates = DataStructureHelper.GetDirectoriesForIsolatedDuplicates(dataLocations);

            // Достаются текущие файлы из 'duplicates found'. Все уникальные из них нужно показать пользователю, как возможную проблему.
            FilesWithSameContent filesFromIsolatedDuplicatesFolders = _dataController.GetDirectoryCurrentFiles(directoriesForIsolatedDuplicates, includeSubDirectories: true);

            SeparateFoundDuplicatesIntoCategories(duplicatesByHash, filesFromIsolatedDuplicatesFolders, out FilesWithSameContent newDuplicatesByHash,
                out FilesWithSameContent partiallyIsolatedDuplicates, out FilesWithSameContent uniqueIsolatedFiles);

            return new DuplicateReport(dataLocations, newDuplicatesByHash, partiallyIsolatedDuplicates, uniqueIsolatedFiles, directoriesForIsolatedDuplicates);
        }

        public void MoveKnownDuplicatesToSpecialFolder(DuplicateReport duplicateReport, HierarchicalObject[] hierarchicalObjects)
        {
            String dataLocationPath;
            if (duplicateReport.DataLocations.Count == 1)
            {
                DataLocation currentDataLocation = duplicateReport.DataLocations.First();
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

        #region Private static methods

        private static void SeparateFoundDuplicatesIntoCategories(
            FilesWithSameContent duplicatesByHash,
            FilesWithSameContent filesFromIsolatedDuplicatesFolder,
            out FilesWithSameContent newDuplicatesByHash,
            out FilesWithSameContent partiallyIsolatedDuplicates,
            out FilesWithSameContent uniqueIsolatedFiles)
        {
            newDuplicatesByHash = new Dictionary<BlobInfo, File[]>();
            partiallyIsolatedDuplicates = new Dictionary<BlobInfo, File[]>();
            uniqueIsolatedFiles = new Dictionary<BlobInfo, File[]>();

            var isolatedDirectories = new HashSet<Directory>();
            foreach (KeyValuePair<BlobInfo, File[]> sameContentIsolatedFiles in filesFromIsolatedDuplicatesFolder)
            {
                File[] isolatedFiles = sameContentIsolatedFiles.Value;
                foreach (File isolatedFile in isolatedFiles)
                {
                    isolatedDirectories.Add(isolatedFile.ParentDirectory);
                }
            }

            foreach (KeyValuePair<BlobInfo, File[]> sameContentFiles in duplicatesByHash)
            {
                BlobInfo blobInfo = sameContentFiles.Key;
                if (filesFromIsolatedDuplicatesFolder.ContainsKey(blobInfo))
                {
                    File[] originalLocationFiles = sameContentFiles.Value;
                    if (DoTheseDirectoriesContainAllFiles(isolatedDirectories, originalLocationFiles)) // Среди таких блобов могут быть те, у которых вообще нет файлов на оригинальных локациях.
                    {
                        uniqueIsolatedFiles.Add(sameContentFiles);
                    }
                    else
                    {
                        partiallyIsolatedDuplicates.Add(sameContentFiles);
                    }
                }
                else
                {
                    newDuplicatesByHash.Add(sameContentFiles);
                }
            }

            foreach (KeyValuePair<BlobInfo, File[]> sameContentIsolatedFiles in filesFromIsolatedDuplicatesFolder)
            {
                BlobInfo blobInfo = sameContentIsolatedFiles.Key;
                File[] files = sameContentIsolatedFiles.Value;

                if (!duplicatesByHash.ContainsKey(blobInfo))
                {
                    uniqueIsolatedFiles.Add(sameContentIsolatedFiles);
                }
            }
        }

        private static Boolean DoTheseDirectoriesContainAllFiles(HashSet<Directory> directories, IEnumerable<File> files)
        {
            foreach (File file in files)
            {
                var directoryToCheck = file.ParentDirectory;
                if (!directories.Contains(directoryToCheck))
                {
                    return false;
                }
            }

            return true;
        }

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

        #endregion
    }
}
