using Language.Lexing;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Language.Parsing.Productions;

public class LeftToRightList<T> : List<T> {}

public record class ColonNotation(LeftToRightList<string> Path, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitColonNotation(this);
    }

    public override void Visualize(string indent, bool last)
    {
        _ = ShowIndent(indent, last);
        AnsiConsole.WriteLine(string.Join("::", Path));
    }
}
