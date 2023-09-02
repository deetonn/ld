
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class FunctionCall(string Identifier, List<Expression>? Arguments, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitFunctionCall(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.MarkupLine($"(Call to [italic]{Identifier}[/])");

        if (Arguments is not null)
        {
            for (int i = 0; i < Arguments.Count; ++i)
            {
                var arg = Arguments[i];
                if (i == (Arguments.Count - 1))
                {
                    arg.Visualize(indent, true);
                }
                else
                {
                    arg.Visualize(indent, false);
                }
            }
        }
    }
}
