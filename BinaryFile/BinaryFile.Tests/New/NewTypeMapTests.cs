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
            public int Y { get; set; }
        }
        class C : B
        {
            public int Z { get; set; }
        }

        [Fact]
        public void DeserializerGet()
        {
            //ITypeMarshalerStore store = new TypeMarshalerStore();

            //TypeMarshaler<A, A> marshA = new TypeMarshaler<A, A>(null);
            //var marshB = marshA.Derrive<B>();
            ////TODO prevent accidental marshA.Derrive<C> when Derrive<B> is already present? C should derrive from B to not fuck up hierarchy
            //var marshC = marshB.Derrive<C>();

            //IMarshaler<A> mA = marshA;
            //IMarshaler<B> mB = marshA;
            //IMarshaler<C> mC = marshA;

            //IActivator<A> aA = marshA;
            //IActivator<A> aB = marshB;
            //IActivator<A> aC = marshC;

            //store.Register(marshA);

            //var a = store.GetDeserializer<A>();
            //var b = store.GetDeserializer<B>();
            //var c = store.GetDeserializer<C>();

            //var aa = store.GetSerializer<A>();
            //var bb = store.GetSerializer<B>();
            //var cc = store.GetSerializer<C>();
        }
    }
}
