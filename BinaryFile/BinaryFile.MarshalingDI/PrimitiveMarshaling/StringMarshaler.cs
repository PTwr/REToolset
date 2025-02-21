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
    public class StringMarshaler : BaseMarshaler, IFullMarshaler<string>
    {
        public StringMarshaler(IDataBuffer dataBuffer, IOffsetStack offsetStack, IHierarchicalFeatureSet features) : base(dataBuffer, offsetStack, features)
        {
        }

        public string Read(out int bytesRead)
        {
            var encoding = features.GetTextEncoding();

            var bytes = dataBuffer.AsSpan().Slice(offsetStack.CurrentAbsoluteOffset);

            var stringLengthStyle = features.GetStringLengthStyle();
            switch (stringLengthStyle)
            {
                case EStringLengthStyle.NullTerminator:
                    bytes = bytes.FindNullTerminator(out var noTerminator, encoding: encoding);
                    bytesRead = noTerminator ? bytes.Length : bytes.Length + 1;
                    break;
                case EStringLengthStyle.PascalString:
                    //first byte is length, but not included
                    bytesRead = bytes[0] + 1;
                    if (bytesRead < bytes.Length - 1)
                        throw new Exception($"Pascal string defined length of {bytes[0]} which reaches beyond end of data stream. {features.GetDebugInfo()}");
                    bytes = bytes.Slice(1, bytes[0]);
                    break;
                case EStringLengthStyle.FixedLength:
                    if (!features.HasStringLength(out bytesRead))
                        throw new Exception($"Fixed length string requires {nameof(EMetadataNames.StringLength)} defined. {features.GetDebugInfo()}");
                    if (bytesRead < bytes.Length)
                        throw new Exception($"Fixed length string defined length of {bytesRead} which reaches beyond end of data stream. {features.GetDebugInfo()}");
                    bytes = bytes.Slice(0, bytesRead);
                    break;
                default:
                    throw new Exception($"Unknown value of {nameof(EStringLengthStyle)} of {stringLengthStyle}. {features.GetDebugInfo()}");
            }

            var str = bytes.ToDecodedString(encoding);
            return str;
        }

        public void Write(string value, out int bytesWrote)
        {
            byte[] bytes;

            var encoding = features.GetTextEncoding();
                        
            var stringLengthStyle = features.GetStringLengthStyle();
            switch (stringLengthStyle)
            {
                case EStringLengthStyle.NullTerminator:
                    bytes = value.ToBytes(encoding, true);
                    break;
                case EStringLengthStyle.PascalString:
                    bytes = value.ToBytes(encoding, true);
                    if (bytes.Length > 255)
                        throw new Exception($"Pascal string '{value}' with encoded length greater than 255. {features.GetDebugInfo()}");
                    bytes = [(byte)bytes.Length, .. bytes];
                    break;
                case EStringLengthStyle.FixedLength:
                    bytes = value.ToBytes(encoding, false);
                    break;
                default:
                    throw new Exception($"Unknown value of {nameof(EStringLengthStyle)} of {stringLengthStyle}. {features.GetDebugInfo()}");
            }

            bytesWrote = bytes.Length;

            dataBuffer.Emplace(offsetStack.CurrentAbsoluteOffset, bytes);
        }
    }
}
