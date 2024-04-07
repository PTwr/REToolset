using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Tests
{
    public class InheritanceTests
    {
        public class Container
        {
            public byte Foo { get; set; }
            public byte Bar { get; set; }
            public Base? ContentByContainerFlag { get; set; }
            public Base? ContentbyPattern { get; set; }
        }
        public class Base
        {
            public byte Length { get; set; } = 3;
            public bool Determinator { get; set; }
        }
        public class ChildA : Base
        {
            public byte[]? B { get; set; }
        }
        public class ChildB : Base
        {
            public string? B { get; set; }
        }

        [Fact]
        public void UseBaseClassFieldDescriptorsInDerrivedTypeMap()
        {
            byte[] data = [
                5, 0,
                0x41, 0x42, 0x43, 0x44, 0x45
                ];

            //TODO fluentcall for Field descriptor to return to Type descriptor or to flow to next field
            var mapBase = new FluentMarshaler<Base>();
            mapBase
                .WithField<byte>("length")
                .AtOffset(0)
                .Into((i, x) => i.Length = x);
            mapBase
                .WithField<bool>("determinator")
                .AtOffset(1)
                .Into((i, x) => i.Determinator = x);
            var mapChildA = new FluentMarshaler<ChildA, Base>()
                .InheritsFrom(mapBase);
            mapChildA
                .WithCollectionOf<byte>("bytes")
                .AtOffset(2)
                .WithLengthOf(i => i.Length)
                .Into((i, x) => i.B = x.ToArray());
            var mapChildB = new FluentMarshaler<ChildB, Base>()
                .InheritsFrom(mapBase);
            mapChildB
                .WithField<string>("string")
                .AtOffset(2)
                .WithLengthOf(i => i.Length)
                .Into((i, x) => i.B = x);

            var mgr = new MarshalerManager();
            var ctx = new RootMarshalingContext(mgr, mgr);
            mgr.Register(mapChildA);
            mgr.Register(mapChildB);
            mgr.Register(mapBase);
            mgr.Register(new IntegerMarshaler());
            mgr.Register(new StringMarshaler());
            mgr.Register(new BinaryArrayMarshaler());

            //TODO someday make helper methods on Ctx to get those without having to cast between Deserializer and Serializer
            mgr.TryGetMapping<Base>(out IDeserializer<Base>? d1);
            Assert.Equal(mapBase, d1);
            mgr.TryGetMapping<ChildA>(out IDeserializer<ChildA>? d2);
            Assert.Equal(mapChildA, d2);
            mgr.TryGetMapping<ChildB>(out IDeserializer<ChildB>? d3);
            Assert.Equal(mapChildB, d3);

            var r1 = d1.Deserialize(data.AsSpan(), ctx, out _);
            var r2 = d2.Deserialize(data.AsSpan(), ctx, out _);
            var r3 = d3.Deserialize(data.AsSpan(), ctx, out _);

            Assert.Equal(5, r1.Length);
            Assert.Equal(5, r2.Length);
            Assert.Equal(5, r3.Length);
            Assert.Equal(data.AsSpan(1, 5).ToArray(), r2.B);
            Assert.Equal("ABCDE", r3.B);
        }

        [Fact]
        public void ConditionallySelectDeriviedTypeMap()
        {
            byte[] dataA = [
                1, 1, //Container Foo and Bar
                5, 0, //Length, Determinator False
                0x41, 0x42, 0x43, 0x44, 0x45
                ];
            byte[] dataB = [
                2, 2, //Container Foo and Bar
                5, 1, //Length, Determinator True
                0x41, 0x42, 0x43, 0x44, 0x45
                ];

            //TODO fluentcall for Field descriptor to return to Type descriptor or to flow to next field
            var mapContainer = new FluentMarshaler<Container>();
            mapContainer
                .WithField<byte>("foo")
                .AtOffset(0)
                .Into((i, x) => i.Foo = x);
            mapContainer
                .WithField<byte>("bar")
                .AtOffset(1)
                .Into((i, x) => i.Bar = x);

            mapContainer
                .WithField<ChildA>("childA by flag")
                .WhenFlag(i => i.Foo == 1)
                .AtOffset(2)
                .Into((i, x) => i.ContentByContainerFlag = x);
            mapContainer
                .WithField<ChildB>("childB by flag")
                .WhenFlag(i => i.Foo == 2)
                .AtOffset(2)
                .Into((i, x) => i.ContentByContainerFlag = x);

            mapContainer
                .WithField<ChildA>("childA by pattern")
                .WithPatternCondition((obj, span, ctx) =>
                {
                    var slice = ctx.Slice(span);
                    return slice[1] == 0;
                })
                .AtOffset(2)
                .Into((i, x) => i.ContentbyPattern = x);
            mapContainer
                .WithField<ChildB>("childB by pattern")
                .WithPatternCondition((obj, span, ctx) =>
                {
                    var slice = ctx.Slice(span);
                    return slice[1] == 1;
                })
                .AtOffset(2)
                .Into((i, x) => i.ContentbyPattern = x);

            var mapBase = new FluentMarshaler<Base>();
            mapBase
                .WithField<byte>("length")
                .AtOffset(0)
                .Into((i, x) => i.Length = x);
            mapBase
                .WithField<bool>("determinator")
                .AtOffset(1)
                .Into((i, x) => i.Determinator = x);
            var mapChildA = new FluentMarshaler<ChildA, Base>()
                .InheritsFrom(mapBase);
            mapChildA
                .WithCollectionOf<byte>("bytes")
                .AtOffset(2)
                .WithLengthOf(i => i.Length)
                .Into((i, x) => i.B = x.ToArray());
            var mapChildB = new FluentMarshaler<ChildB, Base>()
                .InheritsFrom(mapBase);
            mapChildB
                .WithField<string>("string")
                .AtOffset(2)
                .WithLengthOf(i => i.Length)
                .Into((i, x) => i.B = x);

            var mgr = new MarshalerManager();
            var ctx = new RootMarshalingContext(mgr, mgr);
            mgr.Register(mapChildA);
            mgr.Register(mapChildB);
            mgr.Register(mapBase);
            mgr.Register(mapContainer);
            mgr.Register(new IntegerMarshaler());
            mgr.Register(new StringMarshaler());
            mgr.Register(new BinaryArrayMarshaler());

            mgr.TryGetMapping<Container>(out IDeserializer<Container> d);

            var A = d.Deserialize(dataA.AsSpan(), ctx, out _);
            var B = d.Deserialize(dataB.AsSpan(), ctx, out _);

            Assert.IsType<ChildA>(A.ContentByContainerFlag);
            Assert.IsType<ChildA>(A.ContentbyPattern);
            Assert.IsType<ChildB>(B.ContentByContainerFlag);
            Assert.IsType<ChildB>(B.ContentbyPattern);

            //TODO .WithField<TFieldType>().SometimesAs<TDerrivedType>(predicate) probably gonna blow up all the fancy generic constrains when trygetmapping marshallers
            //TODO dont try any fancy ImplementationType switching, it was hell in typless incarnation, its gonna suck here too
            //TODO just add If(predicate), DeserializeIf(predicate), and SerializeIf(predicate) then just duplicate rest of annotation for now
            //TODO by keeping Descriptors for each ImplementationType fully separate, we can maintain strong typing and simplify Activation
            //TODO later on helpers can do some fancy code deduplication, gotta KISS it when making basic fraemwork :)
        }
    }
}
