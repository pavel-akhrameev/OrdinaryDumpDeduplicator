using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public class OrdinaryDumpDeduplicator
    {
        private readonly DuplicatesProcessor _duplicatesProcessor;
        private readonly DataControllerSimple _dataController;
        private readonly FileSystemProvider _fileSystemProvider;

        private DataLocation _currentDataLocation;

        #region Constructor and initializer

        public OrdinaryDumpDeduplicator()
        {
            this._dataController = new DataControllerSimple();
            this._fileSystemProvider = new FileSystemProvider();
            this._duplicatesProcessor = new DuplicatesProcessor(_dataController, _fileSystemProvider);
            this._currentDataLocation = null;
        }

        public void Initialize()
        {
            _dataController.Initialize();
        }

        #endregion

        #region Public methods

        public IReadOnlyCollection<DataLocation> AddDataLocation(String directoryPath)
        {
            _fileSystemProvider.CheckPathValid(directoryPath);

            var directory = _fileSystemProvider.GetDirectoryInfo(directoryPath, parentDirectory: null);
            _dataController.AddDirectory(directory);

            DataLocation newDataLocation = GetOrCreateDataLocation(directory);
            IReadOnlyCollection<DataLocation> dataLocations = _dataController.GetDataLocations();

            return dataLocations;
        }

        public Task<DataLocation> DoInspection(DataLocation dataLocation)
        {
            Task<DataLocation> doInspectionTask = Task.Run(() => DoInspectionInternal(dataLocation));
            return doInspectionTask;
        }

        public Task<DuplicateReport> GetDuplicatesFound(IReadOnlyCollection<DataLocation> dataLocations)
        {
            Task<DuplicateReport> getDuplicatesTask = Task.Run(() => _duplicatesProcessor.GetDuplicatesFound(dataLocations));
            return getDuplicatesTask;
        }

        public IDictionary<FileInfo, FileInfo> MoveDuplicatesToSpecialFolder(DuplicateReport duplicateReport, IReadOnlyCollection<FileInfo> duplicatesToMove)
        {
            IDictionary<FileInfo, FileInfo> movedFilesInfo = _duplicatesProcessor.MoveDuplicatesToSpecialFolder(duplicateReport, duplicatesToMove);
            return movedFilesInfo;
        }

        public IReadOnlyCollection<FileInfo> DeleteDuplicates(DuplicateReport duplicateReport, IReadOnlyCollection<FileInfo> duplicatesToDelete)
        {
            IReadOnlyCollection<FileInfo> removedDuplicatesInfo = _duplicatesProcessor.DeleteDuplicates(duplicateReport, duplicatesToDelete);
            return removedDuplicatesInfo;
        }

        #endregion

        #region Private methods

        private DataLocation DoInspectionInternal(DataLocation dataLocation)
        {
            _currentDataLocation = null;
            var inspection = new Inspection(dataLocation, DateTime.Now);
            _dataController.AddInspection(inspection);

            FsUtils.IndexSubDirectoriesAndFiles(
               dataLocation.Path,
               out Directory rootDirectory,
               out IReadOnlyCollection<Directory> fsFolders,
               out IReadOnlyCollection<File> fsFiles);

            _dataController.AddDirectory(rootDirectory); // Тут просто сохраняем структуру папок-файлов в БД.

            // Собрать атрибуты всех файлов, включая размер. Сохранить FileState в статусе Unknown или Error (если не получилось доcтать атрибуты).
            IReadOnlyCollection<FileState> statesOfFiles = GetAttributesOfFiles(fsFiles, inspection);

            //CountFilesAndSize(statesOfFiles, out Int32 filesCount, out Int64 dataSize);

            // Получить и сохранить хеши всех файлов. Обновить FileState до New.
            ComputeHashesOfFiles(statesOfFiles);

            inspection.FinishInspection(DateTime.Now);
            _dataController.UpdateInspection(inspection);

            _currentDataLocation = dataLocation;
            return _currentDataLocation;
        }

        private void ComputeHashesOfFiles(IReadOnlyCollection<FileState> statesOfFiles)
        {
            foreach (FileState fileState in statesOfFiles)
            {
                FileStatus fileStatus;
                BlobInfo blobInfo;

                try
                {
                    blobInfo = ComputeAndSaveBlobInfo(fileState.File.Path);
                    fileStatus = FileStatus.New;
                }
                catch (Exception exception)
                {
                    blobInfo = BlobInfo.BrokenBlobInfo;
                    fileStatus = FileStatus.Unreadable;

                    String exceptionMessage = exception.Message;
                }

                fileState.SetStatusAndBlobInfo(fileStatus, blobInfo);
                _dataController.UpdateFileState(fileState);
            }
        }

        private BlobInfo ComputeAndSaveBlobInfo(String filePath)
        {
            BlobInfo blobInfo;

            try
            {
                Byte[] sha1HashSum;
                Int64 fileLength;
                using (System.IO.FileStream fileStream = _fileSystemProvider.GetFileStream(filePath))
                {
                    sha1HashSum = FsUtils.ComputeSha1Hash(fileStream, out fileLength);
                }

                blobInfo = BlobInfo.Create(fileLength, sha1HashSum);
            }
            catch (Exception exception)
            {
                blobInfo = BlobInfo.BrokenBlobInfo;

                String exceptionMessage = exception.Message;
            }

            _dataController.AddBlobInfo(blobInfo);
            return blobInfo;
        }

        private IReadOnlyCollection<FileState> GetAttributesOfFiles(IReadOnlyCollection<File> files, Inspection inspection)
        {
            var result = new List<FileState>(files.Count);

            foreach (File file in files)
            {
                FileState fileState;

                try
                {
                    System.IO.FileInfo fileInfo = _fileSystemProvider.GetFileInfo(file);

                    var status = FileStatus.Unknown;
                    fileState = new FileState(file, inspection, previousState: null, fileInfo.Length, status, fileInfo.CreationTime, fileInfo.LastWriteTime);
                }
                catch (Exception ex)
                {
                    // TODO: log exception.

                    var blobInfo = BlobInfo.BrokenBlobInfo;
                    var status = FileStatus.Error;
                    var currentDateTime = DateTime.Now;
                    fileState = new FileState(file, inspection, previousState: null, size: -1, status, creationDate: currentDateTime, modificationDate: currentDateTime, blobInfo);
                }

                _dataController.AddFileState(fileState);
                result.Add(fileState);
            }

            return result;
        }

        private DataLocation GetOrCreateDataLocation(Directory directory)
        {
            DataLocation dataLocation;
            IReadOnlyCollection<DataLocation> dataLocations = _dataController.GetDataLocations(new[] { directory });
            if (dataLocations.Count > 0)
            {
                dataLocation = dataLocations.First();
            }
            else
            {
                dataLocation = new DataLocation(directory);
                _dataController.AddDataLocation(dataLocation);
            }

            return dataLocation;
        }

        /// <remarks>Counts only available files.</remarks>
        private void CountFilesAndSize(IReadOnlyCollection<FileState> statesOfFiles, out Int32 filesCount, out Int64 dataSize)
        {
            filesCount = 0;
            dataSize = 0;
            foreach (FileState fileState in statesOfFiles)
            {
                if (fileState.Status != FileStatus.Error &&
                    fileState.Status != FileStatus.Unreadable &&
                    fileState.Size > 0)
                {
                    filesCount++;
                    dataSize += fileState.Size;
                }
            }
        }

        #endregion
    }
}
