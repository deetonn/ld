namespace Language.Lexing;

public record Span(uint Begin, uint End)
{
    public override string ToString()
    {
        return $"Span {{ Begin = {Begin}, End = {End} }}";
    }
}

public class SourceLocation
{
    public string File { get; init; }

    public uint Line { get; init; }
    public uint Column { get; internal set; }

    public Span Span { get; init; }

    public SourceLocation(uint line, uint col, string file, Span span)
    {
        Line = line;
        Column = col;
        File = file;
        Span = span;
    }

    public SourceLocation WithColumn(uint col)
    {
        Column = col;
        return this;
    }

    public override string ToString()
    {
        return $"SourceLocation {{ File = {File}, Line = {Line}, Column = {Column}, Span = {Span} }}";
    }

    public string ToUserString()
    {
        return $"[[src/{File}:{Line}:{Column}]]";
    }
}

public class Token
{
    public TokenKind Kind { get; init; }
    public SourceLocation Location { get; init; }
    public string? Lexeme { get; internal set; }

    public Token(TokenKind kind, SourceLocation loc, string? lexeme)
    {
        Kind = kind;
        Location = loc;
        Lexeme = lexeme;
    }

    public static Token WithoutLexeme(TokenKind kind, SourceLocation location)
    {
        return new Token(kind, location, null);
    }

    public static Token From(TokenKind kind, SourceLocation location, string? lexeme)
    {
        return new Token(kind, location, lexeme);
    }

    public override string ToString()
    {
        return $@"Token {{ Kind = {Kind}, Lexeme = {Lexeme}, Location = {Location} }},";
    }
}
