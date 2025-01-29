using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.DI
{
    public interface IDIRegistrar
    {
        void Register(ContainerBuilder containerBuilder);
    }
    public sealed class PrimitiveMarshalingRegistrar : IDIRegistrar
    {
        public void Register(ContainerBuilder containerBuilder)
        {

        }
    }
}
