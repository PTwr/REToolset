using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReflectionHelper;

namespace Helpers.Tests.ReflectionHelperTests
{
    public class CastingTests
    {
        [Fact]
        public void Test()
        {
            Assert.True(typeof(byte[]).IsAssignableTo<byte[]>());
            Assert.True(typeof(byte[]).IsAssignableTo<IEnumerable<byte>>());
            Assert.True(typeof(byte[]).IsAssignableTo<IEnumerable>());
        }
    }
}
