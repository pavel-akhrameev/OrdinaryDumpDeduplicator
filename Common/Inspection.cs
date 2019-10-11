using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public class Inspection
    {
        private readonly DataLocation _dataLocation;

        private readonly DateTime _start;

        private DateTime? _finish;

        public Inspection(DataLocation dataLocation, DateTime start, DateTime? end = null)
        {
            this._dataLocation = dataLocation;
            this._start = start;
        }

        public DataLocation DataLocation => _dataLocation;

        public DateTime StartDateTime => _start;

        public DateTime? FinishDateTime => _finish;

        public void FinishInspection(DateTime finish)
        {
            _finish = finish;
        }

        public override String ToString()
        {
            String stringRepresentation = StartDateTime.ToString("dd.MM.yyyy hh.mm.ss");
            return stringRepresentation;
        }
    }
}
