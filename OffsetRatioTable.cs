namespace RotationallyMultiplicativeIntegers;

/// <summary>A single cell: B/A in lowest terms, such that A * rho_offset(x) = B * x.</summary>
readonly record struct OffsetRatio(long Numerator, long Denominator)
{
    public override string ToString() => Denominator == 1 ? $"{Numerator}" : $"{Numerator}/{Denominator}";
}

/// <summary>
/// The offset-ratio table for one repetend cycle, matching Tables 1-4 in
/// rotationally-multiplicative-integers.tex. Entry[offset - 1, pos] holds the ratio for that
/// offset read from the column at position pos.
/// </summary>
sealed class OffsetRatioTable
{
    public required RepetendCycle Cycle { get; init; }
    public required OffsetRatio[,] Entries { get; init; } // [offsetIndex, pos], offsetIndex 0 => offset 1
    public int MaxOffset => Cycle.Length - 1;

    public static OffsetRatioTable Build(RepetendCycle cycle)
    {
        var length = cycle.Length;
        var maxOffset = length - 1;
        var entries = new OffsetRatio[Math.Max(maxOffset, 0), length];

        for (var t = 1; t <= maxOffset; t++)
        {
            for (var pos = 0; pos < length; pos++)
            {
                long num = cycle.Remainders[(pos + t) % length];
                long den = cycle.Remainders[pos];
                var g = Gcd(num, den);
                entries[t - 1, pos] = new OffsetRatio(num / g, den / g);
            }
        }

        return new OffsetRatioTable { Cycle = cycle, Entries = entries };
    }

    private static long Gcd(long a, long b) => b == 0 ? a : Gcd(b, a % b);
}
