using Microsoft.VisualBasic.FileIO;
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
            result = default;

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

        OffsetRelation OffsetRelation { get; }
        int Offset { get; }

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

        public OffsetRelation OffsetRelation { get; protected set; }
        public virtual int Offset { get; protected set; }

        public Type DeclaringType { get; protected set; }
        public FieldInfo FieldInfo { get; protected set; }
        public Type FieldType { get; protected set; }

        public int Order { get; protected set; } = 0;
    }

    public class FieldDescriptor<TDeclaringType, TFieldType> : FieldDescriptor, IFieldDescriptor<TDeclaringType>
        where TDeclaringType : class
    {
        public bool TryDeserialize(Span<byte> bytes, TDeclaringType declaringObject)
        {
            if (Deserialize == false) return false;
            if (declaringObject == null) return false;

            //TODO pass context through ObjectDeserializer to get into deserializermanager
            TFieldType v = default;

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
