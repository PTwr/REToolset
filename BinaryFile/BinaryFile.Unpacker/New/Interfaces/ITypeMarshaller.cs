﻿using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Interfaces
{
    public interface ITypeMarshaler
    {
    }

    public interface ITypeMarshaler<TImplementation>
        : ITypeMarshaler, 
        IActivator<TImplementation>, IDerriverableTypeMarshaler<TImplementation>,
        IDeserializator<TImplementation, TImplementation>, ISerializator<TImplementation>
        where TImplementation : class
    {
        IEnumerable<IOrderedFieldMarshaler<TImplementation>> DerivedDeserializingActions { get; }
        IEnumerable<IOrderedFieldMarshaler<TImplementation>> DerivedSerializingActions { get; }
    }

    public interface ITypeMarshaler<out TBase, TImplementation>
        : ITypeMarshaler<TImplementation>
        where TBase : class
        where TImplementation : class, TBase
    {
        ITypeMarshaler<TImplementation, TDerived> Derrive<TDerived>()
            where TDerived : class, TImplementation;

        ITypeMarshaler<TBase, TImplementation> WithActivatorCondition(ActivatorConditionDelegate predicate);
        ITypeMarshaler<TBase, TImplementation> WithCustomActivator(CustomActivatorDelegate predicate);

        ITypeMarshaler<TBase, TImplementation> WithMarshalingAction(IOrderedFieldMarshaler<TImplementation> action);
        IOrderedUnaryFieldMarshaler<TImplementation, TFieldType, TFieldType> WithField<TFieldType>(string name, Expression<Func<TImplementation, TFieldType>> getter, bool deserialize = true, bool serialize = true);
    }

    public interface IDerriverableTypeMarshaler
    {
        bool HoldsHierarchyFor<T>();
        IDerriverableTypeMarshaler<T>? GetMarshalerToDerriveFrom<T>()
            where T : class;
    }
    public interface IDerriverableTypeMarshaler<TImplementation> : IDerriverableTypeMarshaler
        where TImplementation : class
    {
        ITypeMarshaler<TImplementation, TDerived> Derrive<TDerived>()
            where TDerived : class, TImplementation;
    }

    public interface IActivator
    {
        bool HoldsHierarchyFor<T>();
        IActivator<T>? GetActivatorFor<T>(Span<byte> data, IMarshalingContext ctx);
    }
    public interface IActivator<out TImplementation> : IActivator
    {
        delegate bool ActivatorConditionDelegate(Span<byte> data, IMarshalingContext ctx);
        delegate TImplementation CustomActivatorDelegate(Span<byte> data, IMarshalingContext ctx, object? parent);
        TImplementation Activate(Span<byte> data, IMarshalingContext ctx, object? parent = null);
    }

    //TODO cleanup interface naming, non-generics are needed for collection storage
    public interface IDeserializator<in TMappedType>
    {
        IDeserializator<T>? GetDeserializerFor<T>();
    }
    public interface IDeserializator<in TMappedType, out TResultType> : IDeserializator<TMappedType>, IDeserializingMarshaler<TMappedType, TResultType>
        where TResultType : TMappedType
    {
        IEnumerable<IOrderedFieldMarshaler<TMappedType>> InheritedDeserializingActions { get; }
    }

    public interface ISerializator
    {
        ISerializingMarshaler<T>? GetSerializerFor<T>();
    }
    public interface ISerializator<in TMappedType> : ISerializator, ISerializingMarshaler<TMappedType>
    {
        IEnumerable<IOrderedFieldMarshaler<TMappedType>> InheritedSerializingActions { get; }
    }
}
