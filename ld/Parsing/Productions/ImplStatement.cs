
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

/*
 * NOTE: This class is only for "impl X {}" statements.
 * Use the ImplForStatement for trait implementations.
*/
public record class ImplStatement(string Identifier, List<FunctionDeclaration> Declarations, SourceLocation Location)
    : Statement(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitImplStatement(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine($"Impl({Identifier})");

        foreach (var fn in Declarations)
        {
            fn.Visualize(indent, false);
        }
    }
}
