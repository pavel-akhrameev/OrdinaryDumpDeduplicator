using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public struct IntegerDoubleValue : IProgressValue<IntegerDoubleValue>, IProgressValue<Tuple<Int64, Int64>>
    {
        private Int64 _value1;
        private Int64 _value2;

        public IntegerDoubleValue(Int64 value1, Int64 value2)
        {
            this._value1 = value1;
            this._value2 = value2;
        }

        public Int64 Value1 => _value1;

        public Int64 Value2 => _value2;

        public Boolean CheckIsNotNull()
        {
            Boolean result = _value1 != 0 && _value2 != 0;
            return result;
        }

        public void Increment()
        {
            _value1++;
            _value2++;
        }

        public void IncrementBy(Tuple<Int64, Int64> value)
        {
            _value1 += value.Item1;
            _value2 += value.Item2;
        }

        public void IncrementBy(IntegerDoubleValue value)
        {
            var valuesTuple = new Tuple<Int64, Int64>(value._value1, value._value2);
            IncrementBy(valuesTuple);
        }

        public Single GetCurrentProgress(Tuple<Int64, Int64> lastExpectedValue)
        {
            var currentProgress1 = (Single)_value1 / lastExpectedValue.Item1;
            var currentProgress2 = (Single)_value2 / lastExpectedValue.Item2;

            var currentProgress = (Single)Math.Sqrt((Double)currentProgress1 * (Double)currentProgress2); // TODO
            return currentProgress;
        }

        public float GetCurrentProgress(IntegerDoubleValue lastExpectedValue)
        {
            var valuesTuple = new Tuple<Int64, Int64>(lastExpectedValue._value1, lastExpectedValue._value2);

            Single currentProgress = GetCurrentProgress(valuesTuple);
            return currentProgress;
        }

        public override String ToString()
        {
            String stringRepresentation = $"[{_value1.ToString()}; {_value2.ToString()}]";
            return stringRepresentation;
        }
    }
}
