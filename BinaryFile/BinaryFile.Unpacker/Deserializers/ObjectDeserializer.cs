﻿using Microsoft.VisualBasic.FileIO;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Deserializers
{
    public class ObjectDeserializer<TMappedType> : SerializedAndDeserializerBase<TMappedType>, IDeserializer<TMappedType>
        where TMappedType : class
    {
        private List<IFieldDescriptor<TMappedType>> fields = new List<IFieldDescriptor<TMappedType>>();
        private readonly bool applyToInheritingClasses;

        public ObjectDeserializer(bool applyToInheritingClasses = false)
        {
            this.applyToInheritingClasses = applyToInheritingClasses;
        }

        public bool TryDeserialize(Span<byte> bytes, [NotNullWhen(true)] out TMappedType result)
        {
            //TODO value reuse - use Getter in FieldDescriptor?
            //TODO allow passing RootObj from outside
            //TODO ISegment<TParent> custom initiation - somehow made it fit nicely with other options :D
            result = Activator.CreateInstance(MappedType) as TMappedType;

            if (result is null) throw new Exception($"Failed to create object for ${MappedType.FullName}");

            //TODO type activation

            var f = fields.Where(i => i.Deserialize);
            if (!f.Any())
            {
                return false;
            }

            foreach (var fd in f)
            {
                //TODO pass context to manage position
                if (!fd.TryDeserialize(bytes, result)) return false;
            }

            return true;
        }

        public FieldDescriptor<TMappedType, TFieldType> Field<TFieldType>(Expression<Func<TMappedType, TFieldType>> getterExpression)
        {
            var field = new FieldDescriptor<TMappedType, TFieldType>(this, getterExpression);

            fields.Add(field);

            return field;
        }

        public override bool IsFor(Type type)
        {
            if (applyToInheritingClasses)
            {
                return type.IsAssignableTo(MappedType);
            }
            else
            {
                //exact type match
                return MappedType == type;
            }
        }
    }
    public enum OffsetRelation : int
    {
        Absolute = -1,
        Segment = 0,
        Parent = 1,
        GrandParent = 2,
    }
    public interface IFieldDescriptor
    {
        bool Deserialize { get; }
        bool Serialize { get; }

        Type DeclaringType { get; }
        FieldInfo FieldInfo { get; }
        Type FieldType { get; }

        int Order { get; }
    }
    public interface IFieldDescriptor<TDeclaringType> : IFieldDescriptor
        where TDeclaringType : class
    { 
        bool TryDeserialize(Span<byte> bytes, TDeclaringType declaringObject);
    }

    public abstract class FieldDescriptor : IFieldDescriptor
    {
        public bool Deserialize { get; protected set; }
        public bool Serialize { get; protected set; }

        public Type DeclaringType { get; protected set; }
        public FieldInfo FieldInfo { get; protected set; }
        public Type FieldType { get; protected set; }

        public int Order { get; protected set; } = 0;
    }

    public class FieldDescriptor<TDeclaringType, TFieldType> : FieldDescriptor, IFieldDescriptor<TDeclaringType>
        where TDeclaringType : class
    {
        OffsetRelation OffsetRelation;
        int? Offset;
        Func<TDeclaringType, int>? OffsetFunc;

        public bool TryDeserialize(Span<byte> bytes, TDeclaringType declaringObject)
        {
            if (Deserialize == false) return false;
            if (declaringObject == null) return false;

            //TODO pass context through ObjectDeserializer to get into deserializermanager
            TFieldType v = default;

            //TODO calculate AbsoluteOffset through context
            //TODO same for length
            int offset = OffsetFunc?.Invoke(declaringObject) ?? Offset ?? throw new Exception("Neither Offset nor OffsetFunc has been set!");
            var fieldSlice = bytes.Slice(offset);

            Setter(declaringObject, v);

            return true;
        }

        public Func<TDeclaringType, TFieldType> Getter { get; }
        public Action<TDeclaringType, TFieldType> Setter { get; }

        private readonly ObjectDeserializer<TDeclaringType> deserializer;

        public FieldDescriptor(ObjectDeserializer<TDeclaringType> deserializer, Expression<Func<TDeclaringType, TFieldType>> getterExpression)
        {
            FieldType = typeof(TFieldType);
            DeclaringType = typeof(TDeclaringType);
            this.deserializer = deserializer;

            //it is assumed that reading from object is always possible
            Serialize = true;
            Getter = getterExpression.Compile();

            //writing to object require Field or public Setter
            if (getterExpression.TryGenerateToSetter(out var setter))
            {
                Deserialize = true;
                Setter = setter.Compile();
            }
        }

        public FieldDescriptor<TDeclaringType, TFieldType> OnlyDeserialize()
        {
            Deserialize = true;
            Serialize = false;
            return this;
        }

        public FieldDescriptor<TDeclaringType, TFieldType> OnlySerialize()
        {
            Deserialize = false;
            Serialize = true;
            return this;
        }

        public FieldDescriptor<TDeclaringType, TFieldType> AtOffset(OffsetRelation offsetRelation, int offset)
        {
            OffsetRelation = offsetRelation;
            Offset = offset;
            OffsetFunc = null;
            return this;
        }
        public FieldDescriptor<TDeclaringType, TFieldType> AtOffset(OffsetRelation offsetRelation, Func<TDeclaringType, int> offsetFunc)
        {
            OffsetRelation = offsetRelation;
            OffsetFunc = offsetFunc;
            return this;
        }

        /// <summary>
        /// Stepout to ObjectDeserializer.Field 
        /// </summary>
        /// <typeparam name="TAnotherFieldType"></typeparam>
        /// <param name="getterExpression"></param>
        /// <returns></returns>
        public FieldDescriptor<TDeclaringType, TAnotherFieldType> Field<TAnotherFieldType>(Expression<Func<TDeclaringType, TAnotherFieldType>> getterExpression)
        {
            return deserializer.Field<TAnotherFieldType>(getterExpression);
        }

        /// <summary>
        /// Stepout to Deserializer context
        /// </summary>
        /// <returns></returns>
        public ObjectDeserializer<TDeclaringType> Done()
        {
            return deserializer;
        }
    }
}