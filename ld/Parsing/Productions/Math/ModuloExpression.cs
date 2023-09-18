
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Math;

public record class ModuloExpression(Expression Left, Expression Right, SourceLocation Location)
    : MathematicalExpression(Left, Right, Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitModuloExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine("Modulo");

        Left.Visualize(indent, false);
        Right.Visualize(indent, true);
    }
}
