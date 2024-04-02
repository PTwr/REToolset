﻿using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Marshalers.__Fluent
{
    //TODO is this just duplicating base class? clean up!
    public class FluentFieldDeserializationContext<TDeclaringType, TItem> : MarshalingContext
    {
        private readonly _BaseFluentFieldDescriptor<TDeclaringType, TItem> fieldDescriptor;
        private readonly TDeclaringType declaringObject;

        //TODO this is ugly, this metadata needs to be passed to from Descriptor to Deserializers but results in hard coupling and extraneous fields
        public override int? Length => fieldDescriptor.Length?.Get(declaringObject);
        public override Encoding? Encoding => fieldDescriptor.Encoding?.Get(declaringObject);
        public override bool? NullTerminated => fieldDescriptor.NullTerminated?.Get(declaringObject);
        public override bool? LittleEndian => fieldDescriptor.LittleEndian?.Get(declaringObject);

        public FluentFieldDeserializationContext(MarshalingContext? parent, OffsetRelation offsetRelation, int relativeOffset, _BaseFluentFieldDescriptor<TDeclaringType, TItem> fieldDescriptor, TDeclaringType declaringObject)
            : base(parent, offsetRelation, relativeOffset)
        {
            this.fieldDescriptor = fieldDescriptor;
            this.declaringObject = declaringObject;
        }

        //short circuit Absolute for nested file
        //TODO clean rewrite, this is fugly :D
        public override IMarshalingContext Find(OffsetRelation offsetRelation) => fieldDescriptor.IsNestedFile?.Get(declaringObject) ?? false ?
            offsetRelation is OffsetRelation.Absolute or OffsetRelation.Segment ? this :
                throw new ArgumentException($"{fieldDescriptor}. Looking for ancestor DataOffset of NestedFile. Remaining OffsetRelation = {offsetRelation}")
            : base.Find(offsetRelation);
    }
    //TODO is this just duplicating base class? clean up!
    public class FluentFieldSerializationContext<TDeclaringType, TItem> : SerializationContext
    {
        private readonly _BaseFluentFieldDescriptor<TDeclaringType, TItem> fieldDescriptor;
        private readonly TDeclaringType declaringObject;

        //TODO this is ugly, this metadata needs to be passed to from Descriptor to Deserializers but results in hard coupling and extraneous fields
        public override int? Length => fieldDescriptor.Length?.Get(declaringObject);
        public override Encoding? Encoding => fieldDescriptor.Encoding?.Get(declaringObject);
        public override bool? NullTerminated => fieldDescriptor.NullTerminated?.Get(declaringObject);
        public override bool? LittleEndian => fieldDescriptor.LittleEndian?.Get(declaringObject);

        public FluentFieldSerializationContext(SerializationContext? parent, OffsetRelation offsetRelation, int relativeOffset, _BaseFluentFieldDescriptor<TDeclaringType, TItem> fieldDescriptor, TDeclaringType declaringObject)
            : base(parent, offsetRelation, relativeOffset)
        {
            this.fieldDescriptor = fieldDescriptor;
            this.declaringObject = declaringObject;
        }

        //short circuit Absolute for nested file
        //TODO clean rewrite, this is fugly :D
        public override ISerializationContext Find(OffsetRelation offsetRelation) => fieldDescriptor.IsNestedFile?.Get(declaringObject) ?? false ?
            offsetRelation is OffsetRelation.Absolute or OffsetRelation.Segment ? this :
                throw new ArgumentException($"{fieldDescriptor}. Looking for ancestor DataOffset of NestedFile. Remaining OffsetRelation = {offsetRelation}")
            : base.Find(offsetRelation);
    }
}
