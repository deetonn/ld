
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Conditional;

public record class IsEqualToExpression(Expression Left, Expression Right, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitIsEqualToExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine("IsEqualTo");

        Left.Visualize(indent, false);
        Right.Visualize(indent, true);
    }
}
