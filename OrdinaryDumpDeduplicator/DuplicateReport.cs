using System;
using System.Collections.Generic;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public sealed class DuplicateReport
    {
        #region Private fields

        private readonly IReadOnlyCollection<DataLocation> _dataLocations;

        private readonly IReadOnlyCollection<SameContentFilesInfo> _unprocessedDuplicates;

        private readonly IReadOnlyCollection<SameContentFilesInfo> _allDuplicatesIsolated;

        private readonly IReadOnlyCollection<SameContentFilesInfo> _uniqueIsolatedFiles;

        private HashSet<Directory> _directoriesForIsolatedDuplicates;

        private Dictionary<Directory, File[]> _directoriesWithDuplicates;

        #endregion

        internal DuplicateReport(
            IReadOnlyCollection<DataLocation> dataLocations,
            IReadOnlyCollection<SameContentFilesInfo> unprocessedDuplicates,
            IReadOnlyCollection<SameContentFilesInfo> allDuplicatesIsolated,
            IReadOnlyCollection<SameContentFilesInfo> uniqueIsolatedFiles)
        {
            this._dataLocations = dataLocations;
            this._unprocessedDuplicates = unprocessedDuplicates;
            this._allDuplicatesIsolated = allDuplicatesIsolated;
            this._uniqueIsolatedFiles = uniqueIsolatedFiles;
            this._directoriesWithDuplicates = null;
        }

        #region Public properties

        public IReadOnlyCollection<SameContentFilesInfo> DuplicatesFound => _unprocessedDuplicates;

        public IReadOnlyCollection<SameContentFilesInfo> AllDuplicatesIsolated => _allDuplicatesIsolated;

        public IReadOnlyCollection<SameContentFilesInfo> UniqueIsolatedFiles => _uniqueIsolatedFiles;

        public IReadOnlyCollection<DataLocation> DataLocations => _dataLocations;

        #endregion

        #region Public methods

        public Dictionary<Directory, File[]> AnalyzeDuplicatesAndGroupByFolders()
        {
            // TODO: (?) Решить, нужны ли нам в этой подборке единичные (множественные) файлы из 'duplicates found' или нет.
            IEnumerable<SameContentFilesInfo> allDuplicatesByHash = System.Linq.Enumerable.Concat(_unprocessedDuplicates, _allDuplicatesIsolated);

            if (_directoriesWithDuplicates == null)
            {
                var directoriesWithDuplicates = new Dictionary<Directory, List<File>>();
                foreach (SameContentFilesInfo fileDuplicates in allDuplicatesByHash)
                {
                    foreach (FileInfo fileInfo in fileDuplicates.Duplicates)
                    {
                        if (!directoriesWithDuplicates.ContainsKey(fileInfo.File.ParentDirectory))
                        {
                            directoriesWithDuplicates[fileInfo.File.ParentDirectory] = new List<File>();
                        }

                        var directoryWithDuplicates = directoriesWithDuplicates[fileInfo.File.ParentDirectory];
                        directoryWithDuplicates.Add(fileInfo.File);
                    }
                }

                _directoriesWithDuplicates = new Dictionary<Directory, File[]>(directoriesWithDuplicates.Count);
                foreach (KeyValuePair<Directory, List<File>> directoryWithDuplicates in directoriesWithDuplicates)
                {
                    _directoriesWithDuplicates.Add(directoryWithDuplicates.Key, directoryWithDuplicates.Value.ToArray());
                }
            }

            return _directoriesWithDuplicates;
        }

        public HierarchicalObject[] GroupDuplicatesByDirectories()
        {
            // (*) Тут могут быть папки из разных DataLocation. 

            Dictionary<Directory, File[]> directoriesWithDuplicates = AnalyzeDuplicatesAndGroupByFolders();

            var rootDirectories = new HashSet<Directory>();
            var directoriesToReport = new Dictionary<Directory, HashSet<Directory>>();
            foreach (DataLocation dataLocation in _dataLocations)
            {
                rootDirectories.Add(dataLocation.Directory);
                directoriesToReport.Add(dataLocation.Directory, new HashSet<Directory>());
            }

            foreach (KeyValuePair<Directory, File[]> directoryWithDuplicates in directoriesWithDuplicates)
            {
                Directory directory = directoryWithDuplicates.Key;
                AddDirectory(directoriesToReport, directory);
            }

            var objectsToReport = new Dictionary<Directory, HierarchicalObject>();
            var rootDirectoriesToReport = new List<HierarchicalObject>();
            foreach (KeyValuePair<Directory, HashSet<Directory>> directoryWithChildren in directoriesToReport)
            {
                Directory directory = directoryWithChildren.Key;
                HashSet<Directory> childDirectories = directoryWithChildren.Value;
                var childObjects = new List<HierarchicalObject>();

                foreach (Directory childDirectory in childDirectories)
                {
                    var childDirectoryObject = HierarchicalObject.Create(childDirectory, ObjectSort.None, childObjects: null);
                    objectsToReport.Add(childDirectory, childDirectoryObject);
                    childObjects.Add(childDirectoryObject);
                }

                if (directoriesWithDuplicates.TryGetValue(directory, out File[] files))
                {
                    foreach (var file in files)
                    {
                        var fileObject = HierarchicalObject.Create(file, ObjectSort.None, childObjects: null);
                        childObjects.Add(fileObject);
                    }
                }

                if (objectsToReport.TryGetValue(directory, out HierarchicalObject directoryObject))
                {
                    directoryObject.SetChildObjects(childObjects.ToArray());
                }
                else
                {
                    directoryObject = HierarchicalObject.Create(directory, ObjectSort.None, childObjects.ToArray());
                    objectsToReport.Add(directory, directoryObject);
                }

                if (rootDirectories.Contains(directory))
                {
                    rootDirectoriesToReport.Add(directoryObject);
                }
            }

            return rootDirectoriesToReport.ToArray();
        }

        public HierarchicalObject[] GroupDuplicatesByHash()
        {
            var objectsToReport = new List<HierarchicalObject>();
            foreach (SameContentFilesInfo duplicatesGroup in _allDuplicatesIsolated)
            {
                BlobInfo blobInfo = duplicatesGroup.BlobInfo;
                IReadOnlyCollection<FileInfo> files = duplicatesGroup.Duplicates;

                HierarchicalObject blobObject = MakeUnprocessedDuplicatesObject(blobInfo, files);
                objectsToReport.Add(blobObject);
            }

            foreach (var duplicatesGroup in _unprocessedDuplicates)
            {
                BlobInfo blobInfo = duplicatesGroup.BlobInfo;
                IReadOnlyCollection<FileInfo> files = duplicatesGroup.Duplicates;

                HierarchicalObject blobObject = MakeComplexBlobObject(blobInfo, files); // Вот тут внимательнее
                objectsToReport.Add(blobObject);
            }

            return objectsToReport.ToArray();
        }

        public HierarchicalObject[] GetUniqueIsolatedFiles()
        {
            var objectsToReport = new List<HierarchicalObject>();
            foreach (SameContentFilesInfo duplicateInfo in _uniqueIsolatedFiles)
            {
                BlobInfo blobInfo = duplicateInfo.BlobInfo;
                IReadOnlyCollection<FileInfo> files = duplicateInfo.Duplicates;

                HierarchicalObject blobObject = MakeUniqueIsolatedObject(blobInfo, files);
                objectsToReport.Add(blobObject);
            }

            return objectsToReport.ToArray();
        }

        #endregion

        #region Private methods

        private HierarchicalObject MakeUnprocessedDuplicatesObject(BlobInfo blobInfo, IReadOnlyCollection<FileInfo> files)
        {
            HierarchicalObject[] fileObjects = MakeFileObjects(files, out Int32 unprocessedDuplicatesCount, out Int32 isolatedDuplicatesCount);

            ObjectSort blobSort = ObjectSort.Blob;
            if (isolatedDuplicatesCount > 0) // Откуда-то есть изолированные дубликаты
            {
                // TODO: Warning
                //throw new Exception();
            }

            if (unprocessedDuplicatesCount >= 1)
            {
                blobSort |= ObjectSort.HasOriginalFiles;

                if (unprocessedDuplicatesCount > 1)
                {
                    blobSort |= ObjectSort.HasUnprocessedDuplicates;
                }
                else
                {
                    // TODO: Warning. Странная ситация - всего один файл у блоба.
                    throw new Exception();
                }
            }

            String blobRepresentation = blobInfo.ToString();
            HierarchicalObject blobObject = HierarchicalObject.Create(blobInfo, blobSort, childObjects: fileObjects, blobRepresentation);
            return blobObject;
        }

        private HierarchicalObject MakeComplexBlobObject(BlobInfo blobInfo, IReadOnlyCollection<FileInfo> files)
        {
            HierarchicalObject[] fileObjects = MakeFileObjects(files, out Int32 unprocessedDuplicatesCount, out Int32 isolatedDuplicatesCount);

            ObjectSort blobSort = ObjectSort.Blob;
            if (isolatedDuplicatesCount > 0) // Уже есть изолированные дубликаты
            {
                blobSort |= ObjectSort.HasIsolatedDuplicates;
            }
            else
            {
                // TODO: Warning
                throw new Exception();
            }

            if (unprocessedDuplicatesCount >= 1)
            {
                blobSort |= ObjectSort.HasOriginalFiles;

                if (unprocessedDuplicatesCount > 1)
                {
                    blobSort |= ObjectSort.HasUnprocessedDuplicates;
                }
            }
            else
            {
                // TODO: Warning
                throw new Exception();
            }

            String blobRepresentation = blobInfo.ToString();
            HierarchicalObject blobObject = HierarchicalObject.Create(blobInfo, blobSort, childObjects: fileObjects, blobRepresentation);
            return blobObject;
        }

        private HierarchicalObject MakeUniqueIsolatedObject(BlobInfo blobInfo, IReadOnlyCollection<FileInfo> files)
        {
            HierarchicalObject[] fileObjects = MakeFileObjects(files, out Int32 unprocessedDuplicatesCount, out Int32 isolatedDuplicatesCount);
            ObjectSort blobSort = ObjectSort.Blob;

            if (unprocessedDuplicatesCount > 0)
            {
                // TODO: Warning. Странная ситация - ещё остались необработанные дубликаты.
                blobSort |= ObjectSort.HasOriginalFiles;

                // TODO: Может стоит тут сваливаться, если будет найдет неуникальный файл (который есть по оригинальному пути).
                throw new Exception("");
            }

            if (isolatedDuplicatesCount > 0)
            {
                blobSort |= ObjectSort.HasIsolatedDuplicates | ObjectSort.ContainsUniqueIsolatedFiles;
            }
            else
            {
                // TODO: Warning
                throw new Exception();
            }

            String blobRepresentation = blobInfo.ToString();
            HierarchicalObject blobObject = HierarchicalObject.Create(blobInfo, blobSort, childObjects: fileObjects, blobRepresentation);
            return blobObject;
        }

        private HierarchicalObject[] MakeFileObjects(IReadOnlyCollection<FileInfo> files, out Int32 unprocessedDuplicatesCount, out Int32 isolatedDuplicatesCount)
        {
            unprocessedDuplicatesCount = 0;
            isolatedDuplicatesCount = 0;

            Boolean isFileUnique = files.Count == 1;

            var objects = new List<HierarchicalObject>(files.Count);
            foreach (var fileInfo in files)
            {
                ObjectSort fileSort = isFileUnique
                    ? ObjectSort.FileSpecimen | ObjectSort.IsUnique
                    : ObjectSort.FileSpecimen;

                if (IsDirectoryInIsolatedDuplicates(fileInfo.File.ParentDirectory))
                {
                    fileSort |= ObjectSort.IsolatedDuplicate;
                    isolatedDuplicatesCount++;
                }
                else
                {
                    fileSort |= ObjectSort.InOriginalLocation;
                    unprocessedDuplicatesCount++;
                }

                HierarchicalObject fileObject = MakeFileObject(fileInfo.File, fileSort);
                objects.Add(fileObject);
            }

            return objects.ToArray();
        }

        private Boolean IsDirectoryInIsolatedDuplicates(Directory directory) // TODO: check
        {
            if (_directoriesForIsolatedDuplicates.Contains(directory))
            {
                return true;
            }
            else if (directory.ParentDirectory != null)
            {
                return IsDirectoryInIsolatedDuplicates(directory.ParentDirectory);
            }
            else
            {
                return false;
            }
        }

        private static HierarchicalObject MakeFileObject(File file, ObjectSort fileSort)
        {
            String fileRepresentation = $"{file.Name} | {file.ParentDirectory.Path}";
            HierarchicalObject fileObject = HierarchicalObject.Create(file, fileSort, childObjects: null, fileRepresentation);
            return fileObject;
        }

        private static void AddDirectory(Dictionary<Directory, HashSet<Directory>> directories, Directory directoryToAdd)
        {
            Directory parentDirectory = directoryToAdd.ParentDirectory;
            if (parentDirectory != null)
            {
                if (!directories.ContainsKey(parentDirectory))
                {
                    AddDirectory(directories, parentDirectory);
                }

                directories[parentDirectory].Add(directoryToAdd);
            }

            if (!directories.ContainsKey(directoryToAdd))
            {
                directories.Add(directoryToAdd, new HashSet<Directory>());
            }
        }

        #endregion
    }
}
