using System;
using System.Collections.Generic;
using System.Linq;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    internal class DataControllerSimple : IDataController
    {
        #region Private properties

        private readonly HashSet<File> _files;
        private readonly HashSet<Directory> _directories;
        private readonly HashSet<DataLocation> _dataLocations;
        private readonly HashSet<Inspection> _inspections;
        private readonly HashSet<FileState> _fileStates;
        private readonly HashSet<BlobInfo> _blobInfos;

        #endregion

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

        public void AddFile(File file)
        {
            _files.Add(file);
        }

        public void AddDirectory(Directory directory)
        {
            // Вот тут не происходит обновление.
            if (_directories.Contains(directory))
            {
                // Do update.

            }
            else
            {
                _directories.Add(directory);
            }

            foreach (var subDirectory in directory.SubDirectories)
            {
                AddDirectory(subDirectory);
            }

            foreach (var file in directory.Files)
            {
                AddFile(file);
            }
        }

        public Directory FindDirectory(Directory parentDirectory, String name)
        {
            Directory directory = null;
            if (_directories.Contains(parentDirectory))
            {
                directory = parentDirectory.SubDirectories
                    .FirstOrDefault(subDir => subDir.Name.Equals(name, StringComparison.InvariantCulture));
            }

            return directory;
        }

        public Directory FindDirectory(String directoryPath)
        {
            Directory directory = _directories
                .FirstOrDefault(dir => dir.Path.Equals(directoryPath, StringComparison.InvariantCulture));

            return directory;
        }

        public void AddDataLocation(DataLocation dataLocation)
        {
            _dataLocations.Add(dataLocation);
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
                    rootDataLocationDirectories.Add(dataLocation.Directory, dataLocation);
                }

                foreach (Directory directory in directories)
                {
                    if (!rootDataLocationDirectories.ContainsKey(directory))
                    {
                        Directory directoryToCheck = directory;
                        while (directoryToCheck != null)
                        {
                            if (rootDataLocationDirectories.TryGetValue(directoryToCheck, out var foundDataLocation))
                            {
                                foundDataLocations.Add(foundDataLocation);
                                break;
                            }
                            else
                            {
                                directoryToCheck = directoryToCheck.ParentDirectory;
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
                    if (directoriesSet.Contains(dataLocation.Directory))
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

        public IReadOnlyCollection<DataLocation> GetDataLocations()
        {
            List<DataLocation> foundDataLocations = _dataLocations.ToList();
            return foundDataLocations;
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

        public HashSet<Inspection> GetLastInspections(IEnumerable<DataLocation> dataLocations)
        {
            var dataLocationsSet = new HashSet<DataLocation>(dataLocations);
            var lastInspections = new Dictionary<DataLocation, Inspection>();

            foreach (Inspection inspection in _inspections)
            {
                DataLocation dataLocation = inspection.DataLocation;
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

        /// <remarks>DataControllerSimple не знает напрямую, какой файл из какой DataLocation. Но при работе с БД эта информация будет через JOIN.</remarks>
        public IReadOnlyCollection<FileInfo> GetDuplicates(IEnumerable<DataLocation> dataLocations)
        {
            var dataLocationSet = new HashSet<DataLocation>(dataLocations);
            HashSet<Inspection> inspectionsSet = GetLastInspections(dataLocationSet);

            var blobGroups = new Dictionary<BlobInfo, HashSet<File>>(); // Все блобы и файлы какие есть.
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
                        blobGroups[blobInfo] = new HashSet<File>();
                    }

                    HashSet<File> duplicateFiles = blobGroups[blobInfo];
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

            var result = new List<FileInfo>();
            foreach (KeyValuePair<BlobInfo, HashSet<File>> blobGroup in blobGroups)
            {
                if (blobGroup.Value.Count > 1)
                {
                    BlobInfo blobInfo = blobGroup.Key;
                    HashSet<File> duplicates = blobGroup.Value;

                    if (duplicates.Count > 1)
                    {
                        HashSet<FileInfo> duplicatesSet = MakeDuplicatesSet(blobInfo, duplicates, dataLocationSet);
                        result.AddRange(duplicatesSet);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Получает файлы в наличии из <paramref name="directories"/>, то есть файлы обнаруженные последней инспекцией на своих местах.
        /// </summary>
        public IReadOnlyCollection<FileInfo> GetDirectoryCurrentFiles(IReadOnlyCollection<Directory> directories, Boolean includeSubDirectories)
        {
            IEnumerable<DataLocation> dataLocations = GetDataLocations(directories, includeUpperDirectories: true);
            HashSet<Inspection> inspectionsSet = GetLastInspections(dataLocations); // Нужна крайняя инспекция той DataLocation, где находится каждая папка.

            // (*) Есть ли директория на месте сейчас мы не знаем, да это тут и не важно.
            //HashSet<Directory> directoriesToProcess = GetDirectoriesSet(directories, includeSubDirectories);

            var directoriesToProcess = new HashSet<Directory>(directories);

            if (includeSubDirectories)
            {
                IReadOnlyCollection<Directory> subDirectories = GetSubDirectories(inspectionsSet, directoriesToProcess, doRecursively: includeSubDirectories);
                foreach (Directory subDirectory in subDirectories)
                {
                    directoriesToProcess.Add(subDirectory);
                }
            }

            var result = new HashSet<FileInfo>();
            foreach (FileState fileState in _fileStates)
            {
                if (!inspectionsSet.Contains(fileState.Inspection))
                {
                    continue;
                }

                File file = fileState.File;
                if (!directoriesToProcess.Contains(file.ParentDirectory))
                {
                    continue;
                }

                BlobInfo blobInfo = fileState.BlobInfo;
                if (blobInfo != null)
                {
                    DataLocation dataLocation = dataLocations.FirstOrDefault(location => IsFileFromDirectory(location.Directory, file)); // TODO: optimize
                    var fileInfo = new FileInfo(fileState.BlobInfo, fileState.File, dataLocation);
                    result.Add(fileInfo);
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

            return result;
        }

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

        public Boolean IsFileFromDirectory(Directory directory, File file)
        {
            Boolean isFileFromDirectory = false;

            if (_files.Contains(file))
            {
                if (file.ParentDirectory != null)
                {
                    FsEntity currentFsEntity = file;
                    while (!isFileFromDirectory && currentFsEntity.ParentDirectory != null)
                    {
                        isFileFromDirectory = directory.Equals(currentFsEntity.ParentDirectory);
                        currentFsEntity = currentFsEntity.ParentDirectory;
                    }
                }

                if (!isFileFromDirectory) // Then check by path
                {
                    String relativePath = FileSystemHelper.GetRelativePath(directory.Path, file.Path);
                    if (!relativePath.Contains(".."))
                    {
                        isFileFromDirectory = !relativePath.Contains(file.Path) && relativePath.Length >= file.Name.Length; // Дибильный temporal hack.
                    }
                }
            }
            else
            {
                throw new ArgumentException("File is unknown."); // TODO
            }

            return isFileFromDirectory;
        }

        #endregion

        #region Private methods

        private HashSet<FileInfo> MakeDuplicatesSet(BlobInfo blobInfo, IEnumerable<File> files, IEnumerable<DataLocation> dataLocations)
        {
            var dataLocationSet = new HashSet<DataLocation>(dataLocations);

            var duplicatesSet = new HashSet<FileInfo>();
            foreach (File file in files)
            {
                DataLocation dataLocation = dataLocationSet.FirstOrDefault(location => IsFileFromDirectory(location.Directory, file)); // TODO: optimize
                var copyInfo = new FileInfo(blobInfo, file, dataLocation);
                duplicatesSet.Add(copyInfo);
            }

            return duplicatesSet;
        }

        #endregion
    }
}
