
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Literals;

public record class Number64Bit(long Value, bool IsUnsigned, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.Visit64BitInteger(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        var signedNess = IsUnsigned ? "u" : string.Empty;
        AnsiConsole.WriteLine($"{Value}{signedNess}");
    }
}
