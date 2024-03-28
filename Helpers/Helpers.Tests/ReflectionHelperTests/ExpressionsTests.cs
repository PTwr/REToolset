using ReflectionHelper;

namespace Helpers.Tests.ReflectionHelperTests
{
    public class ExpressionsTests
    {
        class Foo
        {
            public int Bar { get; set; }
            public int Blah;

            public int Private { get; private set; }
        }
        [Fact]
        public void ChangeGetterExpressionIntoSetter()
        {
            Foo foo = new Foo()
            {
                Bar = 1,
                Blah = 2,
            };

            var setter = Expressions.GenerateToSetter<Foo, int>(foo => foo.Blah);

            setter.Compile()(foo, 123);
            Assert.Equal(123, foo.Blah);

            setter = Expressions.GenerateToSetter<Foo, int>(foo => foo.Bar);

            setter.Compile()(foo, 456);
            Assert.Equal(456, foo.Bar);

            Assert.False(Expressions.TryGenerateToSetter<Foo, int>(foo => foo.Private, out setter));
        }
    }
}