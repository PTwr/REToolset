using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BinaryDataHelper
{
    public struct Int24
    {
        public static implicit operator int(Int24 value) => value.BackingField;

        public static int MinValue = -8_388_608;
        public static int MaxValue = 8_388_607;

        private int BackingField;
        public Int24(Span<byte> value)
        {
            BackingField = value[0];
            BackingField <<= 8;
            BackingField |= value[1];
            BackingField <<= 8;
            BackingField |= value[2];
        }
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

        public void Write(Span<byte> bytes)
        {
            bytes[0] = (byte)(BackingField & 0xFF);
            bytes[1] = (byte)((BackingField >> 8) & 0xFF);
            bytes[2] = (byte)((BackingField >> 16) & 0xFF);
        }
    }

    public struct UInt24
    {
        public static implicit operator uint(UInt24 value) => value.BackingField;
        public static implicit operator int(UInt24 value) => (int)value.BackingField;

        public static uint MinValue = 0;
        public static int MaxValue = 16_777_216;

        private uint BackingField;
        public UInt24(Span<byte> value)
        {
            BackingField = value[0];
            BackingField <<= 8;
            BackingField |= value[1];
            BackingField <<= 8;
            BackingField |= value[2];
        }
        public UInt24(uint value)
        {
            if (value < MinValue) throw new ArgumentOutOfRangeException($"Value of {value} exceedes minimum of {MinValue} in signed 24bit integer");
            if (value > MaxValue) throw new ArgumentOutOfRangeException($"Value of {value} exceedes maximum of {MaxValue} in signed 24bit integer");

            BackingField = value;
        }
        public UInt24(int value)
            : this((uint)value)
        {
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

        public void Write(Span<byte> bytes)
        {
            bytes[0] = (byte)(BackingField & 0xFF);
            bytes[1] = (byte)((BackingField >> 8) & 0xFF);
            bytes[2] = (byte)((BackingField >> 16) & 0xFF);
        }
    }
}
