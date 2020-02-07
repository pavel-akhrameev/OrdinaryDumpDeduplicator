using System;

namespace OrdinaryDumpDeduplicator
{
    [Flags]
    public enum ObjectSort
    {
        None = 0,
        Blob = 1,
        FileSpecimen = 2,

        IsUnique = 4, // for File
        InOriginalLocation = 8, // for File
        IsolatedDuplicate = 16, // for File

        HasOriginalFiles = 32, // for Blob
        HasUnprocessedDuplicates = 64, // for Blob
        HasIsolatedDuplicates = 128, // for Blob

        AllDuplicatesIsolated = Blob | HasOriginalFiles | HasIsolatedDuplicates, // TODO: и флаг HasUnprocessedDuplicates не установлен
        ContainsUniqueIsolatedFiles = Blob | HasIsolatedDuplicates // TODO: флаги HasOriginalFile и HasUnprocessedDuplicates не установлены

        // 8 bit - есть изолированные дубликаты или нет.
        // 7 bit - есть необработанные дубликаты или нет.
        // 6 bit - есть хотя бы один такой файл в DataLocation. Если таких много - то это необработанные дубликаты.
    }
}
