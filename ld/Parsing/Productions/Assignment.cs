using Language.Api;
using Language.Lexing;
using Spectre.Console;
using System.Reflection;

namespace Language.Parsing.Productions;

public record class Assignment(
    // The identifier that has been assigned to.
    string Identifier,
    bool IsMutable,
    // The type that has been annotated after the name.
    TypeInformation? AnnotatedType,
    // The expression being assigned.
    Expression? Expression,
    SourceLocation Location
) : Statement(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitAssignment(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        string mutable = IsMutable ? "Mutable" : "Immutable";
        AnsiConsole.MarkupLine($"(Assignment of \"{Identifier}\", {mutable}, {AnnotatedType?.Name})");
        Expression?.Visualize(indent, true);
    }

    public const string Discard = "$";
}
