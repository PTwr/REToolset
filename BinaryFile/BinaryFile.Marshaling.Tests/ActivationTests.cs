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
    }
}
