using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public enum FileStatus
    {
        Unknown = 0,
        New = 1,
        Unchanged = 2,
        Modified = 3,
        Removed = 4,
        Unreadable = 5,
        Error = 6
    }
}
