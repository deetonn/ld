
using Language.Lexing;

namespace Language.Parsing.Productions;

// NOTE(s):
/*
 * The reason that blocks are expressions is to allow for 
 * expression statements. If they are treated like expressions
 * you could do something like:
 *   let a = {
 *     let side_effect = get_data();
 *     return side_effect.data
 *   }
 *   
 * Then "a" would be side_effect.data and it removes the need for
 * cleanup code before and the usual mess.
*/

public record class Block(List<AstNode> Nodes, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitBlock(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        for (int i = 0; i < Nodes.Count; i++)
        {
            var node = Nodes[i];
            if (i == (Nodes.Count - 1))
            {
                node.Visualize(indent, true);
            }
            else
            {
                node.Visualize(indent, false);
            }
        }
    }
}
