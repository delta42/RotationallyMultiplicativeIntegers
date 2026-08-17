namespace RotationallyMultiplicativeIntegers;

/// <summary>
/// Expands every cell of an OffsetRatioTable back into the concrete equation it represents:
/// A * rho_t(x_pos) = B * x_pos, per eq. (4) generalized to arbitrary offset t.
/// </summary>
static class EquationRenderer
{
    public static void Render(OffsetRatioTable table, int? cycleNumber = null, Action<int>? onOffsetDone = null)
    {
        var cycle = table.Cycle;

        if (table.MaxOffset == 0)
        {
            Console.WriteLine("(cycle length 1, no rotations to pair up)");
            return;
        }

        var factorWidth = 1;
        foreach (var entry in table.Entries)
        {
            factorWidth = Math.Max(factorWidth, entry.Numerator.ToString().Length);
            factorWidth = Math.Max(factorWidth, entry.Denominator.ToString().Length);
        }

        string FormatFactor(long value) =>
            value == 1 ? new string(' ', factorWidth + 3) : $"{value.ToString().PadLeft(factorWidth)} * ";

        // Each rotation string costs O(length) to build; with length*(length-1) equations to print,
        // building it fresh per equation would cost O(length^3) overall. Building each one once here
        // and reusing it brings the whole render down to O(length^2).
        var rotations = new string[cycle.Length];
        for (var pos = 0; pos < cycle.Length; pos++)
            rotations[pos] = cycle.FormatRotation(pos);

        var prefix = cycleNumber is { } n ? $"Cycle {n}, " : "";

        for (var t = 1; t <= table.MaxOffset; t++)
        {
            Console.WriteLine($"{prefix}Offset {t}:");
            for (var pos = 0; pos < cycle.Length; pos++)
            {
                var entry = table.Entries[t - 1, pos];
                var a = entry.Denominator;
                var b = entry.Numerator;
                var rotated = rotations[(pos + t) % cycle.Length];
                var baseValue = rotations[pos];
                Console.WriteLine($"  {FormatFactor(a)}{rotated} = {FormatFactor(b)}{baseValue}");
            }

            onOffsetDone?.Invoke(cycle.Length);
        }
    }
}
