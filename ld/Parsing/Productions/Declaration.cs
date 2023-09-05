
using Language.Lexing;

namespace Language.Parsing.Productions;

public record class Declaration(string Identifier, SourceLocation Location): AstNode(Location);
