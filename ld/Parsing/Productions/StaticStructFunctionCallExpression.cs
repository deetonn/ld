
using Language.Api;
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class StaticStructFunctionCallExpression(
    string StructName, string FunctionName, List<TypeInformation>? Generics, List<Expression>? Arguments,
    SourceLocation Location
) : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitStaticStructAccessExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine($"StaticCall({StructName}::{FunctionName})");
        Arguments?.ForEach(x =>
        {
            x.Visualize(indent, false);
        });
    }
}
