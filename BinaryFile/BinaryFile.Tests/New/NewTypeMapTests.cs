using BinaryDataHelper;
using BinaryFile.Unpacker.New.Implementation;
using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Tests.New
{
    public class NewTypeMapTests
    {
        class A
        {
            public int X { get; set; }
        }
        class B : A
        {
            public A parentA;
            public B()
            {
                
            }
            public B(A parent)
            {
                this.parentA = parent;
            }
            public int Y { get; set; }
        }
        class C : B
        {
            public B parentB;
            public C()
            {
                
            }
            public C(B parent) : base(parent)
            {
                this.parentB = parent;
            }
            public int Z { get; set; }
        }

        class LambdaFieldMarshaler<TMappedType, TFieldType>
            : OrderedFieldMarshaler<TMappedType, TFieldType, TFieldType, LambdaFieldMarshaler<TMappedType, TFieldType>>
            where TMappedType : class
        {
            private readonly Action<TMappedType> action;

            public LambdaFieldMarshaler(string name, Action<TMappedType> action) : base(name)
            {
                this.action = action;
            }

            public override void DeserializeInto(TMappedType mappedObject, Span<byte> data, IFluentMarshalingContext ctx, out int fieldByteLengh)
            {
                action(mappedObject);
                fieldByteLengh = 0;
            }

            public override void SerializeFrom(TMappedType mappedObject, ByteBuffer data, IFluentMarshalingContext ctx, out int fieldByteLengh)
            {
                action(mappedObject);
                fieldByteLengh = 0;
            }
        }

        [Fact]
        public void HierarchicalRegistration()
        {
            var store = new MarshalerStore();

            var rootMap = new TypeMarshaler<A, A>();
            rootMap.WithDeserializingAction(new LambdaFieldMarshaler<A, int>("X", i => i.X = 123));

            store.RegisterRootMap(rootMap);

            var shouldBeRootMap = store.GetMarshalerToDerriveFrom<A>();

            Assert.NotNull(shouldBeRootMap);
            Assert.Equal(rootMap, shouldBeRootMap);

            var mapB = shouldBeRootMap.Derrive<B>();

            var shouldBeMapB = store.GetDeserializerFor<B>();
            var thisToShouldBeMapB = store.GetMarshalerToDerriveFrom<B>();

            Assert.NotNull(shouldBeMapB);
            Assert.NotNull(thisToShouldBeMapB);
            Assert.Equal(mapB, shouldBeMapB);
            Assert.Equal(mapB, thisToShouldBeMapB);

            var mapC = thisToShouldBeMapB.Derrive<C>();

            var shouldBeMapC = store.GetDeserializerFor<C>();

            Assert.NotNull(shouldBeMapC);
            Assert.Equal(mapC, shouldBeMapC);
        }

        [Fact]
        public void HierarchicalActivation()
        {
            var store = new MarshalerStore();

            var rootMap = new TypeMarshaler<A, A>();
            store.RegisterRootMap(rootMap);

            //HierarchicalRegistration does tests for this
            store.GetMarshalerToDerriveFrom<A>()!.Derrive<B>();
            store.GetMarshalerToDerriveFrom<B>()!.Derrive<C>();

            var activatorForA = store.GetActivatorFor<A>(null, null);
            var activatorForB = store.GetActivatorFor<B>(null, null);
            var activatorForC = store.GetActivatorFor<C>(null, null);

            Assert.NotNull(activatorForA);
            Assert.NotNull(activatorForB);
            Assert.NotNull(activatorForC);

            var a = activatorForA.Activate(null, null, null);
            var b = activatorForB.Activate(null, null, null);
            var c = activatorForC.Activate(null, null, null);

            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.NotNull(c);

            a = activatorForA.Activate(null, null, null);
            b = activatorForB.Activate(null, null, a);
            c = activatorForC.Activate(null, null, b);

            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.NotNull(c);

            Assert.Equal(a, b.parentA);
            Assert.Equal(b, c.parentB);
        }
    }
}
