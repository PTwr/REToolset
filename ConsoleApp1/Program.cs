using System.Buffers.Binary;
using System.Collections;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Xml;
using static ConsoleApp1.Program;

namespace ConsoleApp1
{
    internal class Program
    {
        public interface ITest
        {
            public int Foo { get; set; }
        }
        public class Test1 : ITest
        {
            public int Foo { get; set; }
        }
        public class Test2 : ITest
        {
            public int Foo { get; set; }
        }
        public class Test3 : ITest
        {
            public int Foo { get; set; }
        }

        public interface IStuffer<out TImplementation>
        {
            TImplementation Stuff();
        }

        public class StufferArrays : IStuffer<byte[]>, IStuffer<int[]>
        {
            byte[] IStuffer<byte[]>.Stuff()
            {
                return new byte[] { 1, 2, 3 };
            }

            int[] IStuffer<int[]>.Stuff()
            {
                return new int[] { 1024, 2048, 4096 };
            }
        }

        public class StufferSane : IStuffer<Test1>, IStuffer<List<byte>>, IStuffer<string>, IStuffer<int>, IStuffer<sbyte>, IStuffer<byte>
        {
            Test1 IStuffer<Test1>.Stuff()
            {
                return new Test1() { Foo = 123 };
            }

            List<byte> IStuffer<List<byte>>.Stuff()
            {
                return [1, 2, 3];
            }

            string IStuffer<string>.Stuff()
            {
                return "string representation";
            }

            int IStuffer<int>.Stuff()
            {
                return 1024;
            }

            sbyte IStuffer<sbyte>.Stuff()
            {
                return -1;
            }

            byte IStuffer<byte>.Stuff()
            {
                return 255;
            }
        }

        //when casting to IStuffer<IEnumerable>, to which are implementations are assignable, first matching interface will be selected
        //therefore interface declarations should be listed from most derived to most base
        public class StufferCollectionsA : IStuffer<IEnumerable<byte>>, IStuffer<IList<byte>>
        {
            IEnumerable<byte> IStuffer<IEnumerable<byte>>.Stuff()
            {
                return [1, 2, 3];
            }

            IList<byte> IStuffer<IList<byte>>.Stuff()
            {
                return [3, 2, 1];
            }
        }

        //when casting to IStuffer<IEnumerable>, to which are implementations are assignable, first matching interface will be selected
        public class StufferCollectionsB : IStuffer<IList<byte>>, IStuffer<IEnumerable<byte>>
        {
            IEnumerable<byte> IStuffer<IEnumerable<byte>>.Stuff()
            {
                return [1, 2, 3];
            }

            IList<byte> IStuffer<IList<byte>>.Stuff()
            {
                return [3, 2, 1];
            }
        }

        interface ICovariant<out T>
        {
            T Foo();
            //void Foo(out T foo);
        }

        interface IContrvariant<in T>
        {
            void Foo(T foo);
            //void Foo(out T foo);
        }

        static void EndianTEst()
        {

            var bytes = new byte[]
            {
                0xD2, 0x02, 0x96, 0x49, //1234567890 little endian
                0x49, 0x96, 0x02, 0xD2, //1234567890 big endian
            };

            var little = bytes.AsSpan(0, 4);
            var big = bytes.AsSpan(4, 4);

            var x = BinaryPrimitives.ReadInt32LittleEndian(little);
            var y = BinaryPrimitives.ReadInt32BigEndian(big);
        }

        static void WriteToSpanTest()
        {
            var bytes = new byte[] {
                0, 1, 2, 3,
                4, 5, 6, 7,
            };

            var span = bytes.AsSpan();

            span[2] = 9;

            Array.Resize(ref bytes, 16);
            span[3] = 99;
            //span[12] = 12; //out of bounds

            var span2 = bytes.AsSpan();

            span2[8] = 123;

            var slice = bytes.AsSpan(4, 4);

            slice.CopyTo(span2.Slice(12, 4));
        }

        static void XmlTraversing()
        {
            var xmlstr = @"
<Contacts>
   <Contact X=""abc"">
       <Child1>blah</Child1>
   </Contact>
asdasd
   <Contact X=""def"">
       <Child2>xxx</Child2>
   </Contact>
</Contacts>
";
            var doc = new XmlDocument();
            doc.LoadXml(xmlstr);
            //TODO serialize Xml directly to Xbf by traversing tree? or use model as intermediate? gotta do XBF<->XML conversions. Gotta ignore Xml root node for xbf and/or ensure xml->str wont add it to file
            //get root element of document   
            XmlElement root = doc.DocumentElement;
            //select all contact element having attribute X
            XmlNodeList nodeList = root.SelectNodes("//*");
            //loop through the nodelist
            foreach (XmlNode xNode in nodeList)
            {
                var texts = xNode.ChildNodes.Cast<XmlNode>().Where(i => i.NodeType is XmlNodeType.Text).Select(i => i.Value);
                Console.WriteLine(xNode.Name);
                Console.WriteLine(string.Concat(texts));
                foreach (XmlAttribute attr in xNode.Attributes)
                {
                    Console.WriteLine(attr.Name + " = " + attr.Value);
                }
                //traverse all childs of the node
            }
        }

        static void Main(string[] args)
        {
            XmlTraversing();
            WriteToSpanTest();
            EndianTEst();

            var reversingTest = new byte[] { 1, 2, 3, 4 };

            var xxx = reversingTest.AsSpan();
            var yyy = xxx.Slice(1, 2);
            yyy.Reverse();

            var xx = xxx.ToArray();
            var yy = yyy.ToArray();

            var data = new byte[] {
                0,0,0,0,
                1,0,0,0,
                3,1,0,0,
                4,0,1,0,
                0,0,0,1,
            };

            var bb = MemoryMarshal.Read<bool>(data.AsSpan());
            var bbbb = MemoryMarshal.Read<bool>(data.AsSpan(4));
            var bbbbbb = MemoryMarshal.Read<bool>(data.AsSpan(8));
            var bbbbbbbb = MemoryMarshal.Read<bool>(data.AsSpan(16));

            var stufferSane = new StufferSane();
            var instance = (stufferSane as IStuffer<ITest>).Stuff();
            var str = (stufferSane as IStuffer<string>).Stuff();

            //Covariance magic
            var list = (stufferSane as IStuffer<IList<byte>>).Stuff();
            var enumerable = (stufferSane as IStuffer<IEnumerable<byte>>).Stuff();
            var typlesenumerable = (stufferSane as IStuffer<IEnumerable>).Stuff();

            var bite = (stufferSane as IStuffer<byte>).Stuff();
            var sbite = (stufferSane as IStuffer<sbyte>).Stuff();
            var integer = (stufferSane as IStuffer<int>).Stuff();

            //Compiler warning and runtime null error, perfect!
            var datetime = (stufferSane as IStuffer<DateTime>).Stuff();


            StufferArrays sa = new StufferArrays();
            //even works on annoying pseudo primitives
            var bytes = (sa as IStuffer<byte[]>).Stuff();
            var ints = (sa as IStuffer<int[]>).Stuff();

            StufferCollectionsA sc = new StufferCollectionsA();

            IStuffer<IEnumerable<byte>> sce = sc;
            var scex = sce.Stuff();
            IStuffer<IList<byte>> scl = sc;
            var sclx = scl.Stuff();
            //IStuffer<List<byte>> scll = sc;
            //var scllx = scl.Stuff();

            IStuffer<IEnumerable> EnumThenList = new StufferCollectionsA();
            var x = EnumThenList.Stuff();

            IStuffer<IEnumerable> ListThenEnum = new StufferCollectionsB();
            var y = ListThenEnum.Stuff();

            ICovariant<IEnumerable> c1 = null;
            ICovariant<IList> c2 = null;

            c1 = c2;
            //c2 = c1;

            IContrvariant<IEnumerable> cc1 = null;
            IContrvariant<IList> cc2 = null;

            //cc1 = cc2;
            cc2 = cc1;

            var test = new Test1();
            var pi = typeof(ITest).GetProperty("Foo");
            pi.SetValue(test, 777);

            Console.WriteLine("Hello, World!");
        }
    }
}
