
using Language.Lexing;
using Language.Parsing.Productions;

namespace Language.Api;

public class LdU32 : LdObject
{

    /// <summary>
    /// The default constructor. defaults to 0.
    /// </summary>
    public LdU32()
        : base((uint)0)
    {}

    public LdU32(uint value)
        : base(value) { }

    public Result<LdU32, string> PerformAdditon(LdU32 other)
    {
        var value = GetInner();

        if (value is null)
        {
            return new($"internal storage of u32 has been corrupted.");
        }

        var otherInner = other.GetInner();

        if (otherInner is null)
        {
            return new($"internal storage of u32 has been corrupted.");
        }

        return new(new LdU32(value.Value + otherInner.Value));
    }

    private uint? GetInner()
    {
        if (Underlying is uint Value)
            return Value;
        return null;
    }
}
