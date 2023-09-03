
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Literals;

public record class ArrayLiteralExpression(List<Expression> Initializer, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitArrayLiteralExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine($"ArrayLiteral[{Initializer.Count}]");

        for (int i = 0; i < Initializer.Count; i++)
        {
            var expr = Initializer[i];
            last = i == (Initializer.Count - 1);
            expr.Visualize(indent, last);
        }
    }
}
