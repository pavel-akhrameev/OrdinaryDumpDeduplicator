using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public struct IntegerSingleValue : IProgressValue<IntegerSingleValue>, IProgressValue<Int64>
    {
        private Int64 _value;

        public IntegerSingleValue(Int64 value)
        {
            this._value = value;
        }

        public Int64 Value => _value;

        public Boolean CheckIsNotNull()
        {
            Boolean result = _value != 0;
            return result;
        }

        public void Increment()
        {
            _value++;
        }

        public void IncrementBy(Int64 value)
        {
            _value += value;
        }

        public void IncrementBy(IntegerSingleValue value)
        {
            IncrementBy(value._value);
        }

        public Single GetCurrentProgress(Int64 lastExpectedValue)
        {
            var currentProgress = (Single)_value / lastExpectedValue;
            return currentProgress;
        }

        public Single GetCurrentProgress(IntegerSingleValue lastExpectedValue)
        {
            Single currentProgress = GetCurrentProgress(lastExpectedValue._value);
            return currentProgress;
        }

        public override String ToString()
        {
            return _value.ToString();
        }
    }
}
