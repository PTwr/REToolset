using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.ObjectMarshaling
{
    public class ObjectMarshaler<TValue>
        : IReadMarshaler<TValue>, IWriteMarshaler<TValue>
        where TValue : class
    {
        private IObjectDescriptor<TValue> objectDescriptor;

        public ObjectMarshaler(ILifetimeScope di)
        {
            objectDescriptor = di.Resolve<IObjectDescriptor<TValue>>();
        }

        public int Order => throw new NotImplementedException();

        public bool IsForReading(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            throw new NotImplementedException();
        }

        public bool IsForWriting(TValue value)
        {
            throw new NotImplementedException();
        }

        public TValue Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            throw new NotImplementedException();
        }

        public void Write(TValue value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            throw new NotImplementedException();
        }
    }
    public interface IChildObjectDescriptor<TValue>
        where TValue : class
    {
        //returning implementation descriptor will allow it to be configurable (Eg, implementation switch depending on header bytes)
        //doing that by testing actual Type is what messed up previous codebase
        //TODO generic typed parent??
        public TValue Activate<T>(T? parent, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out IObjectDescriptor<TValue> implementationDescriptor);
        public void Read(TValue value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);
        public void Write(TValue value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);
    }


}
