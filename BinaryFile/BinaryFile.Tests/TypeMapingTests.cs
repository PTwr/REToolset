using BinaryFile.Unpacker;
using System.Collections;

namespace BinaryFile.Tests
{
    public class TypeMapingTests
    {
        class MockedMapping<TMapedType> : TypeMapping<TMapedType> { }
        [Fact]
        public void ConfirmThatRegisteringWorks()
        {
            var manager = new TypeMappingManager();

            manager.Register(new MockedMapping<byte[]>());
            manager.Register(new MockedMapping<IEnumerable<byte>>());
            manager.Register(new MockedMapping<IEnumerable>());

            {
                Assert.True(manager.TryGetMapping<byte[]>(out var mapping));
                Assert.IsType<MockedMapping<byte[]>>(mapping);

                Assert.True(mapping.IsFor(typeof(byte[])));
                Assert.False(mapping.IsFor(typeof(IEnumerable<byte>)));
                Assert.False(mapping.IsFor(typeof(IEnumerable)));
            }
            {
                Assert.True(manager.TryGetMapping<IEnumerable<byte>>(out var mapping));
                Assert.IsType<MockedMapping<IEnumerable<byte>>>(mapping);

                Assert.True(mapping.IsFor(typeof(byte[])));
                Assert.True(mapping.IsFor(typeof(IEnumerable<byte>)));
                Assert.False(mapping.IsFor(typeof(IEnumerable)));
            }
            {
                Assert.True(manager.TryGetMapping<IEnumerable>(out var mapping));
                Assert.IsType<MockedMapping<IEnumerable>>(mapping);

                Assert.True(mapping.IsFor(typeof(byte[])));
                Assert.True(mapping.IsFor(typeof(IEnumerable<byte>)));
                Assert.True(mapping.IsFor(typeof(IEnumerable)));
            }
        }
    }
}