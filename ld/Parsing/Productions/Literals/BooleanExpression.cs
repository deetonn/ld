
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Literals;

public record class BooleanExpression(bool Value, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitBooleanExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        _ = ShowIndent(indent, last);
        string repr = Value ? "true" : "false";
        AnsiConsole.WriteLine(repr);
    }
}
