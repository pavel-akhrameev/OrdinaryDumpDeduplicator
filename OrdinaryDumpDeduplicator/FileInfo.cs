using System;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public class FileInfo
    {
        #region Private fields

        private readonly BlobInfo _blobInfo;
        private readonly File _file;
        private readonly DataLocation _dataLocation;

        private SameContentFilesInfo _sameContentFilesInfo;
        private DuplicateSort _duplicateSort;

        #endregion

        internal FileInfo(BlobInfo blobInfo, File file, DataLocation dataLocation)
        {
            this._blobInfo = blobInfo;
            this._file = file;
            this._dataLocation = dataLocation;
        }

        #region Public properties

        public SameContentFilesInfo SameContentFilesInfo => _sameContentFilesInfo;

        public BlobInfo BlobInfo
        {
            get
            {
                if (_sameContentFilesInfo != null)
                {

                    return _sameContentFilesInfo.BlobInfo;
                }
                else
                {
                    return _blobInfo;
                }
            }
        }

        public File File => _file;

        public DataLocation DataLocation => _dataLocation;

        public DuplicateSort Sort => _duplicateSort;

        public Boolean IsBlobTotallyIsolated
        {
            get
            {
                Boolean isolatedFilesOnly = _sameContentFilesInfo.Peculiarities.HasFlag(BlobPeculiarities.ContainsIsolatedFilesOnly);
                return isolatedFilesOnly;
            }
        }

        #endregion

        public override String ToString()
        {
            String fileRepresentation = $"{_file.Name} | {_file.ParentDirectory.Path}";
            return fileRepresentation;
        }

        internal void SetSameContentFiles(SameContentFilesInfo sameContentFilesInfo)
        {
            if (!_blobInfo.Equals(sameContentFilesInfo.BlobInfo))
            {
                throw new ArgumentException("", nameof(sameContentFilesInfo)); // TODO: exception
            }

            _sameContentFilesInfo = sameContentFilesInfo;
        }

        internal void SetDuplicateSort(DuplicateSort sort)
        {
            _duplicateSort = sort;
        }
    }
}
