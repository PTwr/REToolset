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
    public class DeserializeIntoDerivedTests
    {
        [Fact]
        public void FieldMarshalerInheritanceAndOverload()
        {
            IMarshalerStore store = new MarshalerStore();

            var mapA = new TypeMarshaler<A, A, A>();
            store.Register(mapA);
            var mapB = mapA.Derive<B>();
            var mapC1 = mapB.Derive<C1>();
            var mapC2 = mapB.Derive<C2>();

            var act1 = new LambdaFieldMarshaler<A, string>("Afoo", x => x.Foo = "bar");
            mapA.WithMarshalingAction(act1);
            var act1override = new LambdaFieldMarshaler<B, string>("Afoo", x => x.Foo = "barbar");
            var act2 = new LambdaFieldMarshaler<B, int>("Bx", x => x.X = 123);
            mapB.WithMarshalingAction(act1override);
            mapB.WithMarshalingAction(act2);
            var act3 = new LambdaFieldMarshaler<C1, int>("C1x", x => x.Y = 456);
            mapC1.WithMarshalingAction(act3);
            var act4 = new LambdaFieldMarshaler<C2, int>("C2x", x => x.Z = 789);
            mapC2.WithMarshalingAction(act4);

            var resultA = mapA.Deserialize(new A(), null, null, null, out _);
            Assert.Equal("bar", resultA.Foo);

            var resultB = mapA.Deserialize(new B(), null, null, null, out _) as B;
            Assert.IsType<B>(resultB);
            Assert.Equal("barbar", resultB.Foo);
            Assert.Equal(123, resultB.X);

            var resultC1 = mapA.Deserialize(new C1(), null, null, null, out _) as C1;
            Assert.IsType<C1>(resultC1);
            Assert.Equal("barbar", resultC1.Foo);
            Assert.Equal(123, resultC1.X);
            Assert.Equal(456, resultC1.Y);

            var resultC2 = mapA.Deserialize(new C2(), null, null, null, out _) as C2;
            Assert.IsType<C2>(resultC2);
            Assert.Equal("barbar", resultC2.Foo);
            Assert.Equal(123, resultC2.X);
            Assert.Equal(789, resultC2.Z);
        }
    }
}
