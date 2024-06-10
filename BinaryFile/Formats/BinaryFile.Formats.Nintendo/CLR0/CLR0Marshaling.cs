using BinaryDataHelper;
using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;

namespace BinaryFile.Formats.Nintendo.CLR0
{
    public static class CLR0Marshaling
    {
        public static void Register(DefaultMarshalerStore marshalerStore)
        {
            var rgba = new RootTypeMarshaler<RGBA>();

            rgba.WithByteLengthOf(4);
            rgba.WithField(i => i.R).AtOffset(0);
            rgba.WithField(i => i.G).AtOffset(1);
            rgba.WithField(i => i.B).AtOffset(2);
            rgba.WithField(i => i.A).AtOffset(3);

            marshalerStore.Register(rgba);

            var CLR0AnimationData = new RootTypeMarshaler<CLR0AnimationData>();

            CLR0AnimationData.WithField(i => i.Mask).AtOffset(0);
            CLR0AnimationData.WithField(i => i.DataOffset).AtOffset(4);
            CLR0AnimationData.WithField(i => i.DataCount).AtOffset(8);
            CLR0AnimationData
                .WithCollectionOf(i => i.Data)
                .AtOffset(i => i.DataOffset - 4)
                .WithCountOf(i => i.DataCount);

            marshalerStore.Register(CLR0AnimationData);

            var CLR0MaterialData = new RootTypeMarshaler<CLR0MaterialData>();

            CLR0MaterialData.WithField(i => i.TargetNameOffset).AtOffset(0);
            CLR0MaterialData.WithField(i => i.AnimationFlag).AtOffset(4);
            CLR0MaterialData.WithField(i => i.AnimationData).AtOffset(8);

            marshalerStore.Register(CLR0MaterialData);

            var CLR0FileV3 = new RootTypeMarshaler<CLR0FileV3>();

            CLR0FileV3.WithField(i => i.Unused).WithExpectedValueOf(0).AtOffset(0);
            CLR0FileV3.WithField(i => i.FrameCount).AtOffset(4);
            CLR0FileV3.WithField(i => i.MaterialsCount).AtOffset(6);
            CLR0FileV3.WithField(i => i.LoopingEnabled).AtOffset(8);

            //CLR0FileV3.WithCollectionOf(i => i.MaterialData).AtOffset(12)
            //    .WithCountOf(i => i.MaterialsCount);

            marshalerStore.Register(CLR0FileV3);

            var CLR0File = marshalerStore.DeriveBinaryFile<CLR0File>(new CustomActivator<IBinaryFile>((data, ctx) =>
            {
                //0x43_4c_52_30 -> CLR0
                if (ctx.ItemSlice(data).Span.StartsWith([0x43, 0x4C, 0x52, 0x30]))
                    return new CLR0File();
                return null;
            }));

            CLR0File.WithField(i => i.Magic).AtOffset(0)
                .WithExpectedValueOf("CLR0")
                .WithByteLengthOf(4);

            CLR0File.WithField(i => i.SubFileLength).AtOffset(4);
            CLR0File.WithField(i => i.SubFileVersion).AtOffset(8);
            CLR0File.WithField(i => i.OuterBrresOffset).AtOffset(12);
            CLR0File.WithField(i => i.DataOffset).AtOffset(16);
            CLR0File.WithField(i => i.FileNameOffset).AtOffset(20);
            
            //todo nullpad to alignment
            CLR0File.WithField(i => i.FileName)
                .AtOffset(i => i.FileNameOffset)
                .RelativeTo(OffsetRelation.Absolute)
                .WithNullTerminator();

            CLR0File.WithField(i => i.V3).AtOffset(24);
        }
    }

}
