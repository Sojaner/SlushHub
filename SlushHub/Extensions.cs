using System;

namespace SlushHub
{
    internal static class Extensions
    {
        public static T LimitTo<T>(this T value, T minimum, T maximum) where T : struct
        {
            double dValue = (double)Convert.ChangeType(value, TypeCode.Double);

            double dMinimum = (double)Convert.ChangeType(minimum, TypeCode.Double);

            double dMaximum = (double)Convert.ChangeType(maximum, TypeCode.Double);

            if (dValue >= dMinimum && dValue <= dMaximum)
            {
                return value;
            }

            return dValue > dMaximum ? maximum : minimum;
        }
    }
}
