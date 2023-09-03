
using System.Diagnostics;

namespace Language.Parsing.Productions.Debugging;

public class ParserDebugBreakExpression
{
    public ParserDebugBreakExpression()
    {
        Debugger.Break();
    }
}
