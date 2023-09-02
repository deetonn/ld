using Language.Lexing;

namespace Language.Parsing.Productions;

public record class Expression(SourceLocation Location): AstNode(Location)
{
}
