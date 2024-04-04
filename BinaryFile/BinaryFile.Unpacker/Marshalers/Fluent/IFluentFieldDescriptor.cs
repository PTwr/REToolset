using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Marshalers.Fluent
{
    public interface IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
        where TImplementation : IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
        TImplementation AtOffset(Func<TDeclaringType, int> offsetFunc, OffsetRelation offsetRelation = OffsetRelation.Segment);
        TImplementation AtOffset(int offset, OffsetRelation offsetRelation = OffsetRelation.Segment);

        TImplementation AsNestedFile(bool isNestedFile = true);
        TImplementation AsNestedFile(Func<TDeclaringType, bool> isNestedFileFunc);

        TImplementation WithLengthOf(Func<TDeclaringType, int> lengthFunc);
        TImplementation WithLengthOf(int length);

        TImplementation InOrder(int order);
        TImplementation InOrder(Func<TDeclaringType, int> orderFunc);
    }
    public interface IFluentFieldDescriptorEvents<TDeclaringType, TItem, TImplementation>
        where TImplementation : IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
        //TODO delegate to put names of parameters?
        TImplementation AfterSerializing(Action<TDeclaringType, int> postProcessByteLength);
    }
    public interface IFluentIntegerFieldDescriptor<TDeclaringType, TItem, TImplementation> where TImplementation : IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
        TImplementation InLittleEndian(bool inLittleEndian = true);
        TImplementation InLittleEndian(Func<TDeclaringType, bool> inLittleEndianFunc);
    }
    public interface IFluentStringFieldDescriptor<TDeclaringType, TItem, TImplementation> where TImplementation : IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
        TImplementation WithEncoding(Encoding encoding);
        TImplementation WithEncoding(Func<TDeclaringType, Encoding> encodingFunc);

        TImplementation WithNullTerminator(bool isNullTerminated = true);
        TImplementation WithNullTerminator(Func<TDeclaringType, bool> isNullTerminatedFunc);
    }
    public interface IFluentValidatedFieldDescriptor<TDeclaringType, TItem, TImplementation> where TImplementation : IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
        TImplementation WithExpectedValueOf(TItem expectedValue);
        TImplementation WithExpectedValueOf(Func<TDeclaringType, TItem> expectedValuefunc);

        TImplementation WithValidator(Func<TDeclaringType, TItem, bool> validateFunc);
    }
    public interface IFluentCollectionFieldDescriptor<TDeclaringType, TItem, TImplementation> where TImplementation : IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
        TImplementation WithCountOf(int count);
        TImplementation WithCountOf(Func<TDeclaringType, int> countFunc);

        TImplementation WithItemLengthOf(int itemLength);
        TImplementation WithItemLengthOf(Func<TDeclaringType, int> itemLengthFunc);

        TImplementation BreakWhen(Func<TDeclaringType, IEnumerable<TItem>, bool> breakPredicate);
    }
    public interface IFluentValidatedCollectionFieldDescriptor<TDeclaringType, TItem, TImplementation> where TImplementation : IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
        TImplementation WithExpectedValueOf(IEnumerable<TItem> expectedValue);
        TImplementation WithExpectedValueOf(Func<TDeclaringType, IEnumerable<TItem>> expectedValuefunc);

        TImplementation WithValidator(Func<TDeclaringType, IEnumerable<TItem>, bool> validateFunc);
    }
}
