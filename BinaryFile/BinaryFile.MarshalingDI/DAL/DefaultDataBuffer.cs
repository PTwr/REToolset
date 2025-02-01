namespace BinaryFile.MarshalingDI.DAL
{
    public class DefaultDataBuffer : IDataBuffer
    {
        private byte[] data;
        private readonly bool allowResize;
        private int actualLength;

        public DefaultDataBuffer(byte[] initialData, bool allowResize)
        {
            data = initialData;
            this.allowResize = allowResize;
            actualLength = data.Length;
        }

        public byte this[int index]
        {
            //reuse length managament/safety/exception in AsSpan
            get
            {
                return this.AsSpan(index, 1)[0];
            }
            set
            {
                this.AsSpan(index, 1)[0] = value;
            }
        }

        public override string ToString()
        {
            return $"Data: {data.Length} | AllowResize: {allowResize}";
        }

        public Span<byte> AsSpan()
        {
            return AsSpan(0);
        }
        public Span<byte> AsSpan(int from)
        {
            return AsSpan(from, actualLength - from);
        }
        public Span<byte> AsSpan(int from, int length)
        {
            if (from < 0) throw new ArgumentOutOfRangeException($"Requested slice starts at negative offset of {from}. This indicates error in offset math");
            if (length < 0) throw new ArgumentOutOfRangeException($"Requested slice starts has negative length of {length}. This indicates error in offset math");

            int requiredLength = from + length;
            EnsureLength(requiredLength);

            if (requiredLength > actualLength)
                throw new ArgumentOutOfRangeException($"Requesting data beyond buffer of length {actualLength}. From: {from} | Length: {length}. Overshot by {actualLength - requiredLength}");

            return data.AsSpan().Slice(from, length);
        }

        public int Length => actualLength;

        private void EnsureLength(int requiredLength)
        {
            if (!allowResize) return;

            //gotta keep track of actual size due to prealocation
            if (requiredLength > actualLength)
            {
                actualLength = requiredLength;
            }

            if (requiredLength > data.Length)
            {
                Array.Resize(ref data, requiredLength);
            }
        }

        public byte ElementAt(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException($"Requested negative index of {index}. This indicates error in offset math");

            EnsureLength(index + 1);

            if (index > actualLength) throw new ArgumentOutOfRangeException($"Requested index of {index} beyond available data. This indicates error in offset math");

            return data[index];
        }
    }
}
