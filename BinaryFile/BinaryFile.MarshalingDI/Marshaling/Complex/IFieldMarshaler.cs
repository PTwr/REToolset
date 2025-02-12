using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public interface IFieldMarshaler<TDeclaringType>
    {
        void ReadField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);
        void WriteField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);
    }
}