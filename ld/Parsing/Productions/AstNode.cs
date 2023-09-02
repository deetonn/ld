
using Language.Lexing;

namespace Language.Parsing.Productions;

public record class AstNode(SourceLocation Location)
{
    /// <summary>
    /// This function is responsible for visiting an AstNode.
    /// The visitor will be any kind of interpreter.
    /// </summary>
    /// <param name="visitor">The visitor.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">This is thrown when this function is not overriden.</exception>
    public virtual object Visit(IAstVisitor visitor)
    {
        throw new NotImplementedException($"The type \"{GetType().Name}\" has not implemented a visit function.");
    }

    public virtual void Visualize(string indent, bool last)
    {
        throw new NotImplementedException($"please implement Visualize() for {GetType().Name}");
    }

    protected static string ShowIndent(string indent, bool last)
    {
        Console.Write(indent);
        if (last)
        {
            Console.Write("\\-");
            indent += " ";
        }
        else
        {
            Console.Write("├─ ");
            indent += "│ ";
        }
        return indent;
    }
}
