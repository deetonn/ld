
using System.Diagnostics.CodeAnalysis;

namespace Language.Api;

public class LdScope 
{
    private readonly IDictionary<string, LdObject> _thisScope;

    public LdScope()
    {
        _thisScope = new Dictionary<string, LdObject>();
    }
    public LdScope(LdScope other)
    {
        _thisScope = new Dictionary<string, LdObject>();
        foreach (var (key, value) in other._thisScope)
            _thisScope.Add(key, value);
    }

    public LdObject Store(string identifier, LdObject value)
    {
        if (_thisScope.ContainsKey(identifier))
        {
            var old = _thisScope[identifier];
            _thisScope[identifier] = value;
            return old;
        }
        _thisScope[identifier] = value;
        return value;
    }
    public void StoreFast(string identifier, LdObject value)
    {
        _thisScope[identifier] = value;
    }

    public bool ContainsObject(string identifier)
    {
        return _thisScope.ContainsKey(identifier);
    }

    public LdObject? Get(string identifier)
    {
        if (!ContainsObject(identifier))
            return null;
        return _thisScope[identifier];
    }
    public bool TryGet(string identifier, [NotNullWhen(true)] out LdObject? result)
    {
        result = Get(identifier);
        return result != null;
    }

    public void Merge(LdScope other)
    {
        foreach (var (key, value) in other._thisScope)
            _thisScope.Add(key, value);
    }
}
