using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public interface IProgressCounter
    {
        Single GetCurrentProgress();

        DateTime GetEstimatedTimeOfAction(DateTime now);
    }
}
