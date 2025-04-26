using Autofac;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Context
{
    public class Padding : IFeature<IPadding>, IPadding
    {
        private readonly IFeatureSet containingFeatureSet;
        private readonly ILifetimeScope diScope;
        private readonly Func<IFeatureSet, ILifetimeScope, int, (int padby, bool write)> padder;
        private readonly byte padValue;

        public IDataBuffer IO { get; }
        public IOffsetStack Offset { get; }

        public Padding(IFeatureSet containingFeatureSet, ILifetimeScope diScope, Func<IFeatureSet, ILifetimeScope, int,(int padby, bool write)> padder, byte padValue = 0)
        {
            this.containingFeatureSet = containingFeatureSet;
            this.diScope = diScope;
            this.padder = padder;
            this.padValue = padValue;

            this.IO = diScope.Resolve<IDataBuffer>();
            this.Offset = diScope.Resolve<IOffsetStack>();
        }

        public string? Name => nameof(EMetadataNames.Padding);

        public int MaxEffectiveAge => 0;

        public IPadding GetValue() => this;

        public void Pad(int bytesRead, out int paddedBytesRead)
        {
            (int padBy, bool write) = padder(containingFeatureSet, diScope, bytesRead);
            paddedBytesRead = bytesRead+padBy;

            //TODO Read/Write mode detection?
            if (write)
            {
                IO.Emplace(Offset.CurrentAbsoluteOffset, Enumerable.Repeat(padValue, padBy).ToArray());
            }
        }
    }
}
