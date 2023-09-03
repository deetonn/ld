
using Language.Lexing;
using Spectre.Console;
using System.Text;

namespace Language.Parsing.Productions.Literals;

public record class VariableDotNotation(List<string> Path, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitDotNotation(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine(string.Join(".", Path));
    }
}
