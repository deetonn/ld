
namespace Language.Api;

/*
 * This object is the base of everything. All things must be stored
 * in one of these in one way or another.
*/

/// <summary>
/// The very base of all object inside of the runtime of
/// Ld.
/// </summary>
public class LdObject
{
    /// <summary>
    /// This is the underlying object.
    /// </summary>
    public object Underlying { get; protected set; }

    private readonly Guid _refId;

    public LdObject(object underlying)
    {
        Underlying = underlying;
        _refId = Guid.NewGuid();
    }

    public bool IsCallable()
        => GetType() == typeof(LdFunction);

    public bool IsU32() => GetType() == typeof(LdU32);

    /// <summary>
    /// This object unique reference Id. If this is equal to
    /// another <see cref="ReferenceId()"/> they are pointing to the
    /// same instance.
    /// </summary>
    /// <returns>The object unique reference ID.</returns>
    public Guid ReferenceId() => _refId;
}
