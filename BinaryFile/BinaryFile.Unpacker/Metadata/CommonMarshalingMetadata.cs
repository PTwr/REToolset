using ReflectionHelper;
using System.Text;

namespace BinaryFile.Unpacker.Metadata
{
    public interface IWithCommonMarshalingMetadata
    {
        int? Length { get; }
        int? ItemLength { get; }
        int? Count { get; }
        Encoding? Encoding { get; }
        bool? IsNestedFile { get; }
        bool? NullTerminated { get; }
        bool? LittleEndian { get; }
    }
    public class DynamicCommonMarshalingMetadata<TDeclaringType>
    {
        public FuncField<TDeclaringType, int>? Length;
        public FuncField<TDeclaringType, int>? ItemLength;
        public FuncField<TDeclaringType, int>? Count;
        public FuncField<TDeclaringType, Encoding>? Encoding;
        public FuncField<TDeclaringType, bool>? IsNestedFile;
        public FuncField<TDeclaringType, bool>? NullTerminated;
        public FuncField<TDeclaringType, bool>? LittleEndian;
    }
}
