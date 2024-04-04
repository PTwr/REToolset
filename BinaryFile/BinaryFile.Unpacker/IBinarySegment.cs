using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker
{
    public interface IBinarySegment<out TParent>
    {
        TParent? Parent { get; }
    }
}
