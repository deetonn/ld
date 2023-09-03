
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
}
