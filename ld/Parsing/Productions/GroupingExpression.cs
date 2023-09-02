
using Language.Lexing;

namespace Language.Parsing.Productions;

// NOTE: the expressions should be reversed before
//       being passed into GroupingExpression so they can
//       be easily evaluated right-to-left.
//                           ^^^^^^^^^^^^^
//       because expressions on the right take precedence.

public record class GroupingExpression(
    List<Expression> Expressions,
    SourceLocation Location
) : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitGrouping(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);

        for (int i = 0; i < Expressions.Count; i++) 
        {
            var e = Expressions[i];
            if (i == Expressions.Count - 1)
            {
                e.Visualize(indent, true);
            }
            else
            {
                e.Visualize(indent, false);
            }
        }
    }
}
