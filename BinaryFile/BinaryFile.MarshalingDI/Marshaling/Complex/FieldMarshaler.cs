using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public class FieldMarshaler<TDeclaringType, TMashaledType>
    {
        private List<object> _metadata = new List<object>();
        public IList Metadata => _metadata.AsReadOnly();

        public FieldMarshaler<TDeclaringType, TMashaledType> From(Func<TDeclaringType, TMashaledType> getter)
        {
            return this;
        }
        public FieldMarshaler<TDeclaringType, TMashaledType> Into(Action<TDeclaringType, TMashaledType> setter)
        {
            return this;
        }

        public FieldMarshaler<TDeclaringType, TMashaledType> AtOffset(Func<TDeclaringType, int> offsetCalculator)
        {
            return this;
        }
    }
}
