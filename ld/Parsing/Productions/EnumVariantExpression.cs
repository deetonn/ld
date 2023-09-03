
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class EnumVariantExpression(
    string EnumName, string VariantName, List<Expression>? Parameters,
    SourceLocation Location
) : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitEnumVariantExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine($"EnumVariant({EnumName}::{VariantName})");
        Parameters?.ForEach(x =>
        {
            x.Visualize(indent, false);
        });
    }
}
