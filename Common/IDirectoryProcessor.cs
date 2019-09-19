using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public interface IDirectoryProcessor
    {
        event EventHandler DirectoryProcessed;

        void AddDirectoryToProcess(Common.Directory directory);
    }
}
