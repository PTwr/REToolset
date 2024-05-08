using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.PrimitiveMarshaling;
using BinaryFile.Marshaling.TypeMarshaling;

namespace BinaryFile.Marshaling.MarshalingStore
{
    public class DefaultMarshalerStore : MarshalerStore
    {
        public DefaultMarshalerStore()
        {
            Register(new IntegerMarshaler());
            Register(new StringMarshaler());
            Register(new IntegerArrayMarshaler());

            RawBinaryFile.Register(this, out ibinaryFileMap);
        }
        RootTypeMarshaler<IBinaryFile> ibinaryFileMap;
        public ITypeMarshaler<IBinaryFile, IBinaryFile, T> DeriveBinaryFile<T>(ICustomActivator<IBinaryFile> customActivator)
            where T : class, IBinaryFile
        {
            ibinaryFileMap.WithCustomActivator(customActivator);
            return ibinaryFileMap.Derive<T>();
        }
    }
}
