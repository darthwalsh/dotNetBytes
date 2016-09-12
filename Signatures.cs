public static class CompressionExtensions
{
    public static uint DecompressUnsigned(this byte[] bytes, int index)
    {
        int sum;

        byte first = bytes[index];
        if ((first & 0x80) == 0x80)
        {
            if ((first & 0x40) == 0x40)
                sum = unchecked(((first & ~0xC0) << 24) + (bytes[index + 1] << 16) + (bytes[index + 2] << 8) + bytes[index + 3]);
            else
                sum = unchecked(((first & ~0xC0) << 8) + bytes[index + 1]);
        }
        else
            sum = first;

        return unchecked((uint)sum);
    }
}
