
using System.Diagnostics.CodeAnalysis;

namespace Language.Api;

public class ScopeId
{
    private readonly Guid _uniqueId;
    private readonly string _name;

    public ScopeId(Guid uniqueId, string name)
    {
        _uniqueId = uniqueId;
        _name = name;
    }

    public Guid GetId() => _uniqueId;
    public string Name => _name;

    public static ScopeId Unique(string name)
        => new(Guid.NewGuid(), name);
}

public class LdScope 
{
    private readonly Dictionary<string, LdObject> _thisScope;
    private ScopeId _id;

    public LdScope(ScopeId id)
    {
        _thisScope = new Dictionary<string, LdObject>();
        _id = id;
    }
    public LdScope(LdScope other)
    {
        _thisScope = new Dictionary<string, LdObject>();
        foreach (var (key, value) in other._thisScope)
            _thisScope.Add(key, value);
        _id = other._id;
    }

    public LdObject Store(string identifier, LdObject value)
    {
        if (_thisScope.TryGetValue(identifier, out LdObject? old_value))
        {
            var old = old_value;
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

    public ScopeId GetId()
        => _id;

    public void Merge(LdScope other)
    {
        foreach (var (key, value) in other._thisScope)
            _thisScope.Add(key, value);
    }
}
