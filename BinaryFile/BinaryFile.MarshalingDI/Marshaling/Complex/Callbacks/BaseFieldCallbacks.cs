using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BinaryFile.MarshalingDI.Context.IHierarchicalFeatureSet;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks
{
    public abstract class BaseFieldCallbacks<TDeclaringType>
        : BaseCallbacks<TDeclaringType>
    {
        //TODO IFeature instead of raw func?
        public Func<IContainer, (int offset, OffsetRelation relation)>? OffsetCalculator = 
            null;
        public Func<IContainer, int> ReadOrderCalculator = 
            (x) => 0;
        public Func<IContainer, int> WriteOrderCalculator = 
            (x) => 0;

        public Func<IContainer, EMarshalingType> MarshalingType = 
            (x) => EMarshalingType.Reading | EMarshalingType.Writing;

        public Func<IContainer, bool> AfterReadValidator = 
            (x) => true;
        public Func<IContainer, bool> BeforeWriteValidator = 
            (x) => true;
        public Action<IContainer, int> OnAfterWrite = 
            (x, bytesWrote) => { };
    }
}
