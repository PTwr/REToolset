namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    public interface IEVEOpCode
    { 
    }

    //TODO implicit converters to compare with 32bit hex mask?
    //Record provides struct-like default equality semantics
    public record class EVEOpCode : IEVEOpCode
    {
        //TODO rethink Terminator, just uint32 should be enough and won't fuck up hierarchy.
        //TODO same with Line header! Hierarchy doesnt work so well if class is used on multiple levels
        public EVEOpCode() { }
        public EVEOpCode(EVESegment parent)
        {
            ParentSegment = parent;
        }
        public EVEOpCode(EVEBlock parent)
            : this(parent.ParentEVE)
        {
            ParentBlock = parent;
        }
        public EVEOpCode(EVELine parent)
            : this(parent.ParentBlock)
        {
            ParentLine = parent;
        }
        public EVEOpCode(IEVELineBody parent)
            : this(parent.ParentLine)
        {
            ParentLineBody = parent;
        }
        public EVEOpCode(EVELine parent, ushort instruction, ushort parameter)
            : this(parent)
        {
            HighWord = instruction;
            LowWord = parameter;
        }
        public EVEOpCode(EVEBlock parent, ushort instruction, ushort parameter)
            : this(parent)
        {
            HighWord = instruction;
            LowWord = parameter;
        }
        public EVEOpCode(ushort instruction, ushort parameter)
        {
            HighWord = instruction;
            LowWord = parameter;
        }
        public EVEOpCode(EVELine parent, uint code)
            : this(parent)
        {
            HighWord = (ushort)(code >> 16);
            LowWord = (ushort)(code);
        }
        public EVEOpCode(uint code)
        {
            HighWord = (ushort)(code >> 16);
            LowWord = (ushort)(code);
        }

        public EVEOpCode(IEnumerable<byte> bytes)
        {
            HighWord = (ushort)(bytes.ElementAt(0) << 8 | bytes.ElementAt(1));
            LowWord = (ushort)(bytes.ElementAt(2) << 8 | bytes.ElementAt(3));
        }
        public EVEOpCode(EVELine parent, IEnumerable<byte> bytes) : this(bytes)
        {
            ParentLine = parent;
        }

        public override string ToString()
        {
            return $"{HighWord:X4} {LowWord:X4}";
        }

        public ushort HighWord { get; set; }
        public ushort LowWord { get; set; }
        public IEVELineBody? ParentLineBody { get; }
        public EVELine? ParentLine { get; }
        public EVEBlock? ParentBlock { get; }
        public EVESegment? ParentSegment { get; }

        public static implicit operator uint(EVEOpCode code)
        {
            return (uint)(code.HighWord << 16 | code.LowWord);
        }

        public byte[] ToBytes()
        {
            byte[] bytes = [
                (byte)(HighWord>>8),
                (byte)(HighWord),
                (byte)(LowWord>>8),
                (byte)(LowWord),
                ];

            return bytes;
        }

        //TODO check if lower word matters, might be just null/one padding 
        public const uint StatementStart = 0x00030000;
        public const uint LineTerminator = 0x00040000;
        public const uint BlockTerminator = 0x0005FFFF;
        public const uint SegmentTerminator = 0x0006FFFF;
    }
}
