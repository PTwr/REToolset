using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.DAL
{
    public interface IDataBuffer
    {
        Span<byte> AsSpan();
        Span<byte> AsSpan(int from);
        Span<byte> AsSpan(int from, int length);
        byte ElementAt(int index);
        void Emplace(int position, byte[] b);

        int Length { get; }

        byte this[int index] { get; set; }
        byte this[IOffsetStack index] { get; set; }
    }
}
