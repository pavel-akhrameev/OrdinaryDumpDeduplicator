using System;
using System.Collections.Generic;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public sealed class DuplicateReport
    {
        #region Private fields

        private readonly IReadOnlyCollection<DataLocation> _dataLocations;

        private readonly IDictionary<BlobInfo, File[]> _unprocessedDuplicatesByHash;

        private readonly IDictionary<BlobInfo, File[]> _partiallyIsolatedDuplicates;

        private readonly IDictionary<BlobInfo, File[]> _uniqueIsolatedFiles;

        private readonly HashSet<Directory> _directoriesForIsolatedDuplicates;

        private Dictionary<Directory, File[]> _directoriesWithDuplicates;

        #endregion

        internal DuplicateReport(
            IReadOnlyCollection<DataLocation> dataLocations,
            IDictionary<BlobInfo, File[]> newDuplicatesByHash,
            IDictionary<BlobInfo, File[]> partiallyIsolatedDuplicates,
            IDictionary<BlobInfo, File[]> uniqueIsolatedFiles,
            HashSet<Directory> directoriesForIsolatedDuplicates)
        {
            this._dataLocations = dataLocations;
            this._unprocessedDuplicatesByHash = newDuplicatesByHash;
            this._partiallyIsolatedDuplicates = partiallyIsolatedDuplicates;
            this._uniqueIsolatedFiles = uniqueIsolatedFiles;
            this._directoriesForIsolatedDuplicates = directoriesForIsolatedDuplicates;
            this._directoriesWithDuplicates = null;
        }

        #region Public properties

        public IReadOnlyCollection<DataLocation> DataLocations => _dataLocations;

        #endregion

        #region Public methods

        public Dictionary<Directory, File[]> AnalyzeDuplicatesAndGroupByFolders()
        {
            // TODO: (?) Решить, нужны ли нам в этой подборке единичные (множественные) файлы из 'duplicates found' или нет.
            IEnumerable<File[]> allDuplicatesByHash = System.Linq.Enumerable.Concat(_unprocessedDuplicatesByHash.Values, _partiallyIsolatedDuplicates.Values);

            if (_directoriesWithDuplicates == null)
            {
                var directoriesWithDuplicates = new Dictionary<Directory, List<File>>();
                foreach (File[] fileDuplicates in allDuplicatesByHash)
                {
                    foreach (File file in fileDuplicates)
                    {
                        if (!directoriesWithDuplicates.ContainsKey(file.ParentDirectory))
                        {
                            directoriesWithDuplicates[file.ParentDirectory] = new List<File>();
                        }

                        var directoryWithDuplicates = directoriesWithDuplicates[file.ParentDirectory];
                        directoryWithDuplicates.Add(file);
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
            foreach (KeyValuePair<BlobInfo, File[]> duplicateInfo in _unprocessedDuplicatesByHash)
            {
                BlobInfo blobInfo = duplicateInfo.Key;
                File[] files = duplicateInfo.Value;

                HierarchicalObject blobObject = MakeUnprocessedDuplicatesObject(blobInfo, files);
                objectsToReport.Add(blobObject);
            }

            foreach (KeyValuePair<BlobInfo, File[]> duplicateInfo in _partiallyIsolatedDuplicates)
            {
                BlobInfo blobInfo = duplicateInfo.Key;
                File[] files = duplicateInfo.Value;

                HierarchicalObject blobObject = MakeComplexBlobObject(blobInfo, files); // Вот тут внимательнее
                objectsToReport.Add(blobObject);
            }

            return objectsToReport.ToArray();
        }

        public HierarchicalObject[] GetUniqueIsolatedFiles()
        {
            var objectsToReport = new List<HierarchicalObject>();
            foreach (KeyValuePair<BlobInfo, File[]> duplicateInfo in _uniqueIsolatedFiles)
            {
                BlobInfo blobInfo = duplicateInfo.Key;
                File[] files = duplicateInfo.Value;

                HierarchicalObject blobObject = MakeUniqueIsolatedObject(blobInfo, files);
                objectsToReport.Add(blobObject);
            }

            return objectsToReport.ToArray();
        }

        #endregion

        #region Private methods

        private HierarchicalObject MakeUnprocessedDuplicatesObject(BlobInfo blobInfo, File[] files)
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

        private HierarchicalObject MakeComplexBlobObject(BlobInfo blobInfo, File[] files)
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

        private HierarchicalObject MakeUniqueIsolatedObject(BlobInfo blobInfo, File[] files)
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

        private HierarchicalObject[] MakeFileObjects(File[] files, out Int32 unprocessedDuplicatesCount, out Int32 isolatedDuplicatesCount)
        {
            unprocessedDuplicatesCount = 0;
            isolatedDuplicatesCount = 0;

            Boolean isFileUnique = files.Length == 1;

            HierarchicalObject[] objects = new HierarchicalObject[files.Length];
            for (Int32 index = 0; index < files.Length; index++)
            {
                var file = files[index];

                ObjectSort fileSort = isFileUnique
                    ? ObjectSort.FileSpecimen | ObjectSort.IsUnique
                    : ObjectSort.FileSpecimen;

                if (IsDirectoryInIsolatedDuplicates(file.ParentDirectory))
                {
                    fileSort |= ObjectSort.IsolatedDuplicate;
                    isolatedDuplicatesCount++;
                }
                else
                {
                    fileSort |= ObjectSort.InOriginalLocation;
                    unprocessedDuplicatesCount++;
                }

                HierarchicalObject fileObject = MakeFileObject(file, fileSort);
                objects[index] = fileObject;
            }

            return objects;
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
