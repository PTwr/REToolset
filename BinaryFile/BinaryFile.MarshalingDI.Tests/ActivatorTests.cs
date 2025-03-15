using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
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
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.DI;

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
            IActivatorMarshaler<int> marshaler = new IntegerMarshaler(null, null, new FeatureSetStack());

            var value = marshaler.Activate();

            Assert.Equal(0, value);
        }

        //Activator logic should be able to provide some instance for specific FIELD type, allowing for custom conditional logic peeking into raw data
        //Marshaler.Write will then be selected in separate flow by exact type of returned instance
        [Fact]
        public void MarshalerStoreTestExact()
        {
            //TODO move container building to MrashalerStoreBuilder
            ContainerBuilder containerBuilder = new ContainerBuilder();
            containerBuilder
                .WithRequiredServices()
                .WithHelpers()
                .WithPrimitiveMarshalers();

            new PatternActivatorMarshaler<_Base>.Builder()
                .WithPattern([])
                .WithActivationOrderOf(int.MaxValue)
                .Register(containerBuilder);
            new PatternActivatorMarshaler<A>.Builder()
                .WithPattern([0x01])
                .WithActivationOrderOf(0)
                .Register(containerBuilder);

            var container = containerBuilder.Build();

            /////////////////////////////////////////////////////////
            byte[] binary = [
                0x01, 0x02, 0x03, 0x04,
                ];
            container.Resolve<IDataBufferIO>().SetData(binary);
            ////////////////////////////////////////////

            var store = container.Resolve<IMarshalerStore>();

            //marshaler shoudl respond to all base classes its registered for
            Assert.IsAssignableFrom<IActivatorMarshaler<face>>(store.GetActivatorMarshaler<A>());
            Assert.IsAssignableFrom<IActivatorMarshaler<face>>(store.GetActivatorMarshaler<_Base>());

            //it is required for Activator marshaler to be registered for exact field data type
            Assert.Throws<TypeLoadException>(() => store.GetActivatorMarshaler<face>());
            //and there should be no way for it to respond to call for child class even if its registered for it in DI
            Assert.Throws<TypeLoadException>(() => store.GetActivatorMarshaler<C>());

            //DI does not even allow it :)
            //TODO Builder should detect such fuck ups and throw its own error
            //containerBuilder.RegisterInstance(ActivatorA).As<IActivatorMarshaler<C>>();
        }

        [Fact]
        public void MarshalerStoreTestPolymorph()
        {

            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .WithRequiredServices()
                .WithHelpers()
                .WithPrimitiveMarshalers();

            new PatternActivatorMarshaler<_Base>.Builder()
                .WithPattern([])
                .WithActivationOrderOf(int.MaxValue)
                .AlsoFor<face>()
                //TODO extension factory method on containerbuilder to pass it to Builder
                .Register(containerBuilder);
            new PatternActivatorMarshaler<A>.Builder()
                .WithPattern([0x01])
                .WithActivationOrderOf(0)
                .AlsoFor<face>().AlsoFor<_Base>()
                .Register(containerBuilder);
            new PatternActivatorMarshaler<B>.Builder()
                .WithPattern([0x02])
                .WithActivationOrderOf(0)
                .AlsoFor<face>().AlsoFor<_Base>().AlsoFor<A>()
                .Register(containerBuilder);
            new PatternActivatorMarshaler<C>.Builder()
                .WithPattern([0x03])
                .WithActivationOrderOf(0)
                //TODO autocrawl with TLimit for base class to stop crawling on, to avoid autoregistering for object itself
                .AlsoFor<face>().AlsoFor<_Base>().AlsoFor<A>().AlsoFor<B>()
                .Register(containerBuilder);

            var container = containerBuilder.Build();

            /////////////////////////////////////////////////////////
            byte[] binary = [
                0x01, 0x02, 0x03, 0x04,
                ];
            container.Resolve<IDataBufferIO>().SetData(binary);
            ////////////////////////////////////////////

            var store = container.Resolve<IMarshalerStore>();
            var offsetStack = container.Resolve<IOffsetStack>();

            offsetStack.Push(0, OffsetRelation.Absolute);
            Assert.IsAssignableFrom<IActivatorMarshaler<face>>(store.GetActivatorMarshaler<face>());
            Assert.IsAssignableFrom<IActivatorMarshaler<A>>(store.GetActivatorMarshaler<A>());
            offsetStack.Push(1, OffsetRelation.Absolute);
            Assert.IsAssignableFrom<IActivatorMarshaler<B>>(store.GetActivatorMarshaler<B>());
            offsetStack.Push(2, OffsetRelation.Absolute);
            Assert.IsAssignableFrom<IActivatorMarshaler<C>>(store.GetActivatorMarshaler<C>());

            ////////////////////////////////////////////

            //GetActivatorMarshaler should interogate all registered marshalers for given interface, thus allowing for child classes to be activated
            offsetStack.Push(0, OffsetRelation.Absolute);
            Assert.IsType<A>(store.GetActivatorMarshaler<face>().Activate());
            offsetStack.Push(1, OffsetRelation.Absolute);
            Assert.IsType<B>(store.GetActivatorMarshaler<face>().Activate());
            offsetStack.Push(2, OffsetRelation.Absolute);
            Assert.IsType<C>(store.GetActivatorMarshaler<face>().Activate());
            offsetStack.Push(3, OffsetRelation.Absolute);
            Assert.IsType<_Base>(store.GetActivatorMarshaler<face>().Activate());
        }
    }
}
