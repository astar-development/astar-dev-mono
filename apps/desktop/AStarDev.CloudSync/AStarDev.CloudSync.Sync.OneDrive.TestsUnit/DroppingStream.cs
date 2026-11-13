namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class DroppingStream(byte[] content, int dropAfter) : Stream
{
    private int position;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (position >= dropAfter)
            throw new IOException("The connection dropped.");

        var take = Math.Min(Math.Min(count, dropAfter - position), content.Length - position);
        Array.Copy(content, position, buffer, offset, take);
        position += take;

        return take;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
