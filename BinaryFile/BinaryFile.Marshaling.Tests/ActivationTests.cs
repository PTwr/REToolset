using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.FieldMarshaling;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Tests
{
    public class ActivationTests
    {
        [Fact]
        public void ActivateThroughParameterlessCtor()
        {
            IMarshalerStore store = new MarshalerStore();

            var mapA = new TypeMarshaler<A, A, A>();
            store.Register(mapA);

            var a = mapA.Activate(null, null, null);

            Assert.NotNull(a);
            Assert.Equal(null, a.Parent);
        }
        [Fact]
        public void ActivateThroughParentCtor()
        {
            IMarshalerStore store = new MarshalerStore();

            var mapA = new TypeMarshaler<A, A, A>();
            store.Register(mapA);

            object parent = new object();

            var a = mapA.Activate(parent, null, null);

            Assert.NotNull(a);
            Assert.Equal(parent, a.Parent);
        }
        [Fact]
        public void ActivateDerivedType()
        {
            IMarshalerStore store = new MarshalerStore();

            var mapA = new TypeMarshaler<A, A, A>();
            store.Register(mapA);
            mapA.Derive<B>();

            var parent = new A();

            var b = mapA.Activate(parent, null, null, typeof(B));

            Assert.NotNull(b);
            Assert.IsType<B>(b);
        }
        [Fact]
        public void CustomActivator()
        {
            IMarshalerStore store = new MarshalerStore();

            var mapA = new TypeMarshaler<A, A, A>();
            store.Register(mapA);
            var mapB = mapA.Derive<B>();

            var caPattern = new CustomActivator<B>((data, ctx) =>
            {
                //conditional activation by byte pattern
                if (data.Span[0] == 0)
                    return new B();
                if (data.Span[0] == 1)
                    return new C1();
                if (data.Span[0] == 2)
                    return new C2();
                //return execution to default activator
                return null;
            });
            //will only be executed if parent is TParent
            var caParent = new CustomActivator<B, int>((parent, data, ctx) =>
            {
                //conditional activation by testing parent
                if (parent == 0)
                    return new B();
                if (parent == 1)
                    return new C1();
                if (parent == 2)
                    return new C2();
                //return execution to default activator
                return null;
            });
            //custom activator will fire before mapA queries its derived maps
            mapA.WithCustomActivator(caPattern);

            //caParent will not fire if parent type is not matching
            mapA.WithCustomActivator(caParent);

            byte[] bytes = [0, 1, 2, 3];
            var b = mapA.Activate(null, bytes.AsMemory(0), null, null);
            var c1 = mapA.Activate(null, bytes.AsMemory(1), null, null);
            var c2 = mapA.Activate(null, bytes.AsMemory(2), null, null);
            var a = mapA.Activate(null, bytes.AsMemory(3), null, null);

            Assert.NotNull(b);
            Assert.IsType<B>(b);
            Assert.NotNull(c1);
            Assert.IsType<C1>(c1);
            Assert.NotNull(c2);
            Assert.IsType<C2>(c2);
            Assert.NotNull(a);
            Assert.IsType<A>(a);

            //all data will point to 3, which will make caPattern return null thus caParent will fire
            b = mapA.Activate(0, bytes.AsMemory(3), null, null);
            c1 = mapA.Activate(1, bytes.AsMemory(3), null, null);
            c2 = mapA.Activate(2, bytes.AsMemory(3), null, null);
            a = mapA.Activate(3, bytes.AsMemory(3), null, null);

            Assert.NotNull(b);
            Assert.IsType<B>(b);
            Assert.NotNull(c1);
            Assert.IsType<C1>(c1);
            Assert.NotNull(c2);
            Assert.IsType<C2>(c2);
            Assert.NotNull(a);
            Assert.IsType<A>(a);
        }
        [Fact]
        public void CustomActivatorOrder()
        {
            IMarshalerStore store = new MarshalerStore();

            var mapA = new TypeMarshaler<A, A, A>();
            store.Register(mapA);
            var mapB = mapA.Derive<B>();

            //non conditional activators
            var caC1 = new CustomActivator<B>((data, ctx) =>
            {
                return new C1();
            }, int.MaxValue);
            var caC2 = new CustomActivator<B>((data, ctx) =>
            {
                return new C2();
            }, int.MinValue);

            //activator order is reverse to insertion order
            mapA.WithCustomActivator(caC1);
            mapA.WithCustomActivator(caC2);

            var x = mapA.Activate(null, null, null, null);

            Assert.NotNull(x);
            Assert.IsType<C2>(x);
        }
    }
}
