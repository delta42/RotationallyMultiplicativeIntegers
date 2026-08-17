namespace RotationallyMultiplicativeIntegers;

static class ConsoleTableRenderer
{
    public static void Render(OffsetRatioTable table, int cycleNumber)
    {
        var cycle = table.Cycle;

        var headers = new[] { "Repetend Digit", "Intermediate Remainder" }
            .Concat(Enumerable.Range(1, table.MaxOffset).Select(t => $"Offset {t}"))
            .ToArray();

        var rowLabelWidth = headers.Max(h => h.Length);
        var colWidths = new int[cycle.Length];
        for (var pos = 0; pos < cycle.Length; pos++)
        {
            var width = Math.Max(cycle.Digits[pos].ToString().Length, cycle.Remainders[pos].ToString().Length);
            for (var t = 1; t <= table.MaxOffset; t++)
                width = Math.Max(width, table.Entries[t - 1, pos].ToString().Length);
            colWidths[pos] = width;
        }

        var tableWidth = rowLabelWidth + 2 + colWidths.Sum(w => w + 2);
        var title = $"Cycle {cycleNumber} (repetend {cycle.FormatRotation(0)}, length {cycle.Length})";
        Console.WriteLine(CenterInBanner(title, tableWidth));

        void WriteRow(string label, Func<int, string> cell)
        {
            Console.Write(label.PadRight(rowLabelWidth + 2));
            for (var pos = 0; pos < cycle.Length; pos++)
                Console.Write(cell(pos).PadLeft(colWidths[pos] + 2));
            Console.WriteLine();
        }

        WriteRow("Repetend Digit", pos => cycle.Digits[pos].ToString());
        WriteRow("Intermediate Remainder", pos => cycle.Remainders[pos].ToString());
        Console.WriteLine(new string('-', tableWidth));

        if (table.MaxOffset == 0)
        {
            Console.WriteLine("(cycle length 1, no nontrivial offsets)");
        }
        else
        {
            for (var t = 1; t <= table.MaxOffset; t++)
            {
                var offset = t;
                WriteRow($"Offset {t}", pos => table.Entries[offset - 1, pos].ToString());
            }
        }

        Console.WriteLine(new string('-', tableWidth));
    }

    private static string CenterInBanner(string title, int totalWidth)
    {
        var padding = totalWidth - title.Length - 2;
        if (padding <= 0)
            return title;

        var left = padding / 2;
        var right = padding - left;
        return $"{new string('=', left)} {title} {new string('=', right)}";
    }
}
