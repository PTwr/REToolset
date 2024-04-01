namespace BinaryFile.Unpacker.Deserializers
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
}
