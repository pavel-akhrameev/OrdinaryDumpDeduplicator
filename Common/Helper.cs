using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public static class Helper
    {
        public static String GetDataSizeString(Int64 dataSizeInBytes)
        {
            const Int32 bytesInKibibyte = 1024;

            Double precisePowerOfKibibyte = Math.Log(dataSizeInBytes, bytesInKibibyte);
            var powerOfKibibyte = (Int16)Math.Floor(precisePowerOfKibibyte);

            String unitString;
            switch (powerOfKibibyte)
            {
                case 0:
                    unitString = "bytes";
                    break;
                case 1:
                    unitString = "KiB";
                    break;
                case 2:
                    unitString = "MiB";
                    break;
                case 3:
                    unitString = "GiB";
                    break;
                case 4:
                    unitString = "TiB";
                    break;
                //case > 4:
                default:
                    powerOfKibibyte = 5;
                    unitString = "PiB";
                    break;
            }

            Int64 denominator = (Int64)Math.Pow(bytesInKibibyte, powerOfKibibyte);
            Double preciseValue = (Double)dataSizeInBytes / denominator;
            Int16 value = (Int16)Math.Round(preciseValue);

            String dataSizeString = $"{value} {unitString}";
            return dataSizeString;
        }
    }
}
