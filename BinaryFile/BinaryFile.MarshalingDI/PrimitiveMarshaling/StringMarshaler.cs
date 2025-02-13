using BinaryDataHelper;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.PrimitiveMarshaling
{
    public class StringMarshaler : IFullMarshaler<string>
    {
        public string Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            var encoding = metadata.GetTextEncoding();

            var bytes = data.AsSpan().Slice(offsetStack.CurrentAbsoluteOffset);

            var stringLengthStyle = metadata.GetStringLengthStyle();
            switch (stringLengthStyle)
            {
                case StringLengthStyle.NullTerminator:
                    bytes = bytes.FindNullTerminator(out var noTerminator, encoding: encoding);
                    bytesRead = noTerminator ? bytes.Length : bytes.Length + 1;
                    break;
                case StringLengthStyle.PascalString:
                    //first byte is length, but not included
                    bytesRead = bytes[0] + 1;
                    if (bytesRead < bytes.Length - 1)
                        throw new Exception($"Pascal string defined length of {bytes[0]} which reaches beyond end of data stream. {metadata.GetDebugInfo()}");
                    bytes = bytes.Slice(1, bytes[0]);
                    break;
                case StringLengthStyle.FixedLength:
                    if (!metadata.HasStringLength(out bytesRead))
                        throw new Exception($"Fixed length string requires {nameof(IStringLengthMetadata)} defined. {metadata.GetDebugInfo()}");
                    if (bytesRead < bytes.Length)
                        throw new Exception($"Fixed length string defined length of {bytesRead} which reaches beyond end of data stream. {metadata.GetDebugInfo()}");
                    bytes = bytes.Slice(0, bytesRead);
                    break;
                default:
                    throw new Exception($"Unknown value of {nameof(StringLengthStyle)} of {stringLengthStyle}. {metadata.GetDebugInfo()}");
            }

            var str = bytes.ToDecodedString(encoding);
            return str;
        }

        public void Write(string value, IDataBuffer data, out int bytesWrote, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            byte[] bytes;

            var encoding = metadata.GetTextEncoding();
                        
            var stringLengthStyle = metadata.GetStringLengthStyle();
            switch (stringLengthStyle)
            {
                case StringLengthStyle.NullTerminator:
                    bytes = value.ToBytes(encoding, true);
                    break;
                case StringLengthStyle.PascalString:
                    bytes = value.ToBytes(encoding, true);
                    if (bytes.Length > 255)
                        throw new Exception($"Pascal string '{value}' with encoded length greater than 255. {metadata.GetDebugInfo()}");
                    bytes = [(byte)bytes.Length, .. bytes];
                    break;
                case StringLengthStyle.FixedLength:
                    bytes = value.ToBytes(encoding, false);
                    break;
                default:
                    throw new Exception($"Unknown value of {nameof(StringLengthStyle)} of {stringLengthStyle}. {metadata.GetDebugInfo()}");
            }

            bytesWrote = bytes.Length;

            data.Emplace(offsetStack.CurrentAbsoluteOffset, bytes);
        }
    }
}
