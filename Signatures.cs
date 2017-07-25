using System;
using System.IO;
using static ElementType;

// II.23.2 Blobs and signatures

public static class CompressionExtensions
{
    // Number is compressed into one of these byte formats, where x is a bit of the number
    //  0xxxxxxx
    //  10xxxxxx xxxxxxxx
    //  110xxxxx xxxxxxxx xxxxxxxx xxxxxxxx
    public static uint DecompressUnsigned(this byte[] bytes, int index)
    {
        byte first = bytes[index];
        switch (GetWidth(first))
        {
            case 1:
                return first;
            case 2:
                return (uint)(((first & ~0xC0) << 8) + bytes[index + 1]);
            case 4:
                return (uint)(((first & ~0xC0) << 24) + (bytes[index + 1] << 16) + (bytes[index + 2] << 8) + bytes[index + 3]);
            default:
                throw new ArgumentException(first.ToString("x"));
        }
    }

    // Number is compressed into one of these byte formats, where x is a bit of the number and b is the 2's complement sign bit
    //  0xxxxxxb
    //  10xxxxxx xxxxxxxb
    //  110xxxxx xxxxxxxx xxxxxxxx xxxxxxxb
    public static int DecompressSigned(this byte[] bytes, int index)
    {
        int sum = (int)bytes.DecompressUnsigned(index);

        if (sum % 2 == 0)
        {
            return sum >> 1;
        }

        int negativeMask;
        byte first = bytes[index];
        switch (GetWidth(first))
        {
            case 1:
                negativeMask = unchecked((int)0xFFFFFFC0);
                break;
            case 2:
                negativeMask = unchecked((int)0xFFFFE000);
                break;
            case 4:
                negativeMask = unchecked((int)0xF0000000);
                break;
            default:
                throw new ArgumentException(first.ToString("x"));
        }

        return negativeMask | (sum >> 1);
    }

    static int GetWidth(byte b)
    {
        switch (b & 0xE0)
        {
            case 0x00:
            case 0x20:
            case 0x40:
            case 0x60:
                return 1;
            case 0x80:
            case 0xA0:
                return 2;
            case 0xC0:
                return 4;
            case 0xE0:
                throw new InvalidOperationException("Not expecting null string!");
            default:
                throw new InvalidOperationException();
        }
    }
}

//II.23.2.14
sealed class TypeSpecSignature : ICanRead
{
    object Value => ""; //TODO needed?

    public CodeNode Node { get; set; }

    public CodeNode Read(Stream stream)
    {
        ElementType b = (ElementType)stream.ReallyReadByte();
        switch(b)
        {
            case Ptr:
                throw new NotImplementedException("Ptr");
            case Fnptr:
                throw new NotImplementedException("Fnptr");
            case ElementType.Array:
                throw new NotImplementedException("Array");
            case Szarray:
                throw new NotImplementedException("Szarray");
            case Genericinst:
                throw new NotImplementedException("Genericinst");
            default:
                throw new InvalidOperationException(b.ToString());
        }
    }
}
