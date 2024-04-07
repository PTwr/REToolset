namespace ReflectionHelper
{
    public class FuncField<TDeclaringType, TValue>
    {
        private readonly TValue? value;
        private readonly Func<TDeclaringType, TValue>? func;

        public FuncField(TValue? value)
        {
            this.value = value;
            this.func = null;
        }

        public FuncField(Func<TDeclaringType, TValue> func)
        {
            this.value = default;
            this.func = func;
        }

        public TValue? Get(TDeclaringType declaringType)
        {
            if (func is not null) return func.Invoke(declaringType);
            return value;
        }
    }
    public class FuncField<TDeclaringType, TItem, TValue>
    {
        private readonly TValue? value;
        private readonly Func<TDeclaringType, TItem, TValue>? func;

        public FuncField(TValue? value)
        {
            this.value = value;
            this.func = null;
        }

        public FuncField(Func<TDeclaringType, TItem, TValue> func)
        {
            this.value = default;
            this.func = func;
        }

        public TValue? Get(TDeclaringType declaringType, TItem item)
        {
            if (func is not null) return func.Invoke(declaringType, item);
            return value;
        }
    }

    //TODO I hate this, switch to single-param func and pass wrapper with all fields set?
    //TODO but passing wrapper won't work with annoying Span's :/
    public class FuncField<TDeclaringType, TItem1, TItem2, TValue>
    {
        private readonly TValue? value;
        private readonly Func<TDeclaringType, TItem1, TItem2, TValue>? func;

        public FuncField(TValue? value)
        {
            this.value = value;
            this.func = null;
        }

        public FuncField(Func<TDeclaringType, TItem1, TItem2, TValue> func)
        {
            this.value = default;
            this.func = func;
        }

        public TValue? Get(TDeclaringType declaringType, TItem1 item1, TItem2 item2)
        {
            if (func is not null) return func.Invoke(declaringType, item1, item2);
            return value;
        }
    }
}
