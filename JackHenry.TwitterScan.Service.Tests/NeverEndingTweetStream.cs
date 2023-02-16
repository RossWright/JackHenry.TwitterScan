namespace JackHenry.TwitterScan.Service.Tests;

public class NeverEndingTweetStream : Stream
{
    public void SuddenlyFail() => fail = true;
    bool fail = false;

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (fail)
        {
            fail = false;
            leftovers = Encoding.UTF8.GetBytes(new string("[")).ToList();
            throw new ApplicationException("Test Failure");
        }
        if (leftovers.Count < count)
        {
            StringBuilder json = new StringBuilder();
            while (leftovers.Count + json.Length < count)
            {
                tweetDataWrapper.Data.Entities.Hashtags[0].Tag = Guid.NewGuid().ToString();
                json.Append(JsonSerializer.Serialize(tweetDataWrapper) + ",");
            }
            leftovers.AddRange(Encoding.UTF8.GetBytes(json.ToString()));
        }
        leftovers.CopyTo(0, buffer, offset, count - offset);
        leftovers.RemoveRange(0, count - offset);
        return count - offset;
    }
    TweetDataWrapper tweetDataWrapper = new TweetDataWrapper(Guid.NewGuid().ToString());
    List<byte> leftovers = Encoding.UTF8.GetBytes(new string("[")).ToList();

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;

    public override void Flush() => throw new NotImplementedException();
    public override long Length => throw new NotSupportedException("Cannot get the length of this Stream.");
    public override long Position
    {
        get => throw new NotSupportedException("Cannot get the position of this Stream.");
        set => throw new NotSupportedException("Cannot set the position of this Stream.");
    }
    public override long Seek(long offset, SeekOrigin origin) => 
        throw new NotSupportedException("Cannot seek on this Stream.");
    public override void SetLength(long value) =>
        throw new NotSupportedException("Cannot set length of this Stream.");
    public override void Write(byte[] buffer, int offset, int count) => 
        throw new NotSupportedException("Cannot write to this Stream.");
}