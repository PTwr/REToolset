using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker.New;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Marshalers.Fluent
{
    public class FluentMarshalingContext<TDeclaringType, TItem> : MarshalingContext
    {
        private readonly DynamicCommonMarshalingMetadata<TDeclaringType, TItem> metadata;
        private readonly TDeclaringType declaringObject;

        public FluentMarshalingContext(string? name, IMarshalingContext? parent, OffsetRelation offsetRelation, int relativeOffset, DynamicCommonMarshalingMetadata<TDeclaringType, TItem> metadata, TDeclaringType declaringObject, int offsetCorrection = 0)
            : base(parent, offsetRelation, relativeOffset, offsetCorrection)
        {
            Name = name;
            this.metadata = metadata;
            this.declaringObject = declaringObject;
        }

        public override IMarshalingContext Find(OffsetRelation offsetRelation) => metadata.IsNestedFile?.Get(declaringObject) ?? false ?
            offsetRelation is OffsetRelation.Absolute or OffsetRelation.Segment ? this :
                throw new ArgumentException($"{Name}. Looking for ancestor DataOffset of NestedFile. Remaining OffsetRelation = {offsetRelation}")
            : base.Find(offsetRelation);

        public override int? Length => metadata.Length?.Get(declaringObject);
        public override Encoding? Encoding => metadata.Encoding?.Get(declaringObject);
        public override bool? NullTerminated => metadata.NullTerminated?.Get(declaringObject);
        public override bool? LittleEndian => metadata.LittleEndian?.Get(declaringObject);

        public override TType Activate<TType>()
        {
            if (declaringObject is not null)
            {
                var ctor = typeof(TType).GetConstructor([typeof(TDeclaringType)]);
                if (ctor is not null) return (TType)ctor.Invoke([declaringObject]);
            }
            return base.Activate<TType>();
        }
    }
}
