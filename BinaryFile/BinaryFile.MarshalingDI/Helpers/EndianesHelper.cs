using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Helpers
{
    public static class EndianesHelper
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
        public static bool HasToNormalize(bool? dataIsLittleEndian)
        {
            return ((dataIsLittleEndian ?? false) != BitConverter.IsLittleEndian);
        }
    }
}
