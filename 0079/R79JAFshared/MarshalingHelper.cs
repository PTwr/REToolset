using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Formats.Nintendo;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R79JAFshared
{
    public static class MarshalingHelper
    {
        public static ITypeMarshaler<XBFFile> mXBF;
        public static ITypeMarshaler<U8File> mU8;
        public static ITypeMarshaler<GEV> mGEV;
        public static IMarshalingContext ctx;

        static MarshalingHelper()
        {
            ctx = PrepXBFMarshaling(out mXBF, out mU8, out mGEV);
        }

        public static IMarshalingContext PrepXBFMarshaling(out ITypeMarshaler<XBFFile> mXBF, out ITypeMarshaler<U8File> mU8
            , out ITypeMarshaler<GEV> mGEV)
        {
            var store = new DefaultMarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            U8Marshaling.Register(store);
            XBFMarshaling.Register(store);
            GEVMarshaling.Register(store);

            mXBF = store.FindMarshaler<XBFFile>();

            mU8 = store.FindMarshaler<U8File>();

            mGEV = store.FindMarshaler<GEV>();

            return rootCtx;
        }
    }
}
