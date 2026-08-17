using System.Numerics;

namespace RotationallyMultiplicativeIntegers;

/// <summary>
/// One cycle of remainders (coprime to the divisor) under repeated multiplication by 10 mod d,
/// together with the repetend digits it produces. See rotationally-multiplicative-integers.tex,
/// eq. (3): r_i * 10 = c_i * d + r_{i-1}.
/// </summary>
sealed class RepetendCycle
{
    public required int Divisor { get; init; }
    public required int[] Remainders { get; init; } // Remainders[pos] = r_i, pos 0..L-1, decreasing i
    public required int[] Digits { get; init; }      // Digits[pos] = c_i
    public int Length => Remainders.Length;

    public BigInteger RepetendValue => RotationValue(0);

    /// <summary>The repetend rotated so that Digits[pos] is the leading digit, i.e. rho_pos(RepetendValue).</summary>
    public BigInteger RotationValue(int pos)
    {
        BigInteger x = 0;
        for (var j = 0; j < Length; j++)
            x = x * 10 + Digits[(pos + j) % Length];
        return x;
    }

    /// <summary>
    /// Same value as RotationValue(pos), formatted with thousands separators but keeping any
    /// leading zero digit, BigInteger.ToString() would otherwise silently drop it (see Remark
    /// 4.2: 1/13 = 0.076923... is a genuine 6-digit repeating block, not the 5-digit number 76923).
    /// </summary>
    public string FormatRotation(int pos)
    {
        var digits = new char[Length];
        for (var j = 0; j < Length; j++)
            digits[j] = (char)('0' + Digits[(pos + j) % Length]);

        var result = new System.Text.StringBuilder();
        for (var j = 0; j < Length; j++)
        {
            if (j > 0 && (Length - j) % 3 == 0)
                result.Append(',');
            result.Append(digits[j]);
        }
        return result.ToString();
    }
}

static class RepetendCycleFinder
{
    /// <summary>
    /// Partitions the remainders coprime to d into their cycles under r -> r*10 mod d.
    /// Requires gcd(d, 10) == 1, so multiplication by 10 permutes the coprime residues.
    /// </summary>
    public static List<RepetendCycle> FindAll(int divisor)
    {
        if (divisor < 2)
            throw new ArgumentOutOfRangeException(nameof(divisor), "Divisor must be at least 2.");
        if (Gcd(divisor, 10) != 1)
            throw new ArgumentException(
                $"Divisor {divisor} shares a factor with 10; its decimal expansion has a non-repeating prefix, " +
                "so it falls outside the repetend theory this table is built from.");

        var visited = new bool[divisor];
        var cycles = new List<RepetendCycle>();

        for (var start = 1; start < divisor; start++)
        {
            if (visited[start] || Gcd(start, divisor) != 1)
                continue;

            var remainders = new List<int>();
            var r = start;
            do
            {
                visited[r] = true;
                remainders.Add(r);
                r = (int)((long)r * 10 % divisor);
            } while (r != start);

            var digits = new int[remainders.Count];
            for (var i = 0; i < remainders.Count; i++)
                digits[i] = (int)((long)remainders[i] * 10 / divisor);

            cycles.Add(new RepetendCycle
            {
                Divisor = divisor,
                Remainders = remainders.ToArray(),
                Digits = digits,
            });
        }

        return cycles;
    }

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
}
