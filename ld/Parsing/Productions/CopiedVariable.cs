using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class CopiedVariable(string Identifier, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.CopyVariable(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.MarkupLine($"(Copy of [italic]{Identifier}[/])");
    }
}
