using Language.Parsing.Productions;

namespace Language.Extensions;

public static class HashMapExtensions
{
    public static void AddRange(this HashSet<AstNode> set, HashSet<AstNode> other)
    {
        foreach (var thing in other)
            set.Add(thing);
    }

    public static void AddRange(this HashSet<AstNode> set, IList<AstNode> other)
    {
        foreach (var thing in other)
            set.Add(thing);
    }
}
