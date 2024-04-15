using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public interface ITest
    {
        int Foo { get; set; }
    }
    public class Test : ITest
    {
        public int Foo { get; set; }
    }
    public interface ITestA : ITest
    {
        int Bar { get; set; }
    }
    public class TestA : Test, ITestA
    {
        public int Bar { get; set; }
    }

    public class TypeMap<TTMappedType>
    {
        public List<IFieldMap<TTMappedType>> Fields = new List<IFieldMap<TTMappedType>>();
    }

    public interface IFieldMap<out TMappedType>
    {
    }
    public interface IFieldMapB<TMappedType> : IFieldMap<TMappedType>
    {
        void Read(TMappedType obj, in int[] data);
        void Write(TMappedType obj, int[] data);
    }
    public interface IFieldMapC<TMappedType, TItem, TImplementation> : IFieldMapB<TMappedType>
        where TImplementation : BaseFieldMap<TMappedType, TItem, TImplementation>
    {
        TImplementation AtOffset(int offset);
        TImplementation WithGetter(Func<TMappedType, TItem> getter);
        TImplementation WithSetter(Action<TMappedType, TItem> setter);
    }

    //TODO this would require inheritance branch to provide this class with no TImplementation param if its to be used directly, otherwise it should be abstract
    public class BaseFieldMap<TMappedType, TItem, TImplementation> : IFieldMapC<TMappedType, TItem, TImplementation>
        where TImplementation : BaseFieldMap<TMappedType, TItem, TImplementation>
    {
        int offset;
        Func<TMappedType, TItem> getter;
        Action<TMappedType, TItem> setter;

        public void Read(TMappedType obj, in int[] data)
        {
            var val = data[offset];
            Console.WriteLine(val);

            if (getter != null)
            {
                //temp hack due to lack of marshalers here :D
                TItem x = (TItem)Convert.ChangeType(val, typeof(TItem));
                setter.Invoke(obj, x);
            }
        }

        public void Write(TMappedType obj, int[] data)
        {
            if (getter != null)
            {
                var val = getter.Invoke(obj);

                if (val is int)
                {
                    //temp hack due to lack of marshalers here :D
                    int xx = (int)Convert.ChangeType(val, typeof(int));
                    data[offset] = xx;
                }
            }
        }

        public TImplementation AtOffset(int offset)
        {
            this.offset = offset;
            return (TImplementation)this;
        }

        public TImplementation WithGetter(Func<TMappedType, TItem> getter)
        {
            this.getter = getter;
            return (TImplementation)this;
        }

        public TImplementation WithSetter(Action<TMappedType, TItem> setter)
        {
            this.setter = setter;
            return (TImplementation)this;
        }

        public bool IsFor<T>() where T : TMappedType
        {
            return false;
        }
    }
    //TODO just skip this and pass same type as TDerriviedType as TMappedType?
    public class FieldMap<TMappedType, TItem>
        : BaseFieldMap<TMappedType, TItem, FieldMap<TMappedType, TItem>>
    {
    }
    public class DerivedFieldMap<TDerivedType, TMappedType, TItem>
        : BaseFieldMap<TDerivedType, TItem, DerivedFieldMap<TDerivedType, TMappedType, TItem>>
        where TDerivedType : TMappedType
    {
    }

    public static class ConditionalGenerics
    {
        public static void Test()
        {
            IFieldMap<ITest> a = new FieldMap<Test, int>()
                .AtOffset(0)
                .WithGetter(i => i.Foo)
                .WithSetter((i, x) => i.Foo = x);
            IFieldMap<ITestA> b = new DerivedFieldMap<TestA, Test, int>()
                .AtOffset(1)
                .WithGetter(i => i.Bar)
                .WithSetter((i, x) => i.Bar = x);
            IFieldMap<ITestA> c = new DerivedFieldMap<TestA, TestA, int>()
                .AtOffset(2)
                .WithGetter(i => i.Bar)
                .WithSetter((i, x) => i.Bar = x);

            List<IFieldMap<ITest>> maps = new List<IFieldMap<ITest>>();
            List<IFieldMap<ITestA>> mapsA = new List<IFieldMap<ITestA>>();
            maps.Add(a);
            mapsA.Add(b);
            mapsA.Add(c);

            var objA = new Test() { Foo = 1 };
            var objB = new TestA() { Foo = 2 };
            var objC = new TestA() { Foo = 3 };

            int[] input = [7, 8, 9];
            int[] output1 = [0, 0, 0];
            int[] output2 = [0, 0, 0];
            int[] output3 = [0, 0, 0];

            foreach (var m in maps)
            {
                //gotta cast to child to get methods breaking covariance
                IFieldMapB<Test> map = (IFieldMapB<Test>)m;

                map.Read(objA, input);
                map.Read(objB, input);
                map.Read(objC, input);

                map.Write(objA, output1);
                map.Write(objB, output2);
                map.Write(objC, output3);
            }

            foreach (var m in mapsA)
            {
                //gotta cast to child to get methods breaking covariance
                IFieldMapB<TestA> map = (IFieldMapB<TestA>)m;

                //base type is not passable
                //map.Read(objA, data);
                map.Read(objB, input);
                map.Read(objC, input);

                map.Write(objB, output2);
                map.Write(objC, output3);
            }
        }
    }
}
