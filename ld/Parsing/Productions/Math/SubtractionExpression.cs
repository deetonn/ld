
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Math;

public record class SubtractionExpression(Expression Left, Expression Right, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitSubtractionExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine("Subtraction");

        Left.Visualize(indent, false);
        Right.Visualize(indent, true);
    }
}
