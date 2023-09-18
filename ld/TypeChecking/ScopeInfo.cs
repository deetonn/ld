
using System.Diagnostics.CodeAnalysis;
using Language.Api;

namespace Language.TypeChecking;

public class ScopedObject
{
    public required string Identifier { get; init; }
    public required TypeInformation? Type { get; init; }
    public required bool Constant { get; init; }
}

/// <summary>
/// This class is used within the type checker. This object represents
/// what entitys are in scope of whatever. So, in a function that
/// is on the top level. The global scope along with arguments would
/// be available. That is what this type encapsulates.
/// </summary>
public class ScopeInfo
{
    private readonly List<ScopedObject> _scope;
    private TypeInformation? _returnType;

    public ScopeInfo(List<ScopedObject> initialScope)
    {
        _scope = initialScope;
    }

    public void SetReturnType(TypeInformation? info)
    {
        _returnType = info;
    }

    public TypeInformation? GetReturnType() => _returnType;

    public void Add(string ident, TypeInformation information, bool constant)
    {
        _scope.Add(new ScopedObject {
            Identifier = ident,
            Type = information,
            Constant = constant 
        });
    }
    public void JoinWith(ScopeInfo other)
    {
        _scope.AddRange(other._scope);
    }
    public bool Contains(string ident, [NotNullWhen(true)] out TypeInformation? type)
    {
        var couldBeInfo = _scope
            .Where(x => x.Identifier == ident)
            .FirstOrDefault();

        if (couldBeInfo is null)
        {
            type = null;
            return false;
        }

        type = couldBeInfo.Type;
        return true;
    }
}
