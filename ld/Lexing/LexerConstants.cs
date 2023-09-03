
namespace Language.Lexing;

public static class LexerConstants
{
    public static readonly IDictionary<string, TokenKind> StrictKeywords = new Dictionary<string, TokenKind>()
    {
        ["let"] = TokenKind.Let,
        ["const"] = TokenKind.Const,
        ["mut"] = TokenKind.Mut,
        ["break"] = TokenKind.Break,
        ["continue"] = TokenKind.Continue,
        ["if"] = TokenKind.If,
        ["else"] = TokenKind.Else,
        ["enum"] = TokenKind.Enum,
        ["true"] = TokenKind.True,
        ["false"] = TokenKind.False,
        ["fn"] = TokenKind.Fn,
        ["for"] = TokenKind.For,
        ["impl"] = TokenKind.Impl,
        ["in"] = TokenKind.In,
        ["loop"] = TokenKind.Loop,
        ["match"] = TokenKind.Match,
        ["pub"] = TokenKind.Pub,
        ["return"] = TokenKind.Return,
        ["self"] = TokenKind.SelfValue,
        ["Self"] = TokenKind.SelfType,
        ["static"] = TokenKind.Static,
        ["struct"] = TokenKind.Struct,
        ["trait"] = TokenKind.Trait,
        ["use"] = TokenKind.Use,
        ["while"] = TokenKind.While,
        ["and"] = TokenKind.And,
        ["__ld_runtime_break"] = TokenKind.DebugBreak
    };

    public const char EqualsSign = '=';
    public const char Bang = '!';
    public const char GreaterThan = '>';
    public const char LessThan = '<';

    public const char LeftParen = '(';
    public const char RightParen = ')';

    public const char LeftBrace = '{';
    public const char RightBrace = '}';

    public const char QuestionMark = '?';
    public const char Colon = ':';
    public const char Comma = ',';
    public const char Dot = '.';
    public const char Semi = ';';

    public const char Plus = '+';
    public const char Minus = '-';
    public const char Slash = '/';
    public const char Star = '*';

    public const char Percent = '%';
    public const char Pipe = '|';
    public const char Ampersand = '&';

    public const char OpenBracket = '[';
    public const char CloseBracket = ']';

    public const char EOF = '\0';

    public static bool IsValidIdentStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    public static bool IsValidIdentRest(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_'; 
    }

    public static bool IsOkForGenericNumber(char c)
    {
        return char.IsDigit(c) || c == '.' || char.ToLower(c) == 'f';
    }
}
