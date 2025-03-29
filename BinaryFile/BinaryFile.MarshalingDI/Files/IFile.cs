using Autofac;
using BinaryFile.MarshalingDI.Marshaling.Complex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Files
{
    public interface IFile
    {
    }
    public class RawBinaryFile : IFile
    {
        public byte[] Data { get; set; } = [];

        public static void Register(ContainerBuilder builder)
        {
            new ObjectBuilder<RawBinaryFile>()
                .AlsoActivateFor<IFile>()
                .WithDefaultActivator((scope) => new RawBinaryFile())
                .WithActivationOrderOf(() => int.MaxValue)
                .WithReadingOrderOf(() => int.MaxValue)
                .WithWritingOrderOf(() => int.MaxValue)
                .WithCollectionOf<byte>()
                .AtOffset(0, DAL.OffsetRelation.Segment)
                //TODO performance here will suck, add byte[] marshaler?
                .ReadInto((x, d, b) => x.Data = d.Select(v => v.Value).ToArray())
                .WriteFrom(x => x.Data)
                .Done(builder)
                .RegisterInDI(builder);
        }
    }
}
