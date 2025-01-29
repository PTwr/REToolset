using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Context
{
    /// <summary>
    /// optional Metadata not necessary for calculating Offset or Slicing
    /// </summary>
    public interface IMarshalingMetadata
    {
        string FieldName { get; }
        Encoding Encoding { get; }
        bool IsLittleEndian { get; }
        bool IsNullTerminated { get; }
        int? ItemCount { get; }
    }

    public class DefaultMarshalingMetadata : IMarshalingMetadata
    {
        public DefaultMarshalingMetadata()
        {
            Encoding = Encoding.ASCII;
        }

        public DefaultMarshalingMetadata(
            string fieldName,
            Encoding? encoding = null,
            bool isLittleEndian = false,
            bool isNullTerminated = false,
            int? itemCount = null)
        {
            FieldName = fieldName;
            Encoding = encoding ?? Encoding.ASCII;
            IsLittleEndian = isLittleEndian;
            IsNullTerminated = isNullTerminated;
            ItemCount = itemCount;
        }

        public string FieldName { get; protected set; }

        public Encoding Encoding { get; protected set; }

        public bool IsLittleEndian { get; protected set; }

        public bool IsNullTerminated { get; protected set; }

        public int? ItemCount { get; protected set; }
    }
}
