using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Tests
{
    interface IFoo
    {
        string Foo { get; set; }
    }

    class X : IFoo
    {
        public string Foo { get; set; }
        public override string ToString()
        {
            return "X" + Foo;
        }
    }
    class Y : X
    {
        public string Blah { get; set; }

    }
    class Z : Y
    {
        public string Bleh { get; set; }
    }

    class A : IFoo
    {
        public A()
        {
            
        }
        public A(object parent)
        {
            Parent = parent;
        }

        public string Foo { get; set; }
        public object Parent { get; private set; }

        public override string ToString()
        {
            return "A" + Foo;
        }
    }
    class B : A { public int X { get; set; } }
    class C1 : B { public int Y { get; set; } }
    class C2 : B { public int Z { get; set; } }
}
