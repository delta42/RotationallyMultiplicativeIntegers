using System.Text;

namespace RotationallyMultiplicativeIntegers;

/// <summary>
/// Mirrors everything written to it into two underlying writers, so console output
/// can be shown on screen and simultaneously saved to a file.
/// </summary>
class TeeTextWriter(TextWriter first, TextWriter second) : TextWriter
{
    public override Encoding Encoding => first.Encoding;

    public override void Write(char value)
    {
        first.Write(value);
        second.Write(value);
    }

    public override void Write(string? value)
    {
        first.Write(value);
        second.Write(value);
    }

    public override void Flush()
    {
        first.Flush();
        second.Flush();
    }
}
