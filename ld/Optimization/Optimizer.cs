
using Language.Parsing.Productions;

namespace Language.Optimization;

public class Optimizer
{
    private List<AstNode> OptimizedAst;
    private List<AstNode> OriginalAst;

    public Optimizer(List<AstNode> ast)
    {
        OriginalAst = ast;
        OptimizedAst = new List<AstNode>();
    }

    public List<AstNode> Optimize()
    {
        foreach (var node in OriginalAst)
        {
            OptimizeNode(node);
        }

        return OptimizedAst;
    }

    private void OptimizeNode(AstNode node)
    {

    }
}
