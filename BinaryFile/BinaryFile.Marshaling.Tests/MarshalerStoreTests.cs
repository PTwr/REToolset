using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;

namespace BinaryFile.Marshaling.Tests
{
    public class MarshalerStoreTests
    {
        [Fact]
        public void ReturnBaseMarshalerForItsDerivedTypes()
        {
            IMarshalerStore store = new MarshalerStore();

            var mapA = new TypeMarshaler<A, A, A>();
            var mapX = new TypeMarshaler<X, X, X>();

            store.Register(mapA);
            store.Register(mapX);

            //Store should return Base map for each hierarchy, base map will take care of its derived maps during Activation/Deserialization/Serialization
            var a = store.FindMarshaler(typeof(A));
            var b = store.FindMarshaler(typeof(B));
            var c1 = store.FindMarshaler(typeof(C1));
            var c2 = store.FindMarshaler(typeof(C2));
            var x = store.FindMarshaler(typeof(X));
            var y = store.FindMarshaler(typeof(Y));
            var z = store.FindMarshaler(typeof(Z));

            Assert.Equal(mapA, a);
            Assert.Equal(mapA, b);
            Assert.Equal(mapA, c1);
            Assert.Equal(mapA, c2);

            Assert.Equal(mapX, x);
            Assert.Equal(mapX, y);
            Assert.Equal(mapX, z);
        }
    }
}