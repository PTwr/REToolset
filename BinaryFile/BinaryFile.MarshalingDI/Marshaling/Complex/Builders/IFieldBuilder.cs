using Autofac;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public interface IFieldBuilder
    {
        void Register(ContainerBuilder containerBuilder, Guid objectMarshalerId);
    }
}
