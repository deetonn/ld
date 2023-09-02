
namespace Language.Lexing;

public enum TokenKind
{
    // Basic characters.
    /// <summary>
    /// The "=" sign. This has a covered test.
    /// </summary>
    Equals,
    /// <summary>
    /// The "!" character. This has a covered test.
    /// </summary>
    Bang,
    /// <summary>
    /// The "&gt;" character. This has a covered test.
    /// </summary>
    Gt,
    /// <summary>
    /// The "&lt;" character. This has a covered test.
    /// </summary>
    Lt,
    /// <summary>
    /// The "(" character. This has a covered test.
    /// </summary>
    LeftParen,
    /// <summary>
    /// The ")" character. This has a covered test.
    /// </summary>
    RightParen,
    /// <summary>
    /// The "{" character. This has a covered test.
    /// </summary>
    LeftBrace,
    /// <summary>
    /// The "}" character. This has a covered test.
    /// </summary>
    RightBrace,
    /// <summary>
    /// The "?" character. This has a covered test.
    /// </summary>
    QuestionMark,
    /// <summary>
    /// The ":" character. This has a covered test.
    /// </summary>
    Colon,
    /// <summary>
    /// The "," character. This has a covered test.
    /// </summary>
    Comma,
    /// <summary>
    /// The ";" character. This has a covered test.
    /// </summary>
    Semi,

    // Math specific characters.
    /// <summary>
    /// The "+" sign. This has a covered test.
    /// </summary>
    Plus,
    /// <summary>
    /// The "-" sign. This has a covered test.
    /// </summary>
    Minus,
    /// <summary>
    /// The "/" sign. This has a covered test.
    /// </summary>
    Slash,
    /// <summary>
    /// The "*" sign. This has a covered test.
    /// </summary>
    Star,

    // "Other" characters.

    /// <summary>
    /// The "%" character. This has a covered test.
    /// </summary>
    Modulo,
    /// <summary>
    /// The "|" character. This has a covered test.
    /// </summary>
    Pipe,
    /// <summary>
    /// The "&amp;" character. This has a covered test.
    /// </summary>
    Ampersand,
    /// <summary>
    /// The "==" operator. This has a covered test.
    /// </summary>
    EqualEqual,
    /// <summary>
    /// The "!=" operator. This has a covered test.
    /// </summary>
    NotEqual,
    /// <summary>
    /// The "&amp;&amp;" operator, or the keyword "and". This has a covered test.
    /// </summary>
    And,
    /// <summary>
    /// The ">=" operator. This has a covered test.
    /// </summary>
    GreaterEquals,
    /// <summary>
    /// The "<=" operator. This has a covered test. 
    /// </summary>
    LesserEquals,
    /// <summary>
    /// The "+=" operator. This has a covered test.
    /// </summary>
    PlusEquals,
    /// <summary>
    /// The "-=" operator. This has a covered test.
    /// </summary>
    MinusEquals,
    /// <summary>
    /// The "||" operator. This has a covered test.
    /// </summary>
    Or,
    /// <summary>
    /// Any number, signed, unsigned, floating point. This has a covered test.
    /// </summary>
    Number,
    /// <summary>
    /// Any identifier that is not a reserved keyword. This has a covered test.
    /// </summary>
    Identifier,
    /// <summary>
    /// A string literal. This has a covered test.
    /// </summary>
    StringLiteral,

    /// <summary>
    /// The "::" identifier seperator. This has a covered test.
    /// </summary>
    ColonColon,
    Dot,
    Arrow,

    Eof,

    // Keywords
    Let, Const, Mut, Break, Continue, 
    If, Else, Enum, True, False, Fn, For,
    Impl, In, Loop, Match, Pub, Return,
    SelfValue, SelfType, Static, Struct, Trait,
    Use, While
}
