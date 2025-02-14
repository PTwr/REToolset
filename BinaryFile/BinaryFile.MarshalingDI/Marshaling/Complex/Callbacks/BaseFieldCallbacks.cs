using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks
{
    public abstract class BaseFieldCallbacks<TDeclaringType>
    {
        public Func<TDeclaringType, (int offset, OffsetRelation relation)>? OffsetCalculator = null;
        public Func<TDeclaringType, int> ReadOrderCalculator = (x) => 0;
        public Func<TDeclaringType, int> WriteOrderCalculator = (x) => 0;

        public Func<TDeclaringType, EMarshalingType> MarshalingType = (x) => EMarshalingType.Reading | EMarshalingType.Writing;
    }
}
