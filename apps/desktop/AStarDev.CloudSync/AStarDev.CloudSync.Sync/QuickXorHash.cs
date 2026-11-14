namespace AStarDev.CloudSync.Sync;

internal static class QuickXorHash
{
    private const int Width = 160;
    private const int Shift = 11;
    private const int BufferSize = 81920;
    private const int WideBits = 128;
    private const int LengthOffset = 12;

    internal static async Task<string> ComputeAsync(Stream stream, Action<ReadOnlyMemory<byte>> observe, CancellationToken cancellationToken)
    {
        UInt128 low = 0;
        uint high = 0;
        long length = 0;
        var position = 0;
        var buffer = new byte[BufferSize];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            observe(buffer.AsMemory(0, read));
            for (var index = 0; index < read; index++)
            {
                XorAt(buffer[index], position, ref low, ref high);
                position = (position + Shift) % Width;
            }

            length += read;
        }

        return Finish(low, high, length);
    }

    private static void XorAt(byte value, int position, ref UInt128 low, ref uint high)
    {
        if (position < WideBits)
        {
            low ^= (UInt128)value << position;
            if (position > WideBits - 8)
                high ^= (uint)(value >> (WideBits - position));

            return;
        }

        high ^= (uint)value << (position - WideBits);
        if (position > Width - 8)
            low ^= (UInt128)(value >> (Width - position));
    }

    private static string Finish(UInt128 low, uint high, long length)
    {
        var bytes = new byte[Width / 8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt128LittleEndian(bytes, low);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), high);
        var lengthBytes = BitConverter.GetBytes(length);
        for (var index = 0; index < lengthBytes.Length; index++)
            bytes[LengthOffset + index] ^= lengthBytes[index];

        return Convert.ToBase64String(bytes);
    }
}
