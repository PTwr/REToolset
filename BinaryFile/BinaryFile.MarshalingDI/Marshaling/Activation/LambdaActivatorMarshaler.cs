using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Marshaling.Activating
{
    //TODO unify with ObjectBuilder
    //TODO parentless version
    //TODO or maybe not unify? let ObjectBuilder jump to this Builder and leave DefaultActivator as order = int.MaxValue?
    public class LambdaActivatorMarshaler<TMarshaledType, TParent> : IActivatorMarshaler<TMarshaledType>
    {
        private readonly IFeatureSetStack features;
        private readonly IOffsetStack offsetStack;
        private readonly IDataBuffer data;
        private readonly Func<IDataBuffer, IOffsetStack, TParent?, TMarshaledType> activator;
        private readonly Func<IDataBuffer, IOffsetStack, TParent?, bool> condition;

        //TODO builder
        public LambdaActivatorMarshaler(
            IFeatureSetStack features,
            IOffsetStack offsetStack,
            IDataBuffer data,
            Func<IDataBuffer, IOffsetStack, TParent?, TMarshaledType> activator,
            Func<IDataBuffer, IOffsetStack, TParent?, bool>? condition = null)
        {
            this.features = features;
            this.offsetStack = offsetStack;
            this.data = data;
            this.activator = activator;
            this.condition = condition ?? ((d, o, p) => true);
        }

        public bool IsForActivating()
        {
            return condition(data, offsetStack, features.GetParent<TParent>());
        }

        public TMarshaledType Activate()
        {
            return activator(data, offsetStack, features.GetParent<TParent>());
        }
    }
}
