
using Language.Api;
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class EnumVariantDeclaration(string Name, List<TypeInformation>? TaggedTypes, SourceLocation Location)
    : Declaration(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitEnumVariantDeclaration(this);
    }

    public override void Visualize(string indent, bool last)
    {
        _ = ShowIndent(indent, last);
        var tags = (TaggedTypes?.Count ?? 0) == 0 ? "No tags" : $"{TaggedTypes?.Count} tag(s)";
        AnsiConsole.WriteLine($"EnumVariant({Name}, {tags})");
    }
}

public record class EnumDeclaration(string Identifier, List<EnumVariantDeclaration>? Variants, SourceLocation Location)
    : Declaration(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitEnumDeclaration(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine($"Enum({Identifier})");
        Variants?.ForEach(x =>
        {
            x.Visualize(indent, false);
        });
    }
}
