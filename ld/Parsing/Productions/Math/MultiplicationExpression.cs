
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Math;

public record class MultiplicationExpression(Expression Left, Expression Right, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitMultiplicationExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine("Multiplication");

        Left.Visualize(indent, false);
        Right.Visualize(indent, true);
    }
}
