namespace RotationallyMultiplicativeIntegers;

class Program
{
    // Beyond this many equation lines, the console table/equations become too wide and too long
    // to usefully read (and writing that much text to a real console window is itself slow), so
    // we skip showing them and just generate the file.
    const long LargeOutputLineThreshold = 2000;

    // A cycle of length L needs an L-by-L OffsetRatioTable (16 bytes/entry) and produces roughly
    // L*(L-1) equation lines, each holding two ~L-digit numbers, so both memory and output size
    // grow like L^3. Past a certain L that's not just slow, it's impossible (gigabytes of RAM,
    // then terabytes of disk). Refuse before attempting it instead of crashing with an OOM.
    const double HardStopEstimatedOutputBytes = 1_000_000_000; // 1 GB

    // Unlike the equation table, the repetend digit string itself only costs O(L) per cycle, so it
    // stays cheap well past the point where the full table becomes impossible. Still cap it so a
    // divisor near int.MaxValue with one giant cycle can't build a multi-gigabyte string in memory.
    const long MaxRepetendOutputChars = 100_000_000; // 100 MB of digits

    static int Main(string[] args)
    {
        var consoleOut = Console.Out;
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var outputDir = Directory.Exists(desktop)
            ? desktop
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var outputLocationPhrase = outputDir == desktop ? "on your Desktop" : "in your home directory";
        var outputPath = Path.Combine(outputDir, $"RMI-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        using var fileWriter = new StreamWriter(outputPath);
        Console.SetOut(new TeeTextWriter(consoleOut, fileWriter));

        const string title = "Rotationally Multiplicative Integers";
        Console.WriteLine(title);
        Console.WriteLine(new string('-', title.Length));
        foreach (var line in WrapText(
            "This tool accompanies the paper of the same name, which studies integers whose digit " +
            "rotations are exact rational multiples of themselves, generalizing the 1949 Bronowski " +
            "puzzle of finding a number that becomes 1.5 times itself when its leading digit is moved " +
            "to the end. Given a divisor coprime to 10, it finds the repetend cycle(s) of its decimal " +
            "expansion, builds the offset-ratio table from the paper, and expands every entry into the " +
            "concrete equation A * rotation(x) = B * x.", 80))
        {
            Console.WriteLine(line);
        }
        Console.WriteLine();

        int divisor;
        if (args.Length > 0)
        {
            if (!TryParseDivisor(args[0], out divisor, out var error))
            {
                Console.Error.WriteLine(error);
                return 1;
            }
        }
        else
        {
            divisor = PromptForDivisor();
            Console.WriteLine();
        }

        List<RepetendCycle> cycles;
        try
        {
            cycles = RepetendCycleFinder.FindAll(divisor);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        Console.WriteLine($"Divisor {divisor}: {cycles.Count} cycle(s) among the coprime remainders.");
        Console.WriteLine();

        // Estimate the output size from cycle lengths alone, before allocating anything O(length^2)
        // or larger, an OffsetRatioTable.Build call for a large enough cycle would itself exhaust
        // memory before we ever got a chance to check.
        var estimatedOutputBytes = cycles.Sum(c => c.Length <= 1 ? 0 : 2.8 * Math.Pow(c.Length, 3));
        if (estimatedOutputBytes > HardStopEstimatedOutputBytes)
        {
            var largestCycleLength = cycles.Max(c => c.Length);
            Console.WriteLine($"Divisor {divisor} has a repetend cycle {largestCycleLength:N0} digits long.");
            Console.WriteLine(
                $"Generating the full Offset-Ratio Table and list of equations would take on the order of " +
                $"{FormatByteCount(estimatedOutputBytes)} of disk/memory space, hence this step will not be performed.");
            Console.WriteLine();

            var totalRepetendChars = cycles.Sum(c => (long)c.Length);
            if (totalRepetendChars > MaxRepetendOutputChars)
            {
                Console.WriteLine(
                    $"Even just the repetend digits themselves come to {totalRepetendChars:N0} characters, " +
                    "still too much to handle here. Try a smaller divisor.");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                if (Console.IsInputRedirected)
                    Console.ReadLine();
                else
                    Console.ReadKey(intercept: true);
                return 1;
            }

            Console.WriteLine("The repetend digits themselves are cheap to produce, though, so those are written out below.");
            Console.WriteLine();

            var repetends = cycles.Select(c => c.FormatRotation(0)).ToList();

            Console.SetOut(fileWriter);
            for (var i = 0; i < cycles.Count; i++)
            {
                Console.WriteLine($"Cycle {i + 1} (length {cycles[i].Length:N0}):");
                Console.WriteLine(repetends[i]);
                Console.WriteLine();
            }
            Console.SetOut(consoleOut);

            for (var i = 0; i < cycles.Count; i++)
                Console.WriteLine($"Cycle {i + 1} (length {cycles[i].Length:N0}): {Preview(repetends[i])}");
            Console.WriteLine();
            Console.WriteLine($"Full repetend digits saved to {Path.GetFileName(outputPath)} {outputLocationPhrase}.");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            if (Console.IsInputRedirected)
                Console.ReadLine();
            else
                Console.ReadKey(intercept: true);
            return 0;
        }

        var tables = cycles.Select(OffsetRatioTable.Build).ToList();
        var totalEquationLines = tables.Sum(t => (long)t.Cycle.Length * t.MaxOffset);
        var isLarge = totalEquationLines > LargeOutputLineThreshold;

        if (isLarge)
        {
            var largestCycle = tables.Max(t => t.Cycle.Length);
            Console.WriteLine(
                $"Divisor {divisor} produces {totalEquationLines:N0} equations across {tables.Count} cycle(s) " +
                $"(largest cycle length {largestCycle:N0}). That's too much to usefully display here, so this may " +
                "take a little while to generate and will be written straight to the output file.");
            Console.WriteLine();

            // Bypass the screen entirely for the heavy part, only the file writer gets it.
            Console.SetOut(fileWriter);

            for (var i = 0; i < tables.Count; i++)
            {
                ConsoleTableRenderer.Render(tables[i], i + 1);
                Console.WriteLine();
            }

            long equationsDone = 0;
            for (var i = 0; i < tables.Count; i++)
            {
                EquationRenderer.Render(tables[i], tables.Count > 1 ? i + 1 : null, linesJustWritten =>
                {
                    equationsDone += linesJustWritten;
                    var percent = equationsDone * 100 / totalEquationLines;
                    consoleOut.Write($"\rGenerating equations... {percent}%");
                });
                Console.WriteLine();
            }

            Console.SetOut(consoleOut);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(
                $"Full output ({totalEquationLines:N0} equation lines) saved to {Path.GetFileName(outputPath)} " +
                $"{outputLocationPhrase}. Open it in a text editor with word wrap turned off, some of the " +
                "numbers are very long, and wrapping them makes the table and equations unreadable.");
        }
        else
        {
            for (var i = 0; i < tables.Count; i++)
            {
                ConsoleTableRenderer.Render(tables[i], i + 1);
                Console.WriteLine();
            }

            Console.WriteLine("Press any key to see the resulting equations of the form A times a rotation of x equals B times x...");
            if (Console.IsInputRedirected)
                Console.ReadLine();
            else
                Console.ReadKey(intercept: true);
            Console.WriteLine();

            for (var i = 0; i < tables.Count; i++)
            {
                EquationRenderer.Render(tables[i], tables.Count > 1 ? i + 1 : null);
                Console.WriteLine();
            }

            Console.WriteLine($"The output of this program was saved to {Path.GetFileName(outputPath)} {outputLocationPhrase}");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        if (Console.IsInputRedirected)
            Console.ReadLine();
        else
            Console.ReadKey(intercept: true);

        return 0;
    }

    static int PromptForDivisor()
    {
        while (true)
        {
            Console.Write("Enter a divisor (integer >= 2, coprime to 10): ");
            var input = Console.ReadLine();
            if (TryParseDivisor(input, out var value, out var error))
                return value;

            Console.WriteLine(error);
        }
    }

    static bool TryParseDivisor(string? input, out int divisor, out string error)
    {
        divisor = 0;
        error = "";

        if (!int.TryParse(input, out divisor))
        {
            error = $"'{input}' is not an integer.";
            return false;
        }

        if (divisor < 2)
        {
            error = "Divisor must be at least 2.";
            return false;
        }

        if (Gcd(divisor, 10) != 1)
        {
            error = $"{divisor} shares a factor with 10 (must be coprime to 10 for a purely repeating expansion).";
            return false;
        }

        return true;
    }

    static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

    static string Preview(string digits)
    {
        const int edge = 40;
        return digits.Length <= edge * 2 + 3 ? digits : $"{digits[..edge]}...{digits[^edge..]}";
    }

    static string FormatByteCount(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
        var i = 0;
        while (bytes >= 1024 && i < units.Length - 1)
        {
            bytes /= 1024;
            i++;
        }

        return $"{bytes:0.#} {units[i]}";
    }

    static IEnumerable<string> WrapText(string text, int width)
    {
        var line = new System.Text.StringBuilder();
        foreach (var word in text.Split(' '))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                yield return line.ToString();
                line.Clear();
            }

            if (line.Length > 0)
                line.Append(' ');
            line.Append(word);
        }

        if (line.Length > 0)
            yield return line.ToString();
    }
}
