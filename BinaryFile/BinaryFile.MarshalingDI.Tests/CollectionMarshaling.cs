using Autofac;
using Autofac.Features.Metadata;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.ObjectMarshaling;
using ReflectionHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BinaryFile.MarshalingDI.Tests.CollectionMarshaling;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BinaryFile.MarshalingDI.Tests
{
    public class CollectionMarshaling
    {
        interface face { byte X { get; set; } }
        class _Base : face { public byte X { get; set; } }
        class A : _Base { }
        class B : A { }
        class C : B { }

        public interface IBlah<out T, out TCollection>
            where TCollection : IEnumerable<T>
        {
            TCollection Make();
        }

        void Foo<T>(T list)
            where T : IEnumerable
        {

        }

        //collection marshaler will always return List<(offset, item)>
        //casting to correct type will thus be relegated to FieldDescriptor<T>.StoreAs(conversionLambda)
        //CollectionRead will thus be special case to be used if no exact marshaler is registered
        //as such, FieldMarshaler needs to invoke GetReadMarshaler without exception
        //TODO change MarshalerStore to TryGet pattern? Leave Get+throw as wrappers on TryGet
        public class CollectionReadMarshaler<T> : IReadMarshaler<List<KeyValuePair<int, T>>>
        {
            private readonly IMarshalerStore store;

            public CollectionReadMarshaler(IMarshalerStore store)
            {
                this.store = store;
            }

            public int Order => 0;

            public (List<KeyValuePair<int, T>>, int) ListReadear(IDataBuffer data, IMarshalingMetadata meta, IOffsetStack stack)
            {
                var bytesRead = 0;
                List<KeyValuePair<int, T>> temp = new List<KeyValuePair<int, T>>((int)(meta.ItemCount ?? 0));
                while (stack.CurrentAbsoluteOffset < data.Length)
                {
                    //TODO try Activate and MutableRead before ImmutableRead 
                    var m = store!.GetReadMarshaler<T, T>(data, meta, stack);
                    var a = m.Read(data, out var br, meta, stack);

                    bytesRead += br;
                    stack.AddOffsetShift(br);

                    temp.Add(new KeyValuePair<int, T>(br, a));

                    if (meta.ItemCount.HasValue && meta.ItemCount.Value == temp.Count) break;
                }
                return (temp, bytesRead);
            }

            public List<KeyValuePair<int, T>> Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            {
                var x = ListReadear(data, metadata, offsetStack);
                bytesRead = x.Item2;
                return x.Item1;
            }
        }

        [Fact]
        public void CollectionMarshalerResolve()
        {
            IBlah<A, A[]> a1 = null;
            IBlah<A, IEnumerable<A>> a2 = null;
            a2 = a1;
            Dictionary<int, int> aa = null;
            IBlah<A, IEnumerable<B>> b1 = null;
            a2 = b1;

            //A can be activated/read by B and C
            IReadMarshaler<A> aaa = null;
            IReadMarshaler<B> bbb = null;
            IReadMarshaler<C> ccc = null;
            aaa = bbb;
            aaa = ccc;
            bbb = ccc;


            IMarshalerStore store = null;
            var m1 = new LambdaReadMarshaler<A>((data, meta, stack) =>
            {
                return (new A() { X = data[stack] }, 1);
            }, 0);
            var mList = new LambdaReadMarshaler<IList>((data, meta, stack) =>
            {
                //TODO ctor inject store

                var bytesRead = 0;
                List<object> temp = new List<object>((int)(meta.ItemCount ?? 0));
                while (stack.CurrentAbsoluteOffset < data.Length)
                {
                    var m = store!.GetReadMarshaler<A, A>(data, meta, stack);
                    var a = m.Read(data, out var br, meta, stack);

                    bytesRead += br;
                    stack.AddOffsetShift(br);

                    temp.Add(a);

                    if (meta.ItemCount.HasValue && meta.ItemCount.Value == temp.Count) break;
                }
                return (temp, bytesRead);
            }, 0);

            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterInstance(m1)
                .As<IReadMarshaler<A>>();
            containerBuilder.RegisterInstance(mList)
                .As<IReadMarshaler<IEnumerable>>();
            containerBuilder.RegisterType<DefaultMarshalerStore>()
                .As<IMarshalerStore>();

            var container = containerBuilder.Build();
            store = new DefaultMarshalerStore(container);

            byte[] binary = [
                0x01, 0x02, 0x03, 0x04,
                ];
            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, false);

            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var xx = typeof(A[]).EnumerateTypeHierarchy().ToList();

            var mA = store.GetReadMarshaler<IEnumerable>(dataBuffer, metadata, offsetStack);
            //var mB = store.GetReadMarshaler<IEnumerable<A>>(dataBuffer, metadata, offsetStack);
            var mC = store.GetReadMarshaler<IList>(dataBuffer, metadata, offsetStack);
            //var mD = store.GetReadMarshaler<IList<A>>(dataBuffer, metadata, offsetStack);
            var mE = store.GetReadMarshaler<IEnumerable, A[]>(dataBuffer, metadata, offsetStack);
        }
        [Fact]
        public void PrimitiveCollectionRead()
        {

        }
        [Fact]
        public void PrimitiveCollectionWrite()
        {

        }
        [Fact]
        public void ObjectCollectionRead()
        {

        }
        [Fact]
        public void ObjectCollectionWrite()
        {

        }
    }
}
