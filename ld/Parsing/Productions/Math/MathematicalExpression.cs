using Language.Lexing;

namespace Language.Parsing.Productions.Math;

public record class MathematicalExpression(Expression Left, Expression Right, SourceLocation Location)
    : Expression(Location)
{
}
