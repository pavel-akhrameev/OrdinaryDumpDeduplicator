using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public class DataProcessor : AbstractWorker, IDirectoryProcessor, IFileProcessor
    {
        private readonly ConcurrentQueue<Common.File> _filesToProcessing = new ConcurrentQueue<Common.File>();
        private readonly ConcurrentQueue<Common.Directory> _foldersToProcessing = new ConcurrentQueue<Common.Directory>();

        private readonly HashSet<Common.File> _enqueuedFiles = new HashSet<Common.File>();
        private readonly HashSet<Common.File> _processedFiles = new HashSet<Common.File>();

        public event EventHandler DirectoryProcessed;
        public event EventHandler FileProcessed;

        public void AddDirectoryToProcess(Directory directory)
        {
            // TODO

            throw new NotImplementedException();
        }

        /// <remarks>Так нельзя. Будет очень много тасков, по колличеству файлов в очереди.</remarks>
        public void AddFileToProcess(Common.File file)
        {
            var isFileProcessed = _processedFiles.Contains(file);
            if (isFileProcessed)
            {
                // Сразу вызвать событие. Событие будет вызвано в потоке, который вызвал метод AddFileToProcess.
                if (FileProcessed != null)
                {
                    FileProcessed.Invoke(null, EventArgs.Empty); // TODO: передать результат.
                }
            }
            else
            {
                bool isFileEnqueued;
                lock (_enqueuedFiles)
                {
                    isFileEnqueued = _enqueuedFiles.Contains(file);
                }

                if (!isFileEnqueued)
                {
                    _enqueuedFiles.Add(file);
                }
                else
                {
                    // Nothing to do here.
                }
            }
        }

        protected override void PerformOperation()
        {
            // TODO

            if (FileProcessed != null)
            {
                FileProcessed.Invoke(null, EventArgs.Empty); // TODO: передать результат.
            }
        }

        protected override void PrepareToWork()
        {
            // Nothing to do here.
        }

        protected override void Stop()
        {
            // TODO: прервать текущую операцию.
        }
    }
}
