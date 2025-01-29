using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Tests
{
    public class TypeCrawlingTests
    {
        [Fact]
        public void BasicTest()
        {
            var bc = typeof(Ccc).BaseType;
            var bb = typeof(Bbb).BaseType;
            var ba = typeof(Aaa).BaseType;
            //object has basetype = null

            //no need to crawl for interfaces, exact type has them all
            var ic = typeof(Ccc).GetInterfaces();
            var ib = typeof(Bbb).GetInterfaces();
            var ia = typeof(Aaa).GetInterfaces();

            var tt = typeof(Blah<int>);
            while(tt!= null)
            {
                tt = tt.BaseType;
            }
        }
    }

    interface A { }
    interface B { }
    interface C { }
    interface D : C { }
    interface E<T> { }

    public class Aaa : A
    {

    }
    public class Bbb : Aaa, B
    {

    }
    public class Ccc : Bbb, D
    {
        
    }
    public class Blah<T> : Bbb, E<T>
    {

    }
}
