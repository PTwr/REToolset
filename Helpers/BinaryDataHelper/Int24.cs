using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BinaryDataHelper
{
    //TODO test
    public struct Int24
    {
        public static int MinValue = -8_388_608;
        public static int MaxValue = 8_388_607;

        private int BackingField;
        public Int24(int value)
        {
            if (value < MinValue) throw new ArgumentOutOfRangeException($"Value of {value} exceedes minimum of {MinValue} in signed 24bit integer");
            if (value > MaxValue) throw new ArgumentOutOfRangeException($"Value of {value} exceedes maximum of {MaxValue} in signed 24bit integer");

            BackingField = value;
        }

        public Span<byte> ToBytes(bool? littleEndian = false)
        {
            var bytes = new byte[3]
            {
                (byte)(BackingField & 0xFF),
                (byte)((BackingField >> 8) & 0xFF),
                (byte)((BackingField >> 16) & 0xFF),
            }.AsSpan();

            if ((littleEndian ?? false) != BitConverter.IsLittleEndian) bytes.Reverse();

            return bytes;
        }
    }

    //TODO test
    public struct UInt24
    {
        public static uint MinValue = 0;
        public static int MaxValue = 16_777_216;

        private uint BackingField;
        public UInt24(uint value)
        {
            if (value < MinValue) throw new ArgumentOutOfRangeException($"Value of {value} exceedes minimum of {MinValue} in signed 24bit integer");
            if (value > MaxValue) throw new ArgumentOutOfRangeException($"Value of {value} exceedes maximum of {MaxValue} in signed 24bit integer");

            BackingField = value;
        }

        public Span<byte> ToBytes(bool? littleEndian = false)
        {
            var bytes = new byte[3]
            {
                (byte)(BackingField & 0xFF),
                (byte)((BackingField >> 8) & 0xFF),
                (byte)((BackingField >> 16) & 0xFF),
            }.AsSpan();

            if ((littleEndian ?? false) != BitConverter.IsLittleEndian) bytes.Reverse();

            return bytes;
        }
    }
}
