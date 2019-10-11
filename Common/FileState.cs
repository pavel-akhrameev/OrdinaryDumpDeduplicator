using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public sealed class FileState
    {
        private readonly File _file;
        private readonly Inspection _inspection;

        private readonly Int64 _size;
        private readonly DateTime _creationDate;
        private readonly DateTime _modificationDate;

        private FileState _previousState;
        private FileStatus _status;
        private BlobInfo _blobInfo;

        public FileState(File file, Inspection inspection, FileState previousState, Int64 size, FileStatus status, DateTime creationDate, DateTime modificationDate, BlobInfo blobInfo)
        {
            this._file = file;
            this._inspection = inspection;
            this._previousState = previousState;
            this._size = size;
            this._status = status;
            this._creationDate = creationDate;
            this._modificationDate = modificationDate;
            this._blobInfo = blobInfo;
        }

        public FileState(File file, Inspection inspection, FileState previousState, Int64 size, FileStatus status, DateTime creationDate, DateTime modificationDate)
            : this(file, inspection, previousState, size, status, creationDate, modificationDate, blobInfo: null)
        {
            // TODO: check status value here.
        }

        public File File => _file;

        public Inspection Inspection => _inspection;

        public FileState PreviousState => _previousState;

        public Int64 Size => _size;

        public FileStatus Status => _status;

        public DateTime DateOfCreation => _creationDate;

        public DateTime DateOfLastModification => _modificationDate;

        public BlobInfo BlobInfo => _blobInfo;

        public void SetStatusAndBlobInfo(FileStatus status, BlobInfo blobInfo)
        {
            this._status = status;
            this._blobInfo = blobInfo;
        }

        public void SetPreviousFileState(FileState previousState)
        {
            this._previousState = previousState;
        }
    }
}
