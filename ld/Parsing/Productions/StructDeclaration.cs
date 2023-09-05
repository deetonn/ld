
using Language.Api;
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class StructFieldDeclaration(string Identifier, TypeInformation Type, SourceLocation Location)
    : Declaration(Identifier, Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitStructFieldDeclaration(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine($"StructField({Identifier})");
    }
}

public record class StructDeclaration(string Identifier, List<StructFieldDeclaration> Fields, SourceLocation Location)
    : Declaration(Identifier, Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitStructDeclaration(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine($"Struct({Identifier})");

        foreach (var field in Fields)
        {
            field.Visualize(indent, false);
        }
    }
}
