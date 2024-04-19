using BinaryFile.Marshaling.MarshalingContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Activation
{
    interface ICustomActivator<T>
    {
        T? Activate(object? parent, Span<byte> data, IMarshalingContext ctx);
    }

    class CustomActivator<T> : ICustomActivator<T>
    {
        private readonly ActivatorDelegate activator;

        public delegate T ActivatorDelegate(Span<byte> data, IMarshalingContext ctx);
        public CustomActivator(ActivatorDelegate activator)
        {
            this.activator = activator;
        }

        public T? Activate(object? parent, Span<byte> data, IMarshalingContext ctx)
        {
            return activator(data, ctx);
        }
    }
    class CustomActivator<TParent, T> : ICustomActivator<T>
    {
        private readonly ActivatorDelegate activator;

        public delegate T ActivatorDelegate(TParent parent, Span<byte> data, IMarshalingContext ctx);
        public CustomActivator(ActivatorDelegate activator)
        {
            this.activator = activator;
        }
        public T? Activate(object? parent, Span<byte> data, IMarshalingContext ctx)
        {
            if (parent is TParent)
            {
                return activator((TParent)parent, data, ctx);
            }
            return default;
        }
    }
}
