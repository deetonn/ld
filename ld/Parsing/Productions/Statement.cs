
using Language.Lexing;

namespace Language.Parsing.Productions;

public record class Statement(SourceLocation Location) : AstNode(Location);
