
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Conditional;

public record class IsGreaterThanExpression(Expression Left, Expression Right, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitIsGreaterThanExpression(this);
    }
}

public record class IsGreaterEqualToExpression(Expression Left, Expression Right, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitIsGreaterEqualToExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine("GreaterEqual");

        Left.Visualize(indent, false);
        Right.Visualize(indent, true);
    }
}
