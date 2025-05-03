using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Xml;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Hello, World!");
        }
    }
    delegate void Del<in T>(T t);
    interface IReader<out T>
    {
        T Read(T a);
    }
    interface IWriter<in T>
    {
        T Write(T t);
    }
    interface IWriter2<T>
    {
        Del<T> Blah();
    }
}
