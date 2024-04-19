using System.Text;

namespace BinaryFile.Marshaling.Context
{
    /// <summary>
    /// optional Metadata not necessary for calculating Offset or Slicing
    /// </summary>
    public interface IMarshalingMetadata
    {
        Encoding Encoding { get; }
        bool LittleEndian { get; }
        bool NullTerminated { get; }
        int? ItemCount { get; }
    }
}
