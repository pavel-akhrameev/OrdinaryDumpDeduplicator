using System;
using System.Collections.Generic;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public class SameContentFilesInfo : IEquatable<SameContentFilesInfo>
    {
        private readonly BlobInfo _blobInfo;
        private readonly IReadOnlyCollection<FileInfo> _duplicates;
        private readonly Dictionary<DataLocation, HashSet<Directory>> _dataLocationsWithDuplicates;
        private readonly Dictionary<Directory, List<File>> _directoriesWithDuplicates;

        private BlobPeculiarities _blobPeculiarities;

        internal SameContentFilesInfo(BlobInfo blobInfo, IReadOnlyCollection<FileInfo> duplicates)
        {
            this._blobInfo = blobInfo;
            this._duplicates = duplicates;

            // MakeFilesAndDirectoriesDictionary(_duplicates, out this._dataLocationsWithDuplicates, out this._directoriesWithDuplicates);

            foreach (FileInfo fileInfo in this._duplicates)
            {
                fileInfo.SetSameContentFiles(this);
            }
        }

        public BlobInfo BlobInfo => _blobInfo;

        public IReadOnlyCollection<FileInfo> Duplicates => _duplicates;

        public Int64 AllDataSize
        {
            get
            {
                Int64 allDataSize = _blobInfo.Size * _duplicates.Count;
                return allDataSize;
            }
        }

        public Int64 DuplicatesDataSize
        {
            get
            {
                Int32 originalLocatedFilesCount = 0;
                foreach (var duplicate in _duplicates)
                {
                    if (duplicate.Sort == DuplicateSort.InOriginalLocation)
                    {
                        originalLocatedFilesCount++;
                    }
                }

                Int64 duplicatesDataSize = _blobInfo.Size * Math.Max(originalLocatedFilesCount - 1, 0);
                return duplicatesDataSize;
            }
        }

        public Int64 AllDuplicatesDataSize
        {
            get
            {
                Int64 duplicatesDataSize = _blobInfo.Size * Math.Max(_duplicates.Count - 1, 0);
                return duplicatesDataSize;
            }
        }

        public BlobPeculiarities Peculiarities
        {
            get
            {
                if (_blobPeculiarities == BlobPeculiarities.None)
                {
                    _blobPeculiarities = AnalyzeDuplicates(_duplicates);
                }

                return _blobPeculiarities;
            }
        }

        public Boolean AllDuplicatesIsolated
        {
            get
            {
                Boolean allDuplicatesIsolated = this.Peculiarities.HasFlag(BlobPeculiarities.AllDuplicatesIsolated) && !this.Peculiarities.HasFlag(BlobPeculiarities.HasUnprocessedDuplicates);
                return allDuplicatesIsolated;
            }
        }

        public Boolean ContainsIsolatedFilesOnly
        {
            get
            {
                Boolean containsIsolatedFilesOnly = this.Peculiarities.HasFlag(BlobPeculiarities.ContainsIsolatedFilesOnly);
                return containsIsolatedFilesOnly;
            }
        }

        #region Overrides of object

        public override Boolean Equals(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            SameContentFilesInfo other = obj as SameContentFilesInfo;
            return Equals(other);
        }

        public Boolean Equals(SameContentFilesInfo other)
        {
            if (_blobInfo == null)
            {
                return false;
            }
            else if (other == null)
            {
                return false;
            }
            else
            {
                return _blobInfo.Equals(other.BlobInfo);
            }
        }

        public override Int32 GetHashCode()
        {
            if (_blobInfo == null)
            {
                return 0;
            }
            else
            {
                return _blobInfo.GetHashCode();
            }
        }

        public override string ToString()
        {
            String dataSizeString = Helper.GetDataSizeString(_blobInfo.Size);

            return $"{dataSizeString} x {_duplicates.Count} | {_blobInfo.HexString}";
        }

        #endregion

        #region Private methods

        private static void MakeFilesAndDirectoriesDictionary(
            IReadOnlyCollection<FileInfo> duplicates,
            out Dictionary<DataLocation, HashSet<Directory>> dataLocationsWithDuplicates,
            out Dictionary<Directory, List<File>> directoriesWithDuplicates)
        {
            dataLocationsWithDuplicates = new Dictionary<DataLocation, HashSet<Directory>>();
            directoriesWithDuplicates = new Dictionary<Directory, List<File>>();

            foreach (var duplicateInfo in duplicates)
            {
                DataLocation dataLocation = duplicateInfo.DataLocation;
                Directory directory = duplicateInfo.File.ParentDirectory;

                if (!dataLocationsWithDuplicates.TryGetValue(dataLocation, out HashSet<Directory> directoriesWithDuplicatesInDataLocation))
                {
                    directoriesWithDuplicatesInDataLocation = new HashSet<Directory>();
                    dataLocationsWithDuplicates.Add(dataLocation, directoriesWithDuplicatesInDataLocation);
                }

                directoriesWithDuplicatesInDataLocation.Add(directory);

                if (!directoriesWithDuplicates.TryGetValue(directory, out List<File> duplicatesInDirectory))
                {
                    duplicatesInDirectory = new List<File>();
                    directoriesWithDuplicates.Add(directory, duplicatesInDirectory);
                }

                duplicatesInDirectory.Add(duplicateInfo.File);
            }
        }

        private static BlobPeculiarities AnalyzeDuplicates(IEnumerable<FileInfo> duplicates)
        {
            Int32 filesOnOriginalLocations = 0;
            Int32 isolatedDuplicatesCount = 0;

            foreach (FileInfo duplicateInfo in duplicates)
            {
                if (duplicateInfo.Sort.HasFlag(DuplicateSort.InOriginalLocation))
                {
                    filesOnOriginalLocations++;
                }

                if (duplicateInfo.Sort.HasFlag(DuplicateSort.IsolatedDuplicate))
                {
                    isolatedDuplicatesCount++;
                }
            }

            BlobPeculiarities blobPeculiarities = filesOnOriginalLocations > 0
                ? BlobPeculiarities.HasOriginalLocatedFiles
                : BlobPeculiarities.HasNoOriginalLocatedFiles;

            if (isolatedDuplicatesCount > 0)
            {
                blobPeculiarities = blobPeculiarities | BlobPeculiarities.HasIsolatedDuplicates;
            }

            if (filesOnOriginalLocations > 1)
            {
                blobPeculiarities = blobPeculiarities | BlobPeculiarities.HasUnprocessedDuplicates;
            }

            if (filesOnOriginalLocations + isolatedDuplicatesCount == 1) // File is unique
            {
                blobPeculiarities = blobPeculiarities | BlobPeculiarities.IsUnique;
            }

            return blobPeculiarities;
        }

        #endregion
    }
}
