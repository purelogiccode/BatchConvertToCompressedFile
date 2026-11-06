namespace BatchConvertToCompressedFile.Services;

/// <summary>Incremental CRC-32 (IEEE 802.3) implementation used for archive verification.</summary>
internal static class Crc32
{
    /// <summary>Seed value used before processing data.</summary>
    public const uint InitialValue = 0xFFFFFFFFu;

    private static readonly uint[] Table = CreateTable();

    /// <summary>Updates a running CRC with the supplied bytes.</summary>
    /// <param name="crc">Current CRC value.</param>
    /// <param name="data">Bytes to add.</param>
    /// <returns>The updated CRC value.</returns>
    public static uint Update(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    /// <summary>Finalizes a running CRC value.</summary>
    /// <param name="crc">Running CRC value.</param>
    /// <returns>The final CRC.</returns>
    public static uint Finish(uint crc) => ~crc;

    /// <summary>Computes the CRC-32 of a byte sequence.</summary>
    /// <param name="data">Input bytes.</param>
    /// <returns>The CRC-32 checksum.</returns>
    public static uint Compute(ReadOnlySpan<byte> data) => Finish(Update(InitialValue, data));

    private static uint[] CreateTable()
    {
        var table = new uint[256];
        for (uint index = 0; index < table.Length; index++)
        {
            var value = index;
            for (var bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
            }

            table[index] = value;
        }

        return table;
    }
}
