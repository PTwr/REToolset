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
        IFullMarshaler<byte>,
        //IReadWriteMarshaler<sbyte>
        //IReadWriteMarshaler<UInt32>
        IFullMarshaler<Int32>
    {
        public int Order => 0;

        byte IReadMarshaler<byte>.Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            => Deserialize<byte>(data, out bytesRead, metadata, offsetStack);

        void IWriteMarshaler<byte>.Write(byte value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            => Serialize<byte>(value, data, out bytesRead, metadata, offsetStack);

        int IReadMarshaler<Int32>.Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            => Deserialize<Int32>(data, out bytesRead, metadata, offsetStack);

        void IWriteMarshaler<Int32>.Write(int value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            => Serialize<Int32>(value, data, out bytesRead, metadata, offsetStack);

        private T Deserialize<T>(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where T : struct
        {
            bytesRead = Marshal.SizeOf<T>();

            if (data.Length < bytesRead) throw new Exception($"{metadata.GetDebugInfo()}. Data length of {data.Length} not enough to read {typeof(T).FullName} of size {bytesRead}");

            var slice = data.AsSpan(offsetStack.CurrentAbsoluteOffset, bytesRead);

            //dont waste effort reversing single bytes :)
            //do not modify original data in case it is being re-read later on
            if (bytesRead > 1) slice = slice.NormalizeEndiannesInCopy(metadata.IsLittleEndian());

            return MemoryMarshal.Read<T>(slice);
        }
        private void Serialize<T>(int value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where T : struct
        {
            bytesRead = Marshal.SizeOf<T>();
            var slice = data.AsSpan(offsetStack.CurrentAbsoluteOffset, bytesRead);

            MemoryMarshal.Write(slice, value);

            //dont waste effort reversing single bytes :)
            if (bytesRead > 1) slice.NormalizeEndiannes(metadata.IsLittleEndian());
        }
    }
}
