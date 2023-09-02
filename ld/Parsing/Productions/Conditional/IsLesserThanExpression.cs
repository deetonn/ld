
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Conditional;

public record class IsLesserThanExpression(Expression Left, Expression Right, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitIsLesserThanExpression(this);
    }
}

public record class IsLesserEqualToExpression(Expression Left, Expression Right, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitIsLesserEqualToExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine("LesserThan");

        Left.Visualize(indent, false);
        Right.Visualize(indent, true);
    }
}
