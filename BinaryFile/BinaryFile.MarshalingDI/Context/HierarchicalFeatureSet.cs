using Autofac;
using Autofac.Core;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using LanguageExt.ClassInstances.Pred;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BinaryFile.MarshalingDI.Context.HierarchicalFeatureSet;
using static BinaryFile.MarshalingDI.Context.IHierarchicalFeatureSet;

namespace BinaryFile.MarshalingDI.Context
{
    public class HierarchicalFeatureSet : IHierarchicalFeatureSet
    {
        //current item is first item, use Insert instead of Add
        //root is for global features added during setup, object/type/field marshalers will be adding more generations
        //same with nested feature list, both are FILO rather than FIFO
        List<(string generationName, List<IFeatureWrapper> features)> featureSets = [("root", new List<IFeatureWrapper>())];
        private readonly ILifetimeScope container;

        public HierarchicalFeatureSet(ILifetimeScope container)
        {
            this.container = container;
        }

        public class FuncFeatureWrapper<T> : IFeatureWrapper<T>
        {
            public override string ToString()
            {
                return $"{Name}";
            }

            protected readonly int maxGeneration;
            protected readonly bool cached;
            protected readonly Func<ILifetimeScope, T> func;
            public string Name { get; }

            public FuncFeatureWrapper(Func<ILifetimeScope, T> func, int maxGeneration, string name = "", bool cached = false)
            {
                this.func = func;
                this.maxGeneration = maxGeneration;
                this.Name = name;
                this.cached = cached;
            }
            public FuncFeatureWrapper(FuncFeatureWrapper<T> wrapper)
            {
                this.func = wrapper.func;
                this.maxGeneration = wrapper.maxGeneration;
                this.Name = wrapper.Name;
                this.cached = wrapper.cached;
            }

            public virtual T Value => throw new InvalidOperationException($"{nameof(FuncFeatureWrapper<T>)}.{nameof(Value)} can't be called directly. Construct {nameof(BoundFuncFeatureWrapper<T>)} to get {nameof(Value)}.");

            public bool IsWithinGenerationLimit(int generationLimit)
                => generationLimit <= maxGeneration;

            public IFeatureWrapper BoundCopy(ILifetimeScope container)
                => new BoundFuncFeatureWrapper<T>(container, this);
        }
        public class BoundFuncFeatureWrapper<T> : FuncFeatureWrapper<T>
        {
            private readonly ILifetimeScope container;

            public BoundFuncFeatureWrapper(ILifetimeScope container, Func<ILifetimeScope, T> func, int maxGeneration, string name = "")
                : base(func, maxGeneration, name)
            {
                this.container = container;
            }
            public BoundFuncFeatureWrapper(ILifetimeScope container, FuncFeatureWrapper<T> featureWrapper)
                : base(featureWrapper)
            {
                this.container = container;
            }
            T cache;
            bool calculated = false;
            public override T Value
            {
                //TODO link between feature and its FeatureSet? cached result aint a solution
                //featureWrapper would need to know its Generation
                //FeatureSet should be its own class instead of basic list, with link to previous gen for recursive lookup?
                get
                {
                    if (cached)
                    {
                        if (!calculated)
                        {
                            cache = func(container);
                            calculated = true;
                        }
                        return cache;
                    }
                    return func(container);
                }
            }
        }
        public class ValueFeatureWrapper<T> : IFeatureWrapper<T>
        {
            public override string ToString()
            {
                return $"{Name} {value.ToString()}";
            }

            T value;
            private readonly int maxGeneration;

            public string Name { get; private set; }

            public ValueFeatureWrapper(T obj, int maxGeneration, string name = "")
            {
                this.value = obj;
                this.maxGeneration = maxGeneration;
                this.Name = name;
            }
            public T Value => this.value;

            public bool IsWithinGenerationLimit(int generationLimit)
                => generationLimit <= maxGeneration;

            public IFeatureWrapper BoundCopy(ILifetimeScope container)
                => this;
        }

        public IEnumerable<IFeatureWrapper<TFeature>> GetAll<TFeature>(string name = "")
        {
            for (int generation = 0; generation < featureSets.Count; generation++)
            {
                var features = featureSets[generation]
                    .features
                    .Where(x => x.IsWithinGenerationLimit(generation))
                    .OfType<IFeatureWrapper<TFeature>>()
                    .Where(x => x.Name == name);

                foreach (var feature in features)
                {
                    yield return feature;
                }
            }
        }
        [return: NotNullIfNotNull(nameof(fallbackValue))]
        public TFeature Get<TFeature>(TFeature fallbackValue, string name = "")
        {
            var wrapper = GetAll<TFeature>(name).FirstOrDefault();
            return wrapper == null ? fallbackValue : wrapper.Value;
        }
        public TFeature GetRequired<TFeature>(string name = "")
        {
            var wrapper = GetAll<TFeature>(name).FirstOrDefault();
            return wrapper != null ? wrapper.Value : throw new NullReferenceException($"Required parameter '{typeof(TFeature).FullName}' with name of '{name}' is missing!");
        }

        public bool TryGet<TFeature>([NotNullWhen(returnValue: true)] out TFeature? result, string name = "")
        {
            var wrapper = GetAll<TFeature>(name).FirstOrDefault();

            result = wrapper != null ? wrapper.Value : default;

            return wrapper != null;
        }

        public void Push(string name)
        {
            featureSets.Insert(0, (name, new List<IFeatureWrapper>()));
        }
        public void Pop()
        {
            featureSets.RemoveAt(0);
        }

        public void AddValueFeature<TFeature>(TFeature feature, int maxGenerations = 0, string name = "")
        {
            featureSets[0].features.Add(new ValueFeatureWrapper<TFeature>(feature, maxGenerations, name));
        }
        public void AddFuncFeature<TFeature>(Func<ILifetimeScope, TFeature> func, int maxGenerations = 0, string name = "")
        {
            featureSets[0].features.Add(new BoundFuncFeatureWrapper<TFeature>(container, func, maxGenerations, name));
        }
        public void AddFeature(IFeatureWrapper feature)
        {
            featureSets[0].features.Add(feature);
        }

        public void AddFeatureRange(IEnumerable<IFeatureWrapper> features)
        {
            featureSets[0].features.InsertRange(0, features);
        }
    }
}
