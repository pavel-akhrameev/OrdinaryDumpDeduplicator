using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public interface IFileProcessor
    {
        event EventHandler FileProcessed;

        void AddFileToProcess(Common.File file);
    }
}
