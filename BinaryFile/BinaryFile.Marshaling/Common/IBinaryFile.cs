using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BinaryFile.Marshaling.Common
{
    public interface IBinaryFile
    {
    }

    public class RawBinaryFile : IBinaryFile
    {
        public virtual byte[] FileContent { get; set; }

        public static void Register(IMarshalerStore marshalerStore)
        {
            var interfaceMap = new RootTypeMarshaler<IBinaryFile>();
            var map = interfaceMap.Derive<RawBinaryFile>();

            map
                .WithField(i => i.FileContent)
                .AtOffset(0)
                .RelativeTo(OffsetRelation.Segment);

            map.WithByteLengthOf(i => i.FileContent.Length);

            //default fallback activator
            interfaceMap.WithCustomActivator(new CustomActivator<IBinaryFile>((data, ctx) =>
            {
                return new RawBinaryFile();
            }, order: int.MaxValue));

            marshalerStore.Register(interfaceMap);
        }
    }
}
