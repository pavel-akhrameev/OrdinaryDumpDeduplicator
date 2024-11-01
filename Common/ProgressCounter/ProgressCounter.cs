using System;
using System.Collections.Generic;

namespace OrdinaryDumpDeduplicator.Common
{
    public class OperationProgressCounter<TValue> : IProgressCounter
        where TValue : struct, IProgressValue<TValue>
    {
        #region Private fields

        private readonly List<ProgressRecord<TValue>> _progressRecords;

        private ProgressRecord<TValue> _currentProgressRecord;
        private IProgressCounter[] _childProgressCounters;

        private TValue _currentValue;
        private TValue _lastExpectedValue;
        private Boolean _isOperationFinished;

        #endregion

        #region Public constructors

        public OperationProgressCounter(TValue initialValue, TValue lastExpectedValue, DateTime startTime) : this()
        {
            Initialize(initialValue, lastExpectedValue, startTime);
        }

        private OperationProgressCounter()
        {
            this._progressRecords = new List<ProgressRecord<TValue>>();
        }

        public static OperationProgressCounter<TValue> Create()
        {
            var newProgressCounter = new OperationProgressCounter<TValue>();
            return newProgressCounter;
        }

        public void Initialize(TValue initialValue, TValue lastExpectedValue, DateTime startTime)
        {
            if (!lastExpectedValue.CheckIsNotNull())
            {
                throw new ArgumentException("", "lastExpectedValue");
            }

            this._childProgressCounters = new IProgressCounter[] { };

            this._currentValue = initialValue;
            this._lastExpectedValue = lastExpectedValue;
            this._isOperationFinished = false;

            AddNewRecord(startTime);
        }

        public void Initialize(params IProgressCounter[] progressCounters)
        {
            this._childProgressCounters = new IProgressCounter[] { }; // TODO
        }

        #endregion

        #region Public properties

        public Boolean IsOperationFinished => _isOperationFinished;

        public String CurrentState => ToString();

        #endregion

        #region Public methods

        public void IncrementValue()
        {
            _currentValue.Increment();
        }

        public void IncrementValue(DateTime now)
        {
            IncrementValue();

            AddNewRecord(now);
        }

        public void IncrementByValue(TValue value)
        {
            _currentValue.IncrementBy(value);
        }

        public void IncrementByValue(TValue value, DateTime now)
        {
            IncrementByValue(value);

            AddNewRecord(now);
        }

        public void SetNewValue(TValue value)
        {
            _currentValue = value;
        }

        public void SetNewValue(TValue value, DateTime now)
        {
            SetNewValue(value);
            AddNewRecord(now);
        }

        public void FinishOperation(DateTime finishTime)
        {
            _isOperationFinished = true;
        }

        public Single GetCurrentProgress()
        {
            Single currentProgress = _currentValue.GetCurrentProgress(_lastExpectedValue);
            return currentProgress;
        }

        public DateTime GetEstimatedTimeOfAction(DateTime now)
        {
            var lastProgressRecordIndex = _progressRecords.Count - 1;
            var lastProgressRecord = _progressRecords[lastProgressRecordIndex];

            return DateTime.Now; // TODO
        }

        public override String ToString()
        {
            Single currentProgress = GetCurrentProgress();
            Single currentProgressInPercent = currentProgress * 100;

            var stringRepresentation = $"{currentProgressInPercent:0.0#}% ({_currentValue.ToString()}/{_lastExpectedValue.ToString()})";
            return stringRepresentation;
        }

        #endregion

        private void AddNewRecord(TValue value, DateTime now)
        {
            _currentProgressRecord = new ProgressRecord<TValue>(value, now);
            _progressRecords.Add(_currentProgressRecord);
        }

        private void AddNewRecord(DateTime now)
        {
            AddNewRecord(_currentValue, now);
        }
    }
}
