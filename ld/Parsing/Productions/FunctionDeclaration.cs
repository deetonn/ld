
using Language.Api;
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

public record class FunctionDeclarationParameter(string Identifier, TypeInformation Type, SourceLocation Location);

// NOTE: When FunctionDeclaration's return type is null,
//       it defaults to void.

/*
 * ReturnType = u32
 * fn do_something() -> u32 {}
 * 
 * ReturnType = void
 * fn do_something() {}
*/

public record class FunctionDeclaration(
    string Identifier,
    List<TypeInformation>? GenericParams,
    List<FunctionDeclarationParameter>? Parameters,
    TypeInformation? ReturnType,
    Block Body,
    SourceLocation Location
): Declaration(Identifier, Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitFunctionDeclaration(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.MarkupLine($"(Declaration of function {Identifier})");

        Body.Visualize(indent, true);
    }
}
