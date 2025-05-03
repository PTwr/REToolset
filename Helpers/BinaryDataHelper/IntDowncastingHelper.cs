using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryDataHelper
{
    public static class IntDowncastingHelper
    {
        //TODO make use of IMinMaxValue present on numerical types?
        public static ushort ToUshort(this int value, bool throwOnOutOfRange = true)
        {
            if (value < ushort.MinValue) throw new ArgumentOutOfRangeException($"Value above ushort.MinValue  of '{ushort.MinValue}'! Value = '{value}'");
            if (value > ushort.MaxValue) throw new ArgumentOutOfRangeException($"Value above ushort.MaxValue  of '{ushort.MaxValue}'! Value = '{value}'");

            return (ushort)value;
        }
    }
}
