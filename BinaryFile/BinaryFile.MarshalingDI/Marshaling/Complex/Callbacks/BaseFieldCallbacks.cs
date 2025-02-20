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
        public Func<ILifetimeScope, (int offset, OffsetRelation relation)>? OffsetCalculator = 
            null;
        public Func<ILifetimeScope, int> ReadOrderCalculator = 
            (x) => 0;
        public Func<ILifetimeScope, int> WriteOrderCalculator = 
            (x) => 0;

        public Func<ILifetimeScope, EMarshalingType> MarshalingType = 
            (x) => EMarshalingType.Reading | EMarshalingType.Writing;

        public Func<ILifetimeScope, bool> AfterReadValidator = 
            (x) => true;
        public Func<ILifetimeScope, bool> BeforeWriteValidator = 
            (x) => true;
        public Action<ILifetimeScope, int> OnAfterWrite = 
            (x, bytesWrote) => { };
    }
}
