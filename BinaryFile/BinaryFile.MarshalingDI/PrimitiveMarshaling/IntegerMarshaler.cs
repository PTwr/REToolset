using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Helpers;
using BinaryFile.MarshalingDI.Marshaling;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.PrimitiveMarshaling
{
    //TODO rest of int sizes
    public class IntegerMarshaler :
        BaseMarshaler,
        IFullMarshaler<byte>,
        //IReadWriteMarshaler<sbyte>
        IFullMarshaler<ushort>,
        IFullMarshaler<short>,
        //IReadWriteMarshaler<UInt32>
        IFullMarshaler<Int32>
    {
        public IntegerMarshaler(IDataBuffer dataBuffer, IOffsetStack offsetStack, IHierarchicalFeatureSet features) : base(dataBuffer, offsetStack, features)
        {
        }

        public int Order(EMarshalingType marshalingType) => 0;

        byte IReadMarshaler<byte>.Read(out int bytesRead)
            => Deserialize<byte>(out bytesRead);

        void IWriteMarshaler<byte>.Write(byte value, out int bytesWrote)
            => Serialize<byte>(value, out bytesWrote);

        int IReadMarshaler<Int32>.Read(out int bytesRead)
            => Deserialize<Int32>(out bytesRead);

        void IWriteMarshaler<Int32>.Write(int value, out int bytesWrote)
            => Serialize<Int32>(value, out bytesWrote);

        ushort IReadMarshaler<ushort>.Read(out int bytesRead)
            => Deserialize<ushort>(out bytesRead);

        void IWriteMarshaler<ushort>.Write(ushort value, out int bytesWrote)
            => Serialize<ushort>(value, out bytesWrote);

        short IReadMarshaler<short>.Read(out int bytesRead)
            => Deserialize<short>(out bytesRead);

        void IWriteMarshaler<short>.Write(short value, out int bytesWrote)
            => Serialize<short>(value, out bytesWrote);

        private T Deserialize<T>(out int bytesRead)
            where T : struct
        {
            bytesRead = Marshal.SizeOf<T>();

            if (dataBuffer.Length < bytesRead) throw new Exception($"Data length of {dataBuffer.Length} not enough to read {typeof(T).FullName} of size {bytesRead}. {features.GetDebugInfo()}");

            var slice = dataBuffer.AsSpan(offsetStack.CurrentAbsoluteOffset, bytesRead);

            //dont waste effort reversing single bytes :)
            //do not modify original data in case it is being re-read later on
            if (bytesRead > 1) slice = slice.NormalizeEndiannesInCopy(features.IsLittleEndian());

            var result = MemoryMarshal.Read<T>(slice);
            return result;
        }
        private void Serialize<T>(T value, out int bytesWrote)
            where T : struct
        {
            bytesWrote = Marshal.SizeOf<T>();
            var slice = dataBuffer.AsSpan(offsetStack.CurrentAbsoluteOffset, bytesWrote);

            MemoryMarshal.Write(slice, value);

            //dont waste effort reversing single bytes :)
            if (bytesWrote > 1) slice.NormalizeEndiannes(features.IsLittleEndian());
        }
    }
}
