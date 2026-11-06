using System.Text;
using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class Crc32Tests
{
    [Fact]
    public void Compute_KnownVector_ReturnsExpectedChecksum()
    {
        var bytes = Encoding.ASCII.GetBytes("123456789");

        Assert.Equal(0xCBF43926u, Crc32.Compute(bytes));
    }

    [Fact]
    public void UpdateInChunks_MatchesSinglePass()
    {
        var bytes = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");
        var expected = Crc32.Compute(bytes);
        var crc = Crc32.InitialValue;
        crc = Crc32.Update(crc, bytes.AsSpan(0, 10));
        crc = Crc32.Update(crc, bytes.AsSpan(10, 15));
        crc = Crc32.Update(crc, bytes.AsSpan(25));

        Assert.Equal(expected, Crc32.Finish(crc));
    }
}
