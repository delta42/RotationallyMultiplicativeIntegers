# Rotationally Multiplicative Integers

A small .NET console tool that accompanies the paper *Rotationally Multiplicative
Integers*. The paper generalizes Jacob Bronowski's 1949 puzzle —
find the smallest integer that becomes 1.5 times itself when its leading digit
is moved to the end — into a general theory of digit rotations driven by
repeating decimal expansions.

Given a divisor coprime to 10, this tool:

1. Finds the repetend cycle(s) of its decimal expansion (the digits of `1/d`,
   `2/d`, ... grouped by which ones share a repeating block).
2. Builds the offset-ratio table from the paper: for every pair of positions
   `t` apart in the cycle, the ratio `B/A` such that `A * rotation(x) = B * x`.
3. Expands every table entry into the concrete equation it represents.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Build & run

```sh
dotnet build
dotnet run
```

You'll be prompted for a divisor, or you can pass one directly:

```sh
dotnet run -- 7
```

The divisor must be an integer >= 2 and coprime to 10 (otherwise the decimal
expansion has a non-repeating prefix and falls outside the theory the tool
implements).

To publish a self-contained single-file executable:

```sh
dotnet publish -r win-x64    # or linux-x64
```

## Example

```
$ dotnet run -- 7
Divisor 7: 1 cycle(s) among the coprime remainders.

======= Cycle 1 (repetend 142,857, length 6) =======
Repetend Digit            1    4    2    8    5    7
Intermediate Remainder    1    3    2    6    4    5
----------------------------------------------------
Offset 1                  3  2/3    3  2/3  5/4  1/5
Offset 2                  2    2    2  5/6  1/4  3/5
...

Offset 1:
      428,571 = 3 * 142,857
  3 * 285,714 = 2 * 428,571
  ...
```

Every run also writes its full output to a timestamped `RMI-*.txt` file on
your desktop (or home directory, if no desktop folder exists) — open it with
word wrap turned off, since some of the numbers involved are very long.

## Large inputs

A cycle of length `L` produces on the order of `L^3` characters of equation
output (`L*(L-1)` equations, each holding two `~L`-digit numbers), so both
memory and disk usage grow fast. Past a certain length, generating the full
offset-ratio table and equation list becomes impractical (gigabytes of RAM,
then terabytes of disk), so the tool refuses to attempt it and instead falls
back to just writing out the repetend digit(s) themselves — cheap to produce
since that only costs `O(L)`. If even that is too large (only possible for a
divisor near `int.MaxValue` with an enormous cycle), it refuses outright and
asks for a smaller divisor.

## Project layout

| File | Responsibility |
|---|---|
| `Program.cs` | CLI entry point, input validation, output-size safety checks, orchestration |
| `RepetendCycles.cs` | Finds the repetend cycle(s) of a divisor's decimal expansion |
| `OffsetRatioTable.cs` | Builds the offset-ratio table for a cycle |
| `ConsoleTableRenderer.cs` | Renders the offset-ratio table |
| `EquationRenderer.cs` | Expands table entries into `A * rotation(x) = B * x` equations |
| `TeeTextWriter.cs` | Mirrors console output to the saved output file |
