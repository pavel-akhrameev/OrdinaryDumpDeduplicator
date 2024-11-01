using System;

namespace OrdinaryDumpDeduplicator.Common
{
    internal struct ProgressRecord<TProgressValue>
    {
        private TProgressValue _value;
        private DateTime _dateTime;

        public ProgressRecord(TProgressValue value, DateTime now)
        {
            this._value = value;
            this._dateTime = now;
        }

        public TProgressValue Value => _value;

        public DateTime DateTime => _dateTime;
    }
}
