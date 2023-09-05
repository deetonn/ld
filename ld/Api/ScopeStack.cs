
using System.Diagnostics.CodeAnalysis;

namespace Language.Api;

public class ScopeStack
{
    public Stack<LdScope> Scopes { get; private set; }

    public ScopeStack()
    {
        Scopes = new Stack<LdScope>();
    }

    public bool ExistsInAny(string identifier, [NotNullWhen(true)] out LdObject? instance)
    {
        instance = null;

        foreach (var item in Scopes)
        {
            if (item.TryGet(identifier, out var obj))
            {
                instance = obj;
                return true;
            }
        }

        return false;
    }

    public void PushScope(LdScope scope)
    {
        Scopes.Push(scope);
    }
    public LdScope PopScope()
    {
        return Scopes.Pop();
    }
    public LdScope GetCurrent()
    {
        return Scopes.Peek();
    }
}
