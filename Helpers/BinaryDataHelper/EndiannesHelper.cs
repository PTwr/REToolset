using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryDataHelper
{
    public static class EndiannesHelper
    {
        /// <summary>
        /// Warning! Will affect parent spans as well!
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataIsLittleEndian"></param>
        public static void NormalizeEndiannes(this Span<byte> data, bool? dataIsLittleEndian = false)
        {
            if ((dataIsLittleEndian ?? false) != BitConverter.IsLittleEndian) data.Reverse();
        }
        /// <summary>
        /// Warning! Will affect parent spans as well!
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataIsLittleEndian"></param>
        public static void NormalizeEndiannes(this Memory<byte> data, bool? dataIsLittleEndian = false)
        {
            if ((dataIsLittleEndian ?? false) != BitConverter.IsLittleEndian)
            {
                data.Span.Reverse();
            }
        }

        /// <summary>
        /// Will return copy of data if reversing is needed.
        /// Performance will thus suck :)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataIsLittleEndian"></param>
        /// <returns></returns>
        public static Span<byte> NormalizeEndiannesInCopy(this Span<byte> data, bool? dataIsLittleEndian = false)
        {
            if ((dataIsLittleEndian ?? false) != BitConverter.IsLittleEndian)
            {
                data = data.ToArray().AsSpan();
                data.Reverse();
                return data;
            }
            return data;
        }
        /// <summary>
        /// Will return copy of data if reversing is needed.
        /// Performance will thus suck :)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataIsLittleEndian"></param>
        /// <returns></returns>
        public static Memory<byte> NormalizeEndiannesInCopy(this Memory<byte> data, bool? dataIsLittleEndian = false)
        {
            if ((dataIsLittleEndian ?? false) != BitConverter.IsLittleEndian)
            {
                data =  data.ToArray().Reverse().ToArray().AsMemory();
                return data;
            }
            return data;
        }

        public static bool HasToNormalize(bool? dataIsLittleEndian)
        {
            return ((dataIsLittleEndian ?? false) != BitConverter.IsLittleEndian);
        }
    }
}
