
namespace Language.Lexing;

using Language.ErrorHandling;
using System.Text;
using static LexerConstants;

public struct LexerInternals
{
    public uint Line;
    public uint Column;
    public string CurrentFile;

    // For Span(Back, Front);
    public uint Back, Front;
}

public class Lexer
{
    private readonly List<Token> _tokens;
    private LexerInternals _internals;
    private int _position;
    private readonly string _contents;

    public Lexer(FileInfo sourceFile)
    {
        _contents = File.ReadAllText(sourceFile.FullName);
        _position = -1;
        _tokens = new List<Token>();
        _internals = new LexerInternals()
        {
            Front = 0,
            Back = 0,
            Line = 1,
            Column = 1,
            CurrentFile = sourceFile.Name
        };
    }

    public Lexer(string contents)
    {
        _contents = contents;
        _position = -1;
        _tokens = new List<Token>();
        _internals = new LexerInternals()
        {
            Front = 0,
            Back = 0,
            Line = 1,
            Column = 1,
            CurrentFile = "[tests]"
        };
    }

    public List<Token> LexTokens()
    {
        if (_tokens.Count != 0)
            return _tokens;

        while (true)
        {
            var token = LexSingleToken();

            if (token is null)
                continue;

            if (token.Kind == TokenKind.Eof)
            {
                break;
            }

            _tokens.Add(token);
        }

        _tokens.Add(new Token(TokenKind.Eof, GetCurrentSourceLocation(), null));

        ErrorHandler.DisplayThenExitIfAny();

        return _tokens;
    }

    public Token? LexSingleToken()
    {
        var next = Advance();

        switch(next)
        {
            case '\n':
                {
                    _internals.Line += 1;
                    _internals.Column = 1;
                    _internals.Back = _internals.Front;
                    return null;
                }
            // fuck windows man fr
            case '\r':
                {
                
                    if (PeekNext() == '\n')
                    {
                        _ = Advance();
                        _internals.Line += 1;
                        _internals.Column = 1;
                        _internals.Back = _internals.Front;
                        return null;
                    }
                    return null;
                }
            case EqualsSign:
                {
                    if (PeekNext() == EqualsSign)
                    {
                        _ = Advance();
                        return MakeToken(TokenKind.EqualEqual);
                    }
                    return MakeToken(TokenKind.Equals);
                }
            case Bang:
                {
                    if (PeekNext() == EqualsSign)
                    {
                        _ = Advance();
                        return MakeToken(TokenKind.NotEqual);
                    }
                    return MakeToken(TokenKind.Bang);
                }
            case GreaterThan:
                {
                    if (PeekNext() == EqualsSign)
                    {
                        _ = Advance();
                        return MakeToken(TokenKind.GreaterEquals);
                    }
                    return MakeToken(TokenKind.Gt);
                }
            case LessThan:
                {
                    {
                        if (PeekNext() == EqualsSign)
                        {
                            _ = Advance();
                            return MakeToken(TokenKind.LesserEquals);
                        }
                        return MakeToken(TokenKind.Lt);
                    }
                }
            case LeftParen: return MakeToken(TokenKind.LeftParen);
            case RightParen: return MakeToken(TokenKind.RightParen);
            case LeftBrace: return MakeToken(TokenKind.LeftBrace);
            case RightBrace: return MakeToken(TokenKind.RightBrace);
            case QuestionMark: return MakeToken(TokenKind.QuestionMark);
            case Colon:
                {
                    if (PeekNext() == Colon)
                    {
                        _ = Advance();
                        return MakeToken(TokenKind.ColonColon);
                    }
                    return MakeToken(TokenKind.Colon);
                }
            case Comma: return MakeToken(TokenKind.Comma);
            case Semi: return MakeToken(TokenKind.Semi);
            case Plus:
                {
                    if (PeekNext() == EqualsSign)
                    {
                        _ = Advance();
                        return MakeToken(TokenKind.PlusEquals);
                    }
                    return MakeToken(TokenKind.Plus);
                }
            case Minus:
                {
                    if (PeekNext() == EqualsSign)
                    {
                        _ = Advance();
                        return MakeToken(TokenKind.MinusEquals);
                    }
                    if (IsOkForGenericNumber(PeekNext())) 
                    {
                        // There is no whitespace between the - and the number.
                        // meaning this is a number.
                        var nextForNumber = Advance();
                        var number = LexNumber(nextForNumber);
                        number.Lexeme = $"-{number.Lexeme}";
                        return number;
                    }
                    if (PeekNext() == GreaterThan)
                    {
                        _ = Advance();
                        return MakeToken(TokenKind.Arrow);
                    }
                    return MakeToken(TokenKind.Minus);
                }
            case Slash: return MakeToken(TokenKind.Slash);
            case Star: return MakeToken(TokenKind.Star);
            case Percent: return MakeToken(TokenKind.Modulo);
            case Pipe:
                {
                    if (PeekNext() == Pipe)
                    {
                        _ = Advance();
                        return MakeToken(TokenKind.Or);
                    }
                    return MakeToken(TokenKind.Pipe);
                }
            case Ampersand:
                {
                    if (PeekNext() == Ampersand)
                    {
                        _ = Advance();
                        return MakeToken(TokenKind.And);
                    }
                    return MakeToken(TokenKind.Ampersand);
                }
            case Dot: return MakeToken(TokenKind.Dot);
            case var c when char.IsWhiteSpace(c): return null;
            case var c when char.IsNumber(c): return LexNumber(c);
            case var c when IsValidIdentStart(c): return LexIdentifierOrKeyword(c);
            case var c when c == '"': return LexString(consumedFirstDelim: true);
            case EOF: return MakeToken(TokenKind.Eof);
            default:
                throw new NotImplementedException($"unhandled character: {next}");
        };
    }

    private Token? LexString(bool consumedFirstDelim)
    {
        var startCol = _internals.Column;

        if (!consumedFirstDelim)
            _ = Advance();

        var result = new StringBuilder();

        var current = Peek();

        if (current == '"') { current = Advance(); }

        while (true)
        {
            if (current == '"')
            {
                break;
            }

            if (current == '\\')
            {
                var escape = Advance();
                if (!EscapeMap.TryGetValue(escape, out char code))
                {
                    var error = new ErrorBuilder(_contents)
                        .WithMessage($"unknown escape code: \"{escape}\".")
                        .WithSourceLocation(GetCurrentSourceLocation().WithColumn(startCol))
                        .WithCode(LdErrorCode.UnknownEscapeSequence)
                        .WithNote($"valid escapes are: ({string.Join(", ", EscapeMap.Keys)})");
                    ErrorHandler.QueueNow(error.Build());
                    // ignore for now to catch more errors as we go.
                }
                result.Append(code);
            }
            else
            {
                result.Append(current);
            }

            if (PeekNext() == '"')
            {
                _ = Advance();
                break;
            }

            current = Advance();
        }

        return MakeToken(TokenKind.StringLiteral, result.ToString());
    }

    public readonly Dictionary<char, char> EscapeMap = new()
    {
        ['a'] = '\a',
        ['b'] = '\b',
        ['e'] = '\b',
        ['f'] = '\f',
        ['n'] = '\n',
        ['r'] = '\r',
        ['t'] = '\t',
        ['v'] = '\v',
        ['\\'] = '\\',
        ['\''] = '\'',
        ['"'] = '"'
    };

    private Token? LexIdentifierOrKeyword(char c)
    {
        var result = new StringBuilder();
        result.Append(c);

        if (!IsValidIdentRest(PeekNext()))
        {
            return MakeToken(TokenKind.Identifier, result.ToString());
        }

        var current = Advance();

        while (true)
        {
            if (!IsValidIdentRest(current))
            {
                break;
            }

            result.Append(current);

            if (!IsValidIdentRest(PeekNext()))
            {
                break;
            }

            current = Advance();
        }

        // identifier is done, check if its a keyword.
        var realIdent = result.ToString();

        if (!StrictKeywords.TryGetValue(realIdent, out var kind))
        {
            return MakeToken(TokenKind.Identifier, realIdent);
        }

        return MakeToken(kind);
    }

    private Token LexNumber(char c)
    {
        var result = new StringBuilder();
        result.Append(c);

        if (!IsOkForGenericNumber(PeekNext()))
        {
            return MakeToken(TokenKind.Number, result.ToString());
        }

        var current = Advance();
        
        while (true)
        {
            if (!IsOkForGenericNumber(current))
            {
                break;
            }

            result.Append(current);

            if (!IsOkForGenericNumber(PeekNext()))
            {
                break;
            }

            current = Advance();
        }

        return MakeToken(TokenKind.Number, result.ToString());
    }

    private SourceLocation GetCurrentSourceLocation()
    {
        var span = new Span(_internals.Back, _internals.Front);

        return new SourceLocation(
            _internals.Line, _internals.Column,
            _internals.CurrentFile, span);
    }

    private char Peek()
    {
        if (_position > _contents.Length)
            return EOF;
        return _contents[_position];
    }

    private char PeekNext()
    {
        var nextPosition = _position + 1;
        if (nextPosition >= _contents.Length)
            return EOF;
        return _contents[nextPosition];
    }

    private char Advance()
    {
        if (++_position >= _contents.Length)
        {
            return EOF;
        }

        _internals.Front++;
        _internals.Column++;
        return _contents[_position];
    }

    private Token MakeToken(TokenKind kind, string? lexeme = null)
    {
        var span = new Span(_internals.Back, _internals.Front);
        _internals.Back = _internals.Front;

        var sourceLocation = new SourceLocation(
            _internals.Line, _internals.Column,
            _internals.CurrentFile, span);

        return Token.From(kind, sourceLocation, lexeme);
    }
}
