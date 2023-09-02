
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Conditional;

public record class NotExpression(Expression Expr, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitNotExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine("Not");

        Expr.Visualize(indent, true);
    }
}
