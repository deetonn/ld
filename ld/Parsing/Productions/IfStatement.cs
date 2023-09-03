
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class IfStatement(Expression What, Block OnTrue, Block? ElseBlock, SourceLocation Location)
    : Statement(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitIfStatement(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine("If");
        What.Visualize(indent, false);
        OnTrue.Visualize(indent, ElseBlock is null);
        ElseBlock?.Visualize(indent, true);
    }
}
