using System;
using System.Collections.Generic;
using System.Linq;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public sealed class DuplicateReport
    {
        #region Private fields

        private readonly HashSet<SameContentFilesInfo> _filesToReport;

        private readonly IReadOnlyCollection<DataLocation> _dataLocations;

        private Dictionary<Directory, FileInfo[]> _directoriesWithDuplicates;

        #endregion

        internal DuplicateReport(IReadOnlyCollection<SameContentFilesInfo> filesToReport, IReadOnlyCollection<DataLocation> dataLocations)
        {
            this._filesToReport = new HashSet<SameContentFilesInfo>(filesToReport);
            this._dataLocations = dataLocations;
            this._directoriesWithDuplicates = null;
        }

        #region Public properties

        public IReadOnlyCollection<SameContentFilesInfo> FilesToReport => _filesToReport;

        public IReadOnlyCollection<SameContentFilesInfo> UnprocessedDuplicatesFound
        {
            get
            {
                var unprocessedDuplicates = _filesToReport
                    .Where(sameContentFiles => sameContentFiles.HasUnprocessedDuplicates)
                    .ToArray();

                return unprocessedDuplicates;
            }
        }

        public IReadOnlyCollection<SameContentFilesInfo> AllDuplicatesIsolated
        {
            get
            {
                var allDuplicatesIsolated = _filesToReport
                    .Where(sameContentFiles => sameContentFiles.AllDuplicatesIsolated)
                    .ToArray();

                return allDuplicatesIsolated;
            }
        }

        public IReadOnlyCollection<SameContentFilesInfo> IsolatedFilesOnly
        {
            get
            {
                var isolatedFilesOnly = _filesToReport
                    .Where(sameContentFiles => sameContentFiles.ContainsIsolatedFilesOnly)
                    .ToArray();

                return isolatedFilesOnly;
            }
        }

        public IReadOnlyCollection<DataLocation> DataLocations => _dataLocations;

        #endregion

        #region Public methods

        public Dictionary<Directory, FileInfo[]> GroupDuplicatesByDirectories(Boolean includeIsolatedDuplicates)
        {
            IEnumerable<SameContentFilesInfo> allDuplicatesByHash;
            if (!includeIsolatedDuplicates)
            {
                allDuplicatesByHash = _filesToReport.Where(sameContentFiles => !sameContentFiles.AllDuplicatesIsolated);
            }
            else
            {
                allDuplicatesByHash = _filesToReport;
            }

            if (_directoriesWithDuplicates == null)
            {
                var directoriesWithDuplicates = new Dictionary<Directory, List<FileInfo>>();
                foreach (SameContentFilesInfo fileDuplicates in allDuplicatesByHash)
                {
                    foreach (FileInfo duplicate in fileDuplicates.Duplicates)
                    {
                        Directory directoryOfTheDuplicate = duplicate.File.ParentDirectory;
                        if (!directoriesWithDuplicates.ContainsKey(directoryOfTheDuplicate))
                        {
                            directoriesWithDuplicates[directoryOfTheDuplicate] = new List<FileInfo>();
                        }

                        var directoryWithDuplicates = directoriesWithDuplicates[directoryOfTheDuplicate];
                        directoryWithDuplicates.Add(duplicate);
                    }
                }

                _directoriesWithDuplicates = new Dictionary<Directory, FileInfo[]>(directoriesWithDuplicates.Count);
                foreach (KeyValuePair<Directory, List<FileInfo>> directoryWithDuplicates in directoriesWithDuplicates)
                {
                    _directoriesWithDuplicates.Add(directoryWithDuplicates.Key, directoryWithDuplicates.Value.ToArray());
                }
            }

            return _directoriesWithDuplicates;
        }

        public IReadOnlyCollection<DirectoryWithDuplicates> GetDuplicatesFoundByDirectories(Boolean includeIsolatedDuplicates)
        {
            Dictionary<Directory, FileInfo[]> directoriesAndDuplicates = GroupDuplicatesByDirectories(includeIsolatedDuplicates);

            var directoriesToReport = new Dictionary<Directory, HashSet<Directory>>(); // Собираем все, включая родительские
            foreach (KeyValuePair<Directory, FileInfo[]> directoryWithDuplicates in directoriesAndDuplicates)
            {
                Directory directory = directoryWithDuplicates.Key;
                AddDirectory(directoriesToReport, directory);
            }

            var rootLevelDirectories = new Dictionary<Directory, HashSet<Directory>>(directoriesToReport);
            foreach (KeyValuePair<Directory, HashSet<Directory>> directoryToReport in directoriesToReport)
            {
                foreach (var subDirectoryToReport in directoryToReport.Value)
                {
                    rootLevelDirectories.Remove(subDirectoryToReport);
                }
            }

            var directoriesWithDuplicates = new List<DirectoryWithDuplicates>();
            foreach (KeyValuePair<Directory, HashSet<Directory>> rootLevelDirectory in rootLevelDirectories)
            {
                DirectoryWithDuplicates directoryWithDuplicates = MakeDirectoryWithDuplicates(directoriesToReport, directoriesAndDuplicates, directory: rootLevelDirectory.Key, subDirectories: rootLevelDirectory.Value);
                directoriesWithDuplicates.Add(directoryWithDuplicates);
            }

            return directoriesWithDuplicates;
        }

        #endregion

        internal void AddFileInfo(FileInfo fileInfo)
        {
            SameContentFilesInfo sameContentFilesInfo = _filesToReport.First(sameContentFiles => fileInfo.BlobInfo.Equals(sameContentFiles.BlobInfo));
            sameContentFilesInfo.AddFileInfo(fileInfo);
        }

        internal void RemoveFileInfo(FileInfo fileInfo)
        {
            SameContentFilesInfo sameContentFilesInfo = _filesToReport.First(sameContentFiles => fileInfo.BlobInfo.Equals(sameContentFiles.BlobInfo));
            sameContentFilesInfo.RemoveFileInfo(fileInfo);
        }

        #region Private methods

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

        private static DirectoryWithDuplicates MakeDirectoryWithDuplicates(IReadOnlyDictionary<Directory, HashSet<Directory>> directoriesToReport, IReadOnlyDictionary<Directory, FileInfo[]> directoriesWithDuplicates, Directory directory, HashSet<Directory> subDirectories)
        {
            var subDirectoriesWithDuplicates = new List<DirectoryWithDuplicates>(subDirectories.Count);

            foreach (Directory subDirectory in subDirectories)
            {
                DirectoryWithDuplicates subDirectoryWithDuplicates;
                if (directoriesToReport.TryGetValue(subDirectory, out HashSet<Directory> subSubDirectories))
                {
                    subDirectoryWithDuplicates = MakeDirectoryWithDuplicates(directoriesToReport, directoriesWithDuplicates, subDirectory, subSubDirectories);
                }
                else
                {
                    directoriesWithDuplicates.TryGetValue(subDirectory, out FileInfo[] subDirectoryDuplicatesInfo);
                    subDirectoryWithDuplicates = new DirectoryWithDuplicates(subDirectory, subDirectoriesWithDuplicates: new DirectoryWithDuplicates[] { }, subDirectoryDuplicatesInfo);
                }

                subDirectoriesWithDuplicates.Add(subDirectoryWithDuplicates);
            }

            directoriesWithDuplicates.TryGetValue(directory, out FileInfo[] duplicatesInfo);
            var directoryWithDuplicates = new DirectoryWithDuplicates(directory, subDirectoriesWithDuplicates, duplicatesInfo);

            return directoryWithDuplicates;
        }

        #endregion
    }
}
