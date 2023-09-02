
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class ReferencedVariable(string Identifier, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.ReferenceVariable(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.MarkupLine($"(Reference to [italic]{Identifier}[/])");
    }
}
