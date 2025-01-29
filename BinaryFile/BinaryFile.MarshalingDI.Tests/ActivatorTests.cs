using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
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
    }
}
