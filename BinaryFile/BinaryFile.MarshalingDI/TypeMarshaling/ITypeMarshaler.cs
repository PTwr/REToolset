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
    public interface IOrderedMarshaler
    {
        public int Order { get; }
    }
    public interface IReadMarshaler<out TMarshaledType> : IOrderedMarshaler
    {
        TMarshaledType Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);

        bool IsForReading(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack) => true;
    }
    public class LambdaReadMarshaler<TMarshaledType> : IReadMarshaler<TMarshaledType>
    {
        private readonly Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, (TMarshaledType value, int bytesRead)> reader;

        public LambdaReadMarshaler(Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, (TMarshaledType value, int bytesRead)> reader, int order)
        {
            this.reader = reader;
            Order = order;
        }

        public int Order { get; }

        public bool IsForReading(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            return true;
        }

        public TMarshaledType Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            var x = reader(data, metadata, offsetStack);
            bytesRead = x.bytesRead;
            return x.value;
        }
    }
    public interface IMutableReadMarshaler<in TMarshaledType> : IOrderedMarshaler
        where TMarshaledType : class
    {
        void Read(TMarshaledType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);

        bool IsForMutableReading(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack) => true;
    }
    public class LambdaMutableReadMarshaler<TMarshaledType> : IMutableReadMarshaler<TMarshaledType>
        where TMarshaledType : class
    {
        private readonly Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, int> reader;

        public LambdaMutableReadMarshaler(Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, int> reader)
        {
            this.reader = reader;
        }

        public int Order => throw new NotImplementedException();

        public bool IsForMutableReading(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            return true;
        }

        public void Read(TMarshaledType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            bytesRead = reader(data, metadata, offsetStack);
        }
    }
    public interface IWriteMarshaler<in TMarshaledType> : IOrderedMarshaler
    {
        void Write(TMarshaledType value, IDataBuffer data, out int bytesWrote, IMarshalingMetadata metadata, IOffsetStack offsetStack);

        bool IsForWriting(TMarshaledType value) => true;
    }
    public interface IActivatorMarshaler<out TMarshaledType> : IOrderedMarshaler
    {
        TMarshaledType Activate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent) => default!;

        bool IsForActivating(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent) => true;
    }
    public interface IFullMarshaler<TMarshaledType>
        : IReadMarshaler<TMarshaledType>, IWriteMarshaler<TMarshaledType>, IActivatorMarshaler<TMarshaledType>
    { }

    public class PatternActivatorMarshaler<TMarshaledType> : IActivatorMarshaler<TMarshaledType>
        where TMarshaledType : new()
    {
        private readonly byte?[] pattern;

        public PatternActivatorMarshaler(IEnumerable<byte?> pattern, int order = 0)
        {
            this.pattern = pattern.ToArray();
            Order = order;
        }

        public int Order { get; }

        public bool IsForActivating(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return (data.AsSpan(offsetStack.CurrentAbsoluteOffset).StartsWith(pattern));
        }

        public TMarshaledType Activate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return new();
        }
    }
    public class LambdaActivatorMarshaler<TMarshaledType> : IActivatorMarshaler<TMarshaledType>
    {
        private readonly Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, object?, TMarshaledType> activator;
        private readonly Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, object?, bool> condition;

        public LambdaActivatorMarshaler(
            Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, object?, TMarshaledType> activator,
            Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, object?, bool>? condition = null)
        {
            this.activator = activator;
            this.condition = condition ?? ((d, m, o, p) => true);
        }

        public int Order => throw new NotImplementedException();

        public bool IsForActivating(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return condition(data, metadata, offsetStack, parent);
        }

        public TMarshaledType Activate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return activator(data, metadata, offsetStack, parent);
        }
    }
}
