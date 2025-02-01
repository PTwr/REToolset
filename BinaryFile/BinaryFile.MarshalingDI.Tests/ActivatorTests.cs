using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.ObjectMarshaling;
using BinaryFile.MarshalingDI.PrimitiveMarshaling;
using BinaryFile.MarshalingDI.Marshaling;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Tests
{
    public class ActivatorTests
    {
        interface face { }
        class _Base : face { }
        class A : _Base { }
        class B : A { }
        class C : B { }

        [Fact]
        public void IntegerActivatorTest()
        {
            IActivatorMarshaler<int> marshaler = new IntegerMarshaler();

            var value = marshaler.Activate(null, null, null, null);

            Assert.Equal(0, value);
        }

        //Activator logic should be able to provide some instance for specific FIELD type, allowing for custom conditional logic peeking into raw data
        //Marshaler.Write will then be selected in separate flow by exact type of returned instance
        [Fact]
        public void MarshalerStoreTest()
        {

            var ActivatorDefault = new PatternActivatorMarshaler<_Base>([]);
            var ActivatorA = new PatternActivatorMarshaler<A>([0x01]);
            var ActivatorB = new PatternActivatorMarshaler<B>([0x02]);
            var ActivatorC = new PatternActivatorMarshaler<C>([0x03]);

            //TODO move container building to MrashalerStoreBuilder
            ContainerBuilder containerBuilder = new ContainerBuilder();

            /////////////////////////////////////////////////////////
            byte[] binary = [
                0x01, 0x02, 0x03, 0x04,
                ];
            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, true);

            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();
            ////////////////////////////////////////////

            //TODO hide behind Builder pattern
            containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(ActivatorA).As<IActivatorMarshaler<A>>();
            containerBuilder.RegisterInstance(ActivatorA).As<IActivatorMarshaler<_Base>>();

            //DI does not even allow it :)
            //TODO Builder should detect such fuck ups and throw its own error
            //containerBuilder.RegisterInstance(ActivatorA).As<IActivatorMarshaler<C>>();

            var container = containerBuilder.Build();

            var store = new DefaultMarshalerStore(container);

            //marshaler shoudl respond to all base classes its registered for
            Assert.IsAssignableFrom<IActivatorMarshaler<face>>(store.GetActivatorMarshaler<A>(dataBuffer, metadata, offsetStack, null));
            Assert.IsAssignableFrom<IActivatorMarshaler<face>>(store.GetActivatorMarshaler<_Base>(dataBuffer, metadata, offsetStack, null));

            //it is required for Activator marshaler to be registered for exact field data type
            Assert.Throws<TypeLoadException>(() => store.GetActivatorMarshaler<face>(dataBuffer, metadata, offsetStack, null));
            //and there should be no way for it to respond to call for child class even if its registered for it in DI
            Assert.Throws<TypeLoadException>(() => store.GetActivatorMarshaler<C>(dataBuffer, metadata, offsetStack, null));

            ////////////////////////////////////////////

            containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterInstance(ActivatorDefault)
                .As<IActivatorMarshaler<face>, IActivatorMarshaler<_Base>>();

            containerBuilder.RegisterInstance(ActivatorA)
                .As<IActivatorMarshaler<A>>();
            containerBuilder.RegisterInstance(ActivatorA)
                .As<IActivatorMarshaler<face>, IActivatorMarshaler<_Base>>();

            containerBuilder.RegisterInstance(ActivatorB)
                .As<IActivatorMarshaler<face>, IActivatorMarshaler<_Base>>();
            containerBuilder.RegisterInstance(ActivatorB)
                .As<IActivatorMarshaler<A>, IActivatorMarshaler<B>>();

            containerBuilder.RegisterInstance(ActivatorC)
                .As<IActivatorMarshaler<face>, IActivatorMarshaler<_Base>>();
            containerBuilder.RegisterInstance(ActivatorC)
                .As<IActivatorMarshaler<A>, IActivatorMarshaler<B>, IActivatorMarshaler<C>>();

            ////////////////////////////////////////////

            container = containerBuilder.Build();

            store = new DefaultMarshalerStore(container);


            Assert.IsAssignableFrom<IActivatorMarshaler<face>>(store.GetActivatorMarshaler<face>(dataBuffer, metadata, offsetStack, null));
            Assert.IsAssignableFrom<IActivatorMarshaler<A>>(store.GetActivatorMarshaler<A>(dataBuffer, metadata, offsetStack, null));
            Assert.IsAssignableFrom<IActivatorMarshaler<B>>(store.GetActivatorMarshaler<B>(dataBuffer, metadata, offsetStack, null));
            Assert.IsAssignableFrom<IActivatorMarshaler<C>>(store.GetActivatorMarshaler<C>(dataBuffer, metadata, offsetStack, null));

            ////////////////////////////////////////////

            //GetActivatorMarshaler should interogate all registered marshalers for given interface, thus allowing for child classes to be activated
            offsetStack.Push(0, OffsetRelation.Absolute);
            Assert.IsType<A>(store.GetActivatorMarshaler<face>(dataBuffer, metadata, offsetStack, null).Activate(dataBuffer, metadata, offsetStack, null));
            offsetStack.Push(1, OffsetRelation.Absolute);
            Assert.IsType<B>(store.GetActivatorMarshaler<face>(dataBuffer, metadata, offsetStack, null).Activate(dataBuffer, metadata, offsetStack, null));
            offsetStack.Push(2, OffsetRelation.Absolute);
            Assert.IsType<C>(store.GetActivatorMarshaler<face>(dataBuffer, metadata, offsetStack, null).Activate(dataBuffer, metadata, offsetStack, null));
            offsetStack.Push(3, OffsetRelation.Absolute);
            Assert.IsType<_Base>(store.GetActivatorMarshaler<face>(dataBuffer, metadata, offsetStack, null).Activate(dataBuffer, metadata, offsetStack, null));
        }
    }
}
