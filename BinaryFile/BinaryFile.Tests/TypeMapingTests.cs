using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Metadata;
using ReflectionHelper;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace BinaryFile.Tests
{
    public class TypeMapingTests
    {
        class MockedDeserializer<TMappedType> : IDeserializer<TMappedType> {
            public TMappedType Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
            {
                success = true;
                return default;
            }

            public bool IsFor(Type type)
            {
                return type.IsAssignableTo<TMappedType>();
            }
        }

        [Fact]
        public void CheckIfMoreSpecificDeserializersAreMatchedFirst()
        {
            var manager = new DeserializerManager();

            manager.Register(new MockedDeserializer<byte[]>());
            manager.Register(new MockedDeserializer<Array>());
            manager.Register(new MockedDeserializer<IEnumerable<byte>>());
            manager.Register(new MockedDeserializer<IEnumerable>());

            {
                Assert.True(manager.TryGetMapping<byte[]>(out var deserializer));
                Assert.NotNull(deserializer);
                Assert.IsType<MockedDeserializer<byte[]>>(deserializer);

                Assert.True(deserializer.IsFor(typeof(byte[])));
                Assert.False(deserializer.IsFor(typeof(Array)));
                Assert.False(deserializer.IsFor(typeof(IEnumerable<byte>)));
                Assert.False(deserializer.IsFor(typeof(IEnumerable)));
            }
            {
                Assert.True(manager.TryGetMapping<Array>(out var deserializer));
                Assert.NotNull(deserializer);
                Assert.IsType<MockedDeserializer<Array>>(deserializer);

                Assert.True(deserializer.IsFor(typeof(byte[])));
                Assert.True(deserializer.IsFor(typeof(Array)));
                Assert.False(deserializer.IsFor(typeof(IEnumerable<byte>)));
                Assert.False(deserializer.IsFor(typeof(IEnumerable)));
            }
            {
                Assert.True(manager.TryGetMapping<IEnumerable<byte>>(out var deserializer));
                Assert.NotNull(deserializer);
                Assert.IsType<MockedDeserializer<IEnumerable<byte>>>(deserializer);

                Assert.True(deserializer.IsFor(typeof(byte[])));
                Assert.False(deserializer.IsFor(typeof(Array))); //Array is non-generic half-legacy code :)
                Assert.True(deserializer.IsFor(typeof(IEnumerable<byte>)));
                Assert.False(deserializer.IsFor(typeof(IEnumerable)));
            }
            {
                Assert.True(manager.TryGetMapping<IEnumerable>(out var deserializer));
                Assert.NotNull(deserializer);
                Assert.IsType<MockedDeserializer<IEnumerable>>(deserializer);

                Assert.True(deserializer.IsFor(typeof(byte[])));
                Assert.True(deserializer.IsFor(typeof(Array)));
                Assert.True(deserializer.IsFor(typeof(IEnumerable<byte>)));
                Assert.True(deserializer.IsFor(typeof(IEnumerable)));
            }
            {
                Assert.False(manager.TryGetMapping<DateTime>(out var deserializer));
                Assert.Null(deserializer);
            }
        }
    }
}