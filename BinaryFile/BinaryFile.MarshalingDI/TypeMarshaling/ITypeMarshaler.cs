using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryDataHelper;

namespace BinaryFile.MarshalingDI.TypeMarshaling
{
    public interface IReadMarshaler<out TMarshaledType>
    {
        TMarshaledType Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);
    }
    public interface IWriteMarshaler<in TMarshaledType>
    {
        void Write(TMarshaledType value, IDataBuffer data, out int bytesWrote, IMarshalingMetadata metadata, IOffsetStack offsetStack);
    }
    public interface IActivatorMarshaler<out TMarshaledType>
    {
        TMarshaledType TryActivate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out bool activated, object? parent);
    }
    public interface IFullMarshaler<TMarshaledType>
        : IReadMarshaler<TMarshaledType>, IWriteMarshaler<TMarshaledType>, IActivatorMarshaler<TMarshaledType>
    { }

    public interface IParentingActivatorMarshaler<TMarshaledType> : IActivatorMarshaler<TMarshaledType>
    {
        void RegisterChild(IActivatorMarshaler<TMarshaledType> childActivator, int order = 0);
    }
    public class PatternActivatorMarshaler<TMarshaledType> : IActivatorMarshaler<TMarshaledType>
        where TMarshaledType : new()
    {
        private readonly byte?[] pattern;

        public PatternActivatorMarshaler(IEnumerable<byte?> pattern)
        {
            this.pattern = pattern.ToArray();
        }
        public TMarshaledType TryActivate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out bool activated, object? parent)
        {
            if (data.AsSpan(offsetStack.CurrentAbsoluteOffset).StartsWith(pattern))
            {
                activated = true;
                return new ();
            }
            activated = false;
            return default!;
        }
    }
    public class LambdaActivatorMarshaler<TMarshaledType> : IActivatorMarshaler<TMarshaledType>
    {
        private readonly Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, object?, (TMarshaledType value, bool success)> activator;

        public LambdaActivatorMarshaler(Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, object?, (TMarshaledType value, bool success)> activator)
        {
            this.activator = activator;
        }

        public TMarshaledType TryActivate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out bool activated, object? parent)
        {
            var x = activator(data, metadata, offsetStack, parent);
            if (x.success)
            {
                activated = true;
                return x.Item1;
            }
            activated = false;
            return default!;
        }
    }
    public class DefaultParentingActivatorMarshaler<TMarshaledType> : IParentingActivatorMarshaler<TMarshaledType>
    {
        public DefaultParentingActivatorMarshaler()
        {
            
        }

        public DefaultParentingActivatorMarshaler(IEnumerable<IActivatorMarshaler<TMarshaledType>> activators)
        {
            foreach (var activator in activators)
            {
                RegisterChild(activator);
            }
        }

        public TMarshaledType TryActivate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out bool activated, object? parent)
        {
            LazySort();
            foreach(var childActivator in childActivators.Select(x=>x.activator))
            {
                var value = childActivator.TryActivate(data, metadata, offsetStack, out activated, parent);
                if (activated) return value;
            }
            activated = false;
            return default!;
        }

        void LazySort()
        {
            if (sorted) return;
            sorted = true;
            childActivators = childActivators.OrderBy(x => x.order).ToList();
        }

        bool sorted = false;
        List<(IActivatorMarshaler<TMarshaledType> activator, int order)> childActivators = [];
        public void RegisterChild(IActivatorMarshaler<TMarshaledType> childActivator, int order = 0)
        {
            sorted = false;
            childActivators.Add((childActivator, order));
        }
    }
}
