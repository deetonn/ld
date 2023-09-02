
using Language.Lexing;

namespace Language.Parsing.Productions;

public record class Declaration(SourceLocation Location): AstNode(Location);
