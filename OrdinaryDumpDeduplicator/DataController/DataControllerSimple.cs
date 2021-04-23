using System;
using System.Collections.Generic;
using System.Linq;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    internal class DataControllerSimple : IDataController
    {
        private readonly HashSet<File> _files;
        private readonly HashSet<Directory> _directories;
        private readonly HashSet<DataLocation> _dataLocations;
        private readonly HashSet<Inspection> _inspections;
        private readonly HashSet<FileState> _fileStates;
        private readonly HashSet<BlobInfo> _blobInfos;

        #region Constructor and initializer

        public DataControllerSimple()
        {
            this._files = new HashSet<File>();
            this._directories = new HashSet<Directory>();
            this._dataLocations = new HashSet<DataLocation>();
            this._inspections = new HashSet<Inspection>();
            this._fileStates = new HashSet<FileState>();
            this._blobInfos = new HashSet<BlobInfo>();
        }

        public void Initialize()
        {
            // Nothing to do here.
        }

        #endregion

        #region Public methods

        /// <remarks>Ну ооочень наивная реализация. Перебираем все папки в базе кучу раз.</remarks>
        public IReadOnlyCollection<Directory> GetSubDirectories(HashSet<Inspection> inspections, HashSet<Directory> directories, Boolean doRecursively)
        {
            // TODO: Учесть инспекцию.
            // (?) А может для директорий инспекции и е важны вовсе. Инспекции важны только для файлов.

            var subDirectories = new List<Directory>();
            foreach (Directory directoryFromDb in _directories)
            {
                if (directories.Contains(directoryFromDb.ParentDirectory))
                {
                    subDirectories.Add(directoryFromDb);
                }
            }

            if (doRecursively && subDirectories.Count > 0)
            {
                var subDirectorieshashSet = new HashSet<Directory>(subDirectories);
                IReadOnlyCollection<Directory> moreSubDirectories = GetSubDirectories(inspections, subDirectorieshashSet, doRecursively: true);
                subDirectories.AddRange(moreSubDirectories);
            }

            return subDirectories;
        }

        public IReadOnlyCollection<File> GetFilesOfDirectory(Directory directory, Boolean includeSubDirectories)
        {
            // TODO: use includeSubDirectories

            File[] filesFound = _files
                .Where(file => directory.Equals(file.ParentDirectory))
                .ToArray();

            return filesFound;
        }

        public void AddFile(File file)
        {
            _files.Add(file);
        }

        public void AddDirectory(Directory directory)
        {
            _directories.Add(directory);

            foreach (var subDirectory in directory.SubDirectories)
            {
                AddDirectory(subDirectory);
            }

            foreach (var file in directory.Files)
            {
                AddFile(file);
            }
        }

        /// <summary>
        /// Получает все <c>DataLocation</c>, где находится эта папка.
        /// </summary>
        public HashSet<DataLocation> GetDataLocations(IEnumerable<Directory> directories, Boolean includeUpperDirectories)
        {
            var foundDataLocations = new HashSet<DataLocation>();

            if (includeUpperDirectories)
            {
                // TODO: Если папка где-то находится на DataLocation, то такую DataLocation надо добавить в результат.
                // (*) Тут можно перебирать все DataLocations и их подпапки, а можно пойти от папки по иерархии вверх.

                var rootDataLocationDirectories = new Dictionary<Directory, DataLocation>();
                foreach (DataLocation dataLocation in _dataLocations)
                {
                    rootDataLocationDirectories.Add(dataLocation.RootDirectory, dataLocation);
                }

                foreach (Directory directory in directories)
                {
                    if (!rootDataLocationDirectories.ContainsKey(directory))
                    {
                        Directory directoryToCheck = directory;
                        while (directoryToCheck != null)
                        {
                            if (!rootDataLocationDirectories.ContainsKey(directoryToCheck))
                            {
                                directoryToCheck = directoryToCheck.ParentDirectory;
                            }
                            else
                            {
                                DataLocation foundDataLocation = rootDataLocationDirectories[directoryToCheck];
                                foundDataLocations.Add(foundDataLocation);

                                break;
                            }
                        }
                    }
                    else
                    {
                        DataLocation foundDataLocation = rootDataLocationDirectories[directory];
                        foundDataLocations.Add(foundDataLocation);
                    }
                }
            }
            else
            {
                HashSet<Directory> directoriesSet = new HashSet<Directory>(directories);

                foreach (DataLocation dataLocation in _dataLocations)
                {
                    if (directoriesSet.Contains(dataLocation.RootDirectory))
                    {
                        foundDataLocations.Add(dataLocation);
                    }
                }
            }

            return foundDataLocations;
        }

        public IReadOnlyCollection<DataLocation> GetDataLocations(IEnumerable<Directory> directories)
        {
            HashSet<DataLocation> dataLocations = GetDataLocations(directories, includeUpperDirectories: false);
            return dataLocations;
        }

        public void AddDataLocation(DataLocation dataLocation)
        {
            _dataLocations.Add(dataLocation);
        }

        public void AddInspection(Inspection inspection)
        {
            _inspections.Add(inspection);
        }

        public void UpdateInspection(Inspection inspection)
        {
            if (_inspections.Contains(inspection))
            {
                // Nothing to do.
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public FileState[] GetSimilarFileStates(FileState fileState)
        {
            // TODO

            return new FileState[] { };
        }

        public void AddFileState(FileState fileState)
        {
            _fileStates.Add(fileState);
        }

        public void UpdateFileState(FileState fileState)
        {
            // Nothing to do here.
        }

        public void AddBlobInfo(BlobInfo blobInfo)
        {
            if (!_blobInfos.Contains(blobInfo))
            {
                _blobInfos.Add(blobInfo);
            }
        }

        public Dictionary<BlobInfo, File[]> GetDuplicatesByHash(IEnumerable<DataLocation> dataLocations)
        {
            var blobGroups = new Dictionary<BlobInfo, List<File>>(); // Все блобы и файлы какие есть.
            HashSet<Inspection> inspectionsSet = GetLastInspections(dataLocations);

            foreach (FileState fileState in _fileStates)
            {
                if (!inspectionsSet.Contains(fileState.Inspection))
                {
                    continue;
                }

                BlobInfo blobInfo = fileState.BlobInfo;
                File file = fileState.File;

                if (blobInfo != null)
                {
                    if (!blobGroups.ContainsKey(blobInfo))
                    {
                        blobGroups[blobInfo] = new List<File>();
                    }

                    var duplicateFiles = blobGroups[blobInfo];
                    duplicateFiles.Add(file);
                }
                else
                {
                    // TODO: log as warning.
                    // TODO: log current fileState.

                    String fileStatusString = Enum.GetName(typeof(FileStatus), fileState.Status);
                    var nullBlobInfoException = new Exception($"FileState with '{fileStatusString}' status and null BlobInfo is not valid (not expected).");
                    throw nullBlobInfoException;
                }
            }

            var result = new Dictionary<BlobInfo, File[]>();
            foreach (KeyValuePair<BlobInfo, List<File>> blobGroup in blobGroups)
            {
                if (blobGroup.Value.Count > 1)
                {
                    File[] duplicates = blobGroup.Value
                        .Distinct()
                        .ToArray();

                    if (duplicates.Length > 1)
                    {
                        result.Add(blobGroup.Key, duplicates);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Получает файлы в наличии из <paramref name="directories"/>, то есть файлы обнаруженные последней инспекцией на своих местах.
        /// </summary>
        /// <remarks>Повторяет функционал метода GetFilesOfDirectory.</remarks>
        public Dictionary<BlobInfo, File[]> GetDirectoryCurrentFiles(IReadOnlyCollection<Directory> directories, Boolean includeSubDirectories)
        {
            IEnumerable<DataLocation> dataLocations = GetDataLocations(directories, includeUpperDirectories: true);
            HashSet<Inspection> inspectionsSet = GetLastInspections(dataLocations); // Нужна крайняя инспекция той DataLocation, где находится каждая папка.

            // (*) Есть ли директория на месте сейчас мы не знаем, да это тут и не важно.
            //HashSet<Directory> directoriesToProcess = GetDirectoriesSet(directories, includeSubDirectories);

            var directoriesToProcess = new HashSet<Directory>(directories);

            if (includeSubDirectories)
            {
                IReadOnlyCollection<Directory> subDirectories = GetSubDirectories(inspectionsSet, directoriesToProcess, includeSubDirectories);
                foreach (Directory subDirectory in subDirectories)
                {
                    directoriesToProcess.Add(subDirectory);
                }
            }

            var blobGroups = new Dictionary<BlobInfo, List<File>>(); // Все блобы и файлы какие есть в указанных папках.
            foreach (FileState fileState in _fileStates)
            {
                if (!inspectionsSet.Contains(fileState.Inspection))
                {
                    continue;
                }

                File file = fileState.File;
                if (!directoriesToProcess.Contains(file.ParentDirectory)) // (?) А тут все директории, которые нам важны?
                {
                    continue;
                }

                BlobInfo blobInfo = fileState.BlobInfo;
                if (blobInfo != null)
                {
                    if (!blobGroups.ContainsKey(blobInfo))
                    {
                        blobGroups[blobInfo] = new List<File>();
                    }

                    var duplicateFiles = blobGroups[blobInfo];
                    duplicateFiles.Add(file);
                }
                else
                {
                    // TODO: log as warning.
                    // TODO: log current fileState.

                    String fileStatusString = Enum.GetName(typeof(FileStatus), fileState.Status);
                    var nullBlobInfoException = new Exception($"FileState with '{fileStatusString}' status and null BlobInfo is not valid (not expected).");
                    throw nullBlobInfoException;
                }
            }

            var result = new Dictionary<BlobInfo, File[]>();
            foreach (KeyValuePair<BlobInfo, List<File>> blobGroup in blobGroups)
            {
                File[] duplicates = blobGroup.Value
                    .Distinct()
                    .ToArray();

                result.Add(blobGroup.Key, duplicates);
            }

            return result;
        }

        public HashSet<Inspection> GetLastInspections(IEnumerable<DataLocation> dataLocations)
        {
            var dataLocationsSet = new HashSet<DataLocation>(dataLocations);
            var lastInspections = new Dictionary<DataLocation, Inspection>();

            foreach (var inspection in _inspections)
            {
                var dataLocation = inspection.DataLocation;
                if (dataLocationsSet.Contains(dataLocation) &&
                    (!lastInspections.TryGetValue(dataLocation, out Inspection lastInspection) ||
                    inspection.StartDateTime > lastInspection.StartDateTime))
                {
                    lastInspections[dataLocation] = inspection;
                }
            }

            var inspectionsSet = new HashSet<Inspection>(lastInspections.Values);
            return inspectionsSet;
        }

        #endregion
    }
}
