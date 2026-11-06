using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class Crc32Tests
{
    [Fact]
    public void Compute_KnownVector_ReturnsExpectedChecksum()
    {
        Assert.Equal(0xCBF43926u, Crc32.Compute("123456789"u8));
    }

    [Fact]
    public void UpdateInChunks_MatchesSinglePass()
    {
        ReadOnlySpan<byte> bytes = "The quick brown fox jumps over the lazy dog"u8;
        var expected = Crc32.Compute(bytes);
        var crc = Crc32.InitialValue;
        crc = Crc32.Update(crc, bytes[..10]);
        crc = Crc32.Update(crc, bytes.Slice(10, 15));
        crc = Crc32.Update(crc, bytes[25..]);

        Assert.Equal(expected, Crc32.Finish(crc));
    }
}
