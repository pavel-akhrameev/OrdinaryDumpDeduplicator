using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public sealed class File : FsEntity
    {
        public File(String name, Directory parentDirectory) : base(name, parentDirectory)
        {
        }
    }
}
