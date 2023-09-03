
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class InlineStructInitializationParameter(string Identifier, Expression Expression, SourceLocation Location)
{
    public void Visualize(string structName, string indent, bool last)
    {
        indent = AstNode.ShowIndent(indent, last);
        AnsiConsole.Write($"{structName}::{Identifier} = ");
        Expression.Visualize(indent, last);
    }
}

public record class StructInitializationExpression(
    string Identifier, List<InlineStructInitializationParameter>? Initializers, SourceLocation Location
) : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitStructInitialization(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine($"StructInit({Identifier})");

        Initializers?.ForEach(x =>
        {
            x.Visualize(Identifier, indent, last);
        });
    }
}
