using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public interface IProgressValue<TValue> : IProgressValue
    {
        void IncrementBy(TValue value);

        Single GetCurrentProgress(TValue lastExpectedValue);
    }

    public interface IProgressValue
    {
        Boolean CheckIsNotNull();

        void Increment();
    }
}
