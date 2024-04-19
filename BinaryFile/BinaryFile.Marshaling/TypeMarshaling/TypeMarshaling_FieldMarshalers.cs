using BinaryFile.Marshaling.FieldMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.TypeMarshaling
{
    public partial class TypeMarshaler<TRoot, TBase, TImplementation> : ITypeMarshaler<TRoot, TBase, TImplementation>
        where TBase : class, TRoot
        where TImplementation : class, TBase, TRoot
    {
        List<IOrderedFieldMarshaler<TImplementation>> MarshalingActions = new List<IOrderedFieldMarshaler<TImplementation>>();

        public ITypeMarshaler<TRoot, TBase, TImplementation> WithMarshalingAction(IOrderedFieldMarshaler<TImplementation> action)
        {
            MarshalingActions.Add(action);
            return this;
        }

        public IEnumerable<IOrderedFieldMarshaler<TImplementation>> DerivedMarshalingActions
        {
            get
            {
                if (Parent is not null)
                {
                    var parentStuff =
                        Parent.DerivedMarshalingActions
                        //filter out overriden fields
                        .Where(i => !MarshalingActions.Any(j => j.Name == i.Name));
                    var combined = MarshalingActions.Concat(parentStuff);
                    return combined;
                }
                return MarshalingActions;
            }
        }

        //public IOrderedUnaryFieldMarshaler<TImplementation, TFieldType, TFieldType>
        //    WithField<TFieldType>(Expression<Func<TImplementation, TFieldType>> getter, bool deserialize = true, bool serialize = true)
        //    => WithField(getter.GetMemberName(), getter, deserialize, serialize);
        //public IOrderedUnaryFieldMarshaler<TImplementation, TFieldType, TFieldType>
        //    WithField<TFieldType>(string name, Expression<Func<TImplementation, TFieldType>> getter, bool deserialize = true, bool serialize = true)
        //{
        //    var action = new OrderedUnaryFieldMarshaler<TImplementation, TFieldType, TFieldType>(name);

        //    if (deserialize)
        //    {
        //        var setter = getter.GenerateToSetter().Compile();
        //        action
        //            .MarshalInto((obj, x, y) => y)
        //            .Into(setter);
        //    }
        //    if (serialize)
        //    {
        //        action
        //            .MarshalFrom((obj, x) => x)
        //            .From(getter.Compile());
        //    }

        //    MarshalingActions.Add(action);

        //    return action;
        //}
        //public IOrderedCollectionFieldMarshaler<TImplementation, TFieldType, TFieldType>
        //    WithCollectionOf<TFieldType>(Expression<Func<TImplementation, IEnumerable<TFieldType>>> getter, bool deserialize = true, bool serialize = true)
        //    => WithCollectionOf(getter.GetMemberName(), getter, deserialize, serialize);
        //public IOrderedCollectionFieldMarshaler<TImplementation, TFieldType, TFieldType>
        //    WithCollectionOf<TFieldType>(string name, Expression<Func<TImplementation, IEnumerable<TFieldType>>> getter, bool deserialize = true, bool serialize = true)
        //{
        //    var action = new OrderedCollectionFieldMarshaler<TImplementation, TFieldType, TFieldType>(name);

        //    action
        //        .MarshalInto((obj, x, y) => y)
        //        .MarshalFrom((obj, x) => x);

        //    if (deserialize)
        //    {
        //        var setter = getter.GenerateToSetter(tryToUseCtor: true).Compile();
        //        action
        //            .Into(setter);
        //    }
        //    if (serialize)
        //    {
        //        action
        //            .From(getter.Compile());
        //    }

        //    MarshalingActions.Add(action);

        //    return action;
        //}
    }
}
