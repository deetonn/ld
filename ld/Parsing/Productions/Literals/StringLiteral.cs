
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Literals;

public record class StringLiteral(string Value, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitStringLiteral(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine($"{Value}");
    }
}
