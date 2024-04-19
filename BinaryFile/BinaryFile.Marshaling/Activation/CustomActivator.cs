﻿using BinaryFile.Marshaling.MarshalingContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Activation
{
    public class CustomActivator<T> : ICustomActivator<T>
    {
        private readonly ICustomActivator<T>.ActivatorDelegate activator;

        public CustomActivator(ICustomActivator<T>.ActivatorDelegate activator)
        {
            this.activator = activator;
        }

        public T? Activate(object? parent, Memory<byte> data, IMarshalingContext ctx)
        {
            return activator(data, ctx);
        }
    }
    public class CustomActivator<TParent, T> : ICustomActivator<TParent, T>
    {
        private readonly ICustomActivator<TParent, T>.ChildActivatorDelegate activator;

        public CustomActivator(ICustomActivator<TParent, T>.ChildActivatorDelegate activator)
        {
            this.activator = activator;
        }
        public T? Activate(object? parent, Memory<byte> data, IMarshalingContext ctx)
        {
            if (parent is TParent)
            {
                return activator((TParent)parent, data, ctx);
            }
            return default;
        }
    }
}
