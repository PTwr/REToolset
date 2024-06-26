using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;

namespace BinaryFile.Marshaling.Common
{
    public class RawBinaryFile : IBinaryFile
    {
        public virtual byte[] Data { get; set; }

        public RawBinaryFile()
        {
            
        }

        public RawBinaryFile(byte[] data)
        {
            Data = data;
        }

        public static void Register(IMarshalerStore marshalerStore, out RootTypeMarshaler<IBinaryFile> baseMap)
        {
            baseMap = new RootTypeMarshaler<IBinaryFile>();
            marshalerStore.Register(baseMap);

            var defaultImplementation = baseMap.Derive<RawBinaryFile>();
            marshalerStore.Register(defaultImplementation);

            defaultImplementation
                .WithField(x => x.Data)
                .AtOffset(0)
                .RelativeTo(OffsetRelation.Absolute);

            //fallback to byte[] equivalent if no other activator reported in
            var defaultActivator = new CustomActivator<IBinaryFile>((data, ctx) =>
            {
                return new RawBinaryFile();
            }, order: int.MaxValue);

            baseMap
                .WithCustomActivator(defaultActivator);
        }
    }
}
