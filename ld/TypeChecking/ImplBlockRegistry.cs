using Language.Parsing.Productions;

namespace Language.TypeChecking;

public class ImplBlockRegistry
{
    private readonly List<ImplStatement> _impls;

    public ImplBlockRegistry(List<ImplStatement> impls)
    {
        _impls = impls;
    }

    public List<FunctionDeclaration>? GetDeclarationsOf(string ident)
    {
        return 
            _impls.Where(x => x.Identifier == ident)
            .Select(x => x.Declarations)
            .SelectMany(x => x)
            .ToList();
    }

    public List<ImplStatement> Impls() => _impls;
}
