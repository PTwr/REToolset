using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.ObjectMarshaling;
using BinaryFile.MarshalingDI.PrimitiveMarshaling;
using BinaryFile.MarshalingDI.TypeMarshaling;
using System;
using System.Collections.Generic;
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

            var value = marshaler.TryActivate(null, null, null, out var success);

            Assert.Equal(0, value);
            Assert.True(success);
        }

        [Fact]
        public void ChildActivatorTest()
        {
            byte[] binary = [
                0x01, 0x02, 0x03, 0x04,
                ];

            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, true);

            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            DefaultParentingActivatorMarshaler<face> baseTypeActivator = new DefaultParentingActivatorMarshaler<face>();
            baseTypeActivator.RegisterChild(new LambdaActivatorMarshaler<A>((IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack) =>
            {
                if (data.ElementAt(offsetStack.CurrentAbsoluteOffset) == 0x01)
                {
                    return (new A(), true);
                }
                return (null!, false);
            }), order: 1);
            baseTypeActivator.RegisterChild(new LambdaActivatorMarshaler<B>((IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack) =>
            {
                if (data.ElementAt(offsetStack.CurrentAbsoluteOffset) == 0x02)
                {
                    return (new B(), true);
                }
                return (null!, false);
            }), order: 2);
            baseTypeActivator.RegisterChild(new LambdaActivatorMarshaler<C>((IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack) =>
            {
                if (data.ElementAt(offsetStack.CurrentAbsoluteOffset) == 0x03)
                {
                    return (new C(), true);
                }
                return (null!, false);
            }), order: 2);

            /////////////////////////////////////////////////////

            IActivatorMarshaler<face> activatorMarshaler = baseTypeActivator;

            bool activated = false;

            offsetStack.Push(0, OffsetRelation.Absolute);
            var a = activatorMarshaler.TryActivate(dataBuffer, metadata, offsetStack, out activated);

            Assert.True(activated);
            Assert.NotNull(a);
            Assert.IsType<A>(a);

            offsetStack.Push(1, OffsetRelation.Absolute);
            var b = activatorMarshaler.TryActivate(dataBuffer, metadata, offsetStack, out activated);

            Assert.True(activated);
            Assert.NotNull(b);
            Assert.IsType<B>(b);

            offsetStack.Push(2, OffsetRelation.Absolute);
            var c = activatorMarshaler.TryActivate(dataBuffer, metadata, offsetStack, out activated);

            Assert.True(activated);
            Assert.NotNull(c);
            Assert.IsType<C>(c);

            offsetStack.Push(3, OffsetRelation.Absolute);
            var d = activatorMarshaler.TryActivate(dataBuffer, metadata, offsetStack, out activated);

            Assert.False(activated);
            Assert.Null(d);

            //default implementation registered correctly as last handler
            baseTypeActivator.RegisterChild(new LambdaActivatorMarshaler<_Base>((IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack) =>
            {
                return (new _Base(), true);
            }), order: int.MaxValue);

            offsetStack.Push(3, OffsetRelation.Absolute);
            d = activatorMarshaler.TryActivate(dataBuffer, metadata, offsetStack, out activated);

            Assert.True(activated);
            Assert.NotNull(d);
            Assert.IsType<_Base>(d);

            //default implementation registered errorneusly as first handler
            baseTypeActivator.RegisterChild(new LambdaActivatorMarshaler<_Base>((IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack) =>
            {
                return (new _Base(), true);
            }), order: int.MinValue);

            offsetStack.Push(0, OffsetRelation.Absolute);
            a = activatorMarshaler.TryActivate(dataBuffer, metadata, offsetStack, out activated);

            Assert.True(activated);
            Assert.NotNull(a);
            Assert.IsType<_Base>(a);
        }

        [Fact]
        public void MarshalerStoreTest()
        {
            var ActivatorDefault = new PatternActivatorMarshaler<_Base>([]);
            var ActivatorA = new PatternActivatorMarshaler<A>([0x01]);
            var ActivatorB = new PatternActivatorMarshaler<B>([0x02]);
            var ActivatorC = new PatternActivatorMarshaler<C>([0x03]);

            //TODO move container building to MrashalerStoreBuilder
            ContainerBuilder containerBuilder = new ContainerBuilder();

            //Register<_Base>().As<_face>().As<A>().As<B>().As<C>()... ?
            //Activate<C>().As<face>().As<_base>()...
            DefaultParentingActivatorMarshaler<face> _face = new DefaultParentingActivatorMarshaler<face>(
                [ActivatorA, ActivatorB, ActivatorC, ActivatorDefault]);
            DefaultParentingActivatorMarshaler<A> _a = new DefaultParentingActivatorMarshaler<A>(
                [ActivatorA, ActivatorB, ActivatorC]);
            DefaultParentingActivatorMarshaler<B> _b = new DefaultParentingActivatorMarshaler<B>(
                [ActivatorB, ActivatorC]);
            DefaultParentingActivatorMarshaler<C> _c = new DefaultParentingActivatorMarshaler<C>(
                [ActivatorC]);

            containerBuilder.RegisterInstance(_face).As<IActivatorMarshaler<face>>();

            var container = containerBuilder.Build();

            IMarshalerStore store = new DefaultMarshalerStore(container);

            //it is required for Activator marshaler to be registered for exact field data type
            Assert.Throws<TypeLoadException>(() => store.GetActivatorMarshaler<A>());

            Assert.IsAssignableFrom<IActivatorMarshaler<face>>(store.GetActivatorMarshaler<face>());

            /////////////////////////////////////////////////////////

            containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(_face).As<IActivatorMarshaler<face>>();
            containerBuilder.RegisterInstance(_a).As<IActivatorMarshaler<A>>();
            containerBuilder.RegisterInstance(_b).As<IActivatorMarshaler<B>>();
            containerBuilder.RegisterInstance(_c).As<IActivatorMarshaler<C>>();

            container = containerBuilder.Build();

            store = new DefaultMarshalerStore(container);

            Assert.IsAssignableFrom<IActivatorMarshaler<face>>(store.GetActivatorMarshaler<face>());
            Assert.IsAssignableFrom<IActivatorMarshaler<A>>(store.GetActivatorMarshaler<A>());
            Assert.IsAssignableFrom<IActivatorMarshaler<B>>(store.GetActivatorMarshaler<B>());
            Assert.IsAssignableFrom<IActivatorMarshaler<C>>(store.GetActivatorMarshaler<C>());

            ////////////////////////////////////////////
            
            byte[] binary = [
                0x01, 0x02, 0x03, 0x04,
                ];
            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, true);

            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            offsetStack.Push(0, OffsetRelation.Absolute);
            Assert.IsType<A>(store.GetActivatorMarshaler<face>().TryActivate(dataBuffer, metadata, offsetStack, out _));
            offsetStack.Push(1, OffsetRelation.Absolute);
            Assert.IsType<B>(store.GetActivatorMarshaler<face>().TryActivate(dataBuffer, metadata, offsetStack, out _));
            offsetStack.Push(2, OffsetRelation.Absolute);
            Assert.IsType<C>(store.GetActivatorMarshaler<face>().TryActivate(dataBuffer, metadata, offsetStack, out _));
            offsetStack.Push(3, OffsetRelation.Absolute);
            Assert.IsType<_Base>(store.GetActivatorMarshaler<face>().TryActivate(dataBuffer, metadata, offsetStack, out _));
        }
    }
}
