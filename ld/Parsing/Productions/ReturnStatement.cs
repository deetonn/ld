
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class ReturnStatement(Expression? Return, SourceLocation Location)
    : Statement(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitReturnStatement(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.MarkupLine("Return");
        Return?.Visualize(indent, true);
    }
}
