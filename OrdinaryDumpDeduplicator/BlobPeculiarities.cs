using System;

namespace OrdinaryDumpDeduplicator
{
    [Flags]
    public enum BlobPeculiarities
    {
        None = 0,
        IsUnique = 4,
        HasNoOriginalLocatedFiles = 16,
        HasOriginalLocatedFiles = 32,
        HasUnprocessedDuplicates = 64,
        HasIsolatedDuplicates = 128,

        AllDuplicatesIsolated = HasOriginalLocatedFiles | HasIsolatedDuplicates, // Флаги IsUnique и HasUnprocessedDuplicates не установлены
        ContainsIsolatedFilesOnly = HasNoOriginalLocatedFiles | HasIsolatedDuplicates, // Флаги HasOriginalFile и HasUnprocessedDuplicates не установлены
        HasPartiallyIsolatedDuplicates = HasOriginalLocatedFiles | HasUnprocessedDuplicates | HasIsolatedDuplicates
    }
}
