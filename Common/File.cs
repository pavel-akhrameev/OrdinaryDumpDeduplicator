using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public class File : FsEntity
    {
        private readonly ulong _size;
        private Digest _hashValue;

        public File(String name, Directory parentDirectory, DateTime creationDate, DateTime modificationDate)
            : this(name, parentDirectory, 0, creationDate, modificationDate)
        {
        }

        public File(String name, Directory parentDirectory, ulong size, DateTime creationDate, DateTime modificationDate)
            : base(name, parentDirectory, creationDate, modificationDate)
        {
            _size = size;
        }

        public Digest HashValue
        {
            get
            {
                return _hashValue;
            }
            set
            {
                if (_hashValue == null)
                {
                    _hashValue = value;
                }
                else
                {
                    throw new InvalidOperationException("Hash value is already set.");
                }
            }
        }
    }
}
