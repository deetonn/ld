
using Language.Api;
using Language.ErrorHandling;
using Language.Lexing;
using Language.Parsing.Productions;
using Language.Parsing.Productions.Literals;
using Language.Parsing.Productions.Math;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Language.Parsing;

public class ParserDoneException : Exception { }

public class Parser
{
    private List<Token> _tokens;
    private List<AstNode> _ast;
    private int _position;
    private string _source;

    public Parser(List<Token> tokens, string source)
    {
        _tokens = tokens;
        _position = 0;
        _ast = new List<AstNode>();
        _source = source;
    }

    public TypeInformation ParseTypename()
    {
        var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
            .WithMessage("expected an identifier. (while parsing type)")
            .WithNote("specify a typename.")
            .Build());

        var typeInfo = new TypeInformation(identifier.Lexeme!);

        if (Matches(TokenKind.Lt))
        {
            typeInfo.Generics = new List<TypeInformation>();

            while (!Matches(TokenKind.Gt))
            {
                var genericParam = ParseTypename();
                var nextToken = PeekNext();

                if (nextToken is not null && nextToken.Kind == TokenKind.Gt)
                {
                    _ = Expect(TokenKind.Comma, GetErrorBuilder()
                        .WithMessage("expected comma seperating generic parameter.")
                        .WithNote($"add a comma after \"{genericParam.Name}\"")
                        .Build());
                }

                typeInfo.Generics.Add(genericParam);
            }
        }

        return typeInfo;
    }

    /// <summary>
    /// Any tokens that can be used as mathematical operators.
    /// </summary>
    private readonly TokenKind[] _mathTokens = new TokenKind[]
    {
        TokenKind.Plus, TokenKind.Minus, 
        TokenKind.Slash, TokenKind.Star,
        TokenKind.Modulo
    };

    /// <summary>
    /// This function handles all expressions. This is regardless of type,
    /// function calls, literals, math, etc... all handled in here.
    /// </summary>
    /// <returns></returns>
    public Expression? ParseExpression()
    {
        // I don't want to use the usual deep call stack business
        // when you call ParseXXX in each function. It doesn't make it easy
        // to work with.

        /*
         * With most expressions, you can figure out what's going on
         * with the next token. If there is no next token, then simple.
         */

        var current = Peek();
        var next = PeekNext();

        if (current is null)
        {
            // there is no expression as there is nothing here.
            return null;
        }

        // if next is null, there is no other character, so it's EOF.
        // short circuit on this case.
        if (next is null)
        {
            return ParsePrimaryExpression();
        }

        // If the next is a LeftParen & the current is an Identifier
        // this is definately a function call.
        if (next.Kind is TokenKind.LeftParen && current.Kind is TokenKind.Identifier)
        {
            return ParseFunctionCall();
        }

        // If the next token is a mathematical operator, this
        // is a mathematical expression.
        if (_mathTokens.Contains(next.Kind))
        {
            return ParseMathExpression();
        }

        // If the next token is a dot, this is a variable access.
        if (next.Kind is TokenKind.Dot)
        {
            return ParseDotNotation();
        }

        // if the current is an ampersand and the next is an identifier,
        // this is an expression with intent of referencing a variable.
        if (current.Kind is TokenKind.Ampersand
            && next.Kind is TokenKind.Identifier)
        {
            // skip "&identifier"
            MoveForwardBy(2);
            // TODO: Make this parse expressions too, it's not just going
            // to be single variables being referenced.
            var identifier = next.Lexeme;
            return new ReferencedVariable(identifier!, current.Location); ;
        }

        return ParsePrimaryExpression();
    }

    public Expression ParseMathExpression()
    {
        // NOTE: math expressions just boil down to
        //       groupings always. This keeps code
        //       evaluating things consistent with precidence.

        // NOTE[2]: Actual groupings are not supported yet.

        var leftExpr = ParseExpression() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected expression due to mathematical operator.")
            .Build());
        var @operator = Peek() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("unexpected EOF during parsing a math expression")
            .WithNote("finish the expression.")
            .Build());

        var rightExpr = ParseExpression() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected expression due to mathematical operator.")
            .WithNote($"due to operator \"{@operator.Location}\", an expression was expected.")
            .Build());

        return @operator.Kind switch
        {
            TokenKind.Plus => new AdditionExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Minus => new SubtractionExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Slash => new DivisionExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Star => new DivisionExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Modulo => new ModuloExpression(leftExpr, rightExpr, @operator.Location),
            _ => throw ParseError(GetErrorBuilder()
                .WithMessage("unrecognized operator.")
                .Build())
        };
    }

    public Expression ParseFunctionCall()
    {
        // The "!" here is because the caller should
        // not have called this function if there is no
        // identifier here.
        var identifierToken = PeekThenAdvance()!;
        // Again, the caller should have checked for this.
        var leftParenToken = PeekThenAdvance()!;

        {
            var possiblyRightParen = Peek() ?? throw ParseError(GetErrorBuilder()
                    .WithMessage("expected expression or \")\" after \"(\".")
                    .WithNote("reached EOF while parsing function call.")
                    .Build());

            if (possiblyRightParen.Kind == TokenKind.RightParen)
            {
                _ = MoveNext();
                // nice little short-circuit.
                return new FunctionCall(identifierToken.Lexeme!, null, identifierToken.Location);
            }
        } // end short-circuit.

        var arguments = new List<Expression>();

        while (!Matches(TokenKind.RightParen))
        {
            var expression = ParseExpression() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("function arguments can only be expressions.")
                .WithNote("could not parse an expression here.")
                .Build());
            var nextToken = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("expected expression or right-paren within function arguments.")
                .Build());

            if (nextToken.Kind != TokenKind.Comma)
            {
                if (nextToken.Kind is TokenKind.RightParen)
                {
                    _ = MoveNext();
                    break;
                }

                throw ParseError(GetErrorBuilder()
                    .WithMessage($"expected a comma after this expression.")
                    .WithNote("because there is another argument, you must add a comma to seperate them.")
                    .Build());
            }

            arguments.Add(expression);
        }

        return new FunctionCall(identifierToken.Lexeme!, arguments, identifierToken.Location);
    }

    public Expression ParseDotNotation()
    {
        // We expect the current token to be an identifier followed by
        // a dot.

        var firstIdentifier = Peek()!;
        var identifiers = new List<string>() { firstIdentifier.Lexeme! };
        _ = MoveNext();

        while (Matches(TokenKind.Dot))
        {
            var nextIdentifier = Peek() ?? throw ParseError(GetErrorBuilder()
                    .WithMessage("expected an identifier after \".\"")
                    .WithNote("did you forget to reference something?")
                    .Build());
            _ = MoveNext();
            if (nextIdentifier.Kind != TokenKind.Identifier)
            {
                throw ParseError(GetErrorBuilder()
                    .WithMessage($"expected an identifier, but got \"{nextIdentifier.Lexeme}\"")
                    .Build());
            }
            identifiers.Add(nextIdentifier.Lexeme!);
        }

        return new VariableDotNotation(identifiers, firstIdentifier.Location);
    }

    public Expression ParseNumber()
    {
        var current = Peek()!;
        var number = current.Lexeme!;

        // This function ONLY handles a SINGLE number.
        // No further expressions here, just parse the number.

        if (number.Contains('f'))
        {
            // remove the "f", do a double.TryParse()
            // and return a FloatingPointNumber
            var numberWithoutFCharacter = number.Replace("f", string.Empty);
            if (!double.TryParse(numberWithoutFCharacter, out double valueWhenDouble))
            {
                throw ParseError(GetErrorBuilder()
                    .WithMessage("invalid float literal.")
                    .WithNote($"failed to parse simplified float \"{numberWithoutFCharacter}\".")
                    .Build());
            }

            _ = MoveNext();
            return new FloatingPointNumber((float)valueWhenDouble, current.Location);
        }

        if (int.TryParse(number, out int valueWhenInt))
        {
            _ = MoveNext();
            return new Number32Bit(valueWhenInt, valueWhenInt > -1, current.Location);
        }

        if (long.TryParse(number, out long valueWhenLong))
        {
            _ = MoveNext();
            return new Number64Bit(valueWhenLong, valueWhenLong > -1, current.Location);
        }

        throw ParseError(GetErrorBuilder()
            .WithMessage("invalid number literal.")
            .WithNote($"the literal \"[red italic]{number}[/]\" is not recognized as")
            .WithNote("any of the following types: (u32, i32, u64, i64, f64, f32)")
            .Build());
    }

    public Expression? ParsePrimaryExpression()
    {
        if (Matches(TokenKind.Number))
        {
            return ParseNumber();
        }

        if (Matches(TokenKind.StringLiteral))
        {
            var stringLiteral = Peek()!;
            _ = MoveNext();
            return new StringLiteral(stringLiteral.Lexeme!, stringLiteral.Location);
        }

        if (Matches(TokenKind.Identifier))
        {
            // If matches succeeds, peek() is not null.
            var identifier = Peek()!;
            // short circuit, no need to add a new function for something
            // this simple.
            _ = MoveNext();
            return new CopiedVariable(identifier.Lexeme!, identifier.Location);
        }

        return null;
    }

    public Assignment ParseAssignment()
    {
        _ = Expect(TokenKind.Let, GetErrorBuilder()
            .WithMessage("expected \"let\" keyword.")
            .WithSourceLocation(GetCurrentSourceLocation())
            .Build());

        Token? mutKeyword = null;
        if (Matches(TokenKind.Mut))
        {
            // This never fails.
            mutKeyword = Expect(TokenKind.Mut, GetErrorBuilder()
                .Build());
        }

        var previousKeyword = mutKeyword is null ? "let" : "mut";

        var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
            .WithMessage($"expected an identifier after \"{previousKeyword}\".")
            .WithNote("an identifier is anything A-z-0-9 including underscores.")
            .Build());

        TypeInformation? specifiedType = null;
        if (Matches(TokenKind.Colon))
        {
            _ = MoveNext();
            specifiedType = ParseTypename();
        }

        Expression? expression = null;

        if (Matches(TokenKind.Equals))
        {
            _ = MoveNext();
            expression = ParseExpression();
        }

        return new Assignment(
            identifier.Lexeme!,
            mutKeyword != null,
            specifiedType,
            expression,
            identifier.Location);
    }

    public Block ParseBlock()
    {
        var shouldBeleftBrace = Peek() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("unexpected EOF while parsing block.")
            .Build());

        if (shouldBeleftBrace.Kind != TokenKind.LeftBrace)
        {
            throw ParseError(GetErrorBuilder()
                .WithMessage("expected left brace to begin block.")
                .Build());
        }

        _ = MoveNext();

        var nodes = new List<AstNode>();

        while (true)
        {
            var nextToken = Peek() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("expected a right brace to close block.")
                .WithNote("encountered EOF while parsing a block.")
                .Build());

            if (nextToken.Kind == TokenKind.RightBrace)
            {
                _ = MoveNext();
                break;
            }

            nodes.Add(ParseStatement());
        }

        if (nodes.Count == 0)
        {
            ParseWarning(GetWarningBuilder()
                .WithMessage("this is an empty block, it will do nothing.")
                .WithNote("consider removing this unless it's just a placeholder for now.")
                .Build());
        }

        return new Block(nodes, shouldBeleftBrace.Location);
    }

    public FunctionDeclarationParameter ParseFunctionDeclarationParameter() 
    {
        var identifier = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected identifier for function parameter.")
            .Build());

        _ = Expect(TokenKind.Colon, GetErrorBuilder()
            .WithMessage("expected \":\" after function parameter identifier.")
            .Build());

        var type = ParseTypename();

        return new FunctionDeclarationParameter(identifier.Lexeme!, type, identifier.Location);
    }

    public FunctionDeclaration ParseFunctionDeclaration()
    {
        var fnKeyword = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected \"fn\" keyword but encountered EOF.")
            .Build());

        var identifier = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected identifier after \"fn\" keyword.")
            .WithNote("got EOF after function declarator keyword.")
            .Build());

        if (identifier.Kind != TokenKind.Identifier)
        {
            throw ParseError(GetErrorBuilder()
                .WithMessage("expected identifier after \"fn\" keyword.")
                .WithNote("example: fn identifier() {}")
                .Build());
        }

        var generics = new List<TypeInformation>();
        if (Matches(TokenKind.Lt))
        {
            _ = MoveNext();
            while (!Matches(TokenKind.Gt))
            {
                var generic = ParseTypename();
                generics.Add(generic);
                if (Matches(TokenKind.Comma))
                {
                    _ = MoveNext();
                }
            }
            _ = Expect(TokenKind.Gt, GetErrorBuilder()
                .WithMessage("expected \">\" after generic parameters.")
                .Build());
        }

        _ = Expect(TokenKind.LeftParen, GetErrorBuilder()
            .WithMessage("expected \"(\" after function name.")
            .Build());

        var parameters = new List<FunctionDeclarationParameter>();
        while (!Matches(TokenKind.RightParen))
        {
            var parameter = ParseFunctionDeclarationParameter();
            parameters.Add(parameter);
            if (Matches(TokenKind.Comma))
            {
                _ = MoveNext();
            }
        }
        _ = Expect(TokenKind.RightParen, GetErrorBuilder()
            .WithMessage("expected \")\" after function parameters.")
            .Build());

        TypeInformation? returnType = null;

        if (Matches(TokenKind.Arrow))
        {
            _ = Expect(TokenKind.Arrow, GetErrorBuilder()
                .WithMessage("expected \"->\" after function parameters.")
                .Build());

            returnType = ParseTypename();
        }

        var block = ParseBlock();

        return new FunctionDeclaration(identifier.Lexeme!, generics, parameters, returnType, block, identifier.Location);
    }

    public ReturnStatement ParseReturnStatement()
    {
        var retKeyword = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected return keyword")
            .WithNote("got EOF while parsing return statement.")
            .Build());

        var expression = ParseExpression();

        return new ReturnStatement(expression, retKeyword.Location);
    }

    public Statement ParseStatement()
    {
        if (Matches(TokenKind.Let))
        {
            return ParseAssignment();
        }

        if (Matches(TokenKind.Return))
        {
            return ParseReturnStatement();
        }

        if (Matches(TokenKind.Eof))
        {
            throw new ParserDoneException();
        }

        ParseError(GetErrorBuilder()
            .WithMessage($"The token type \"{Peek()?.Kind}\" was unexpected here.")
            .WithNote("This is either invalid syntax, or the token kind is not handled yet.")
            .Build());

        return null!;
    }

    public AstNode ParseSomething()
    {
        if (Matches(TokenKind.Fn))
        {
            return ParseFunctionDeclaration();
        }

        if (Matches(TokenKind.Eof))
        {
            throw new ParserDoneException();
        }

        return ParseStatement();
    }

    public List<AstNode> ParseTokens()
    {
        while (true)
        {
            AstNode nextNode;

            try
            {
                nextNode = ParseSomething();
            }
            catch (ParserDoneException)
            {
                break;
            }

            _ast.Add(nextNode);
        }

        return _ast;
    }

    private SourceLocation GetCurrentSourceLocation()
    {
        if (Peek() is null)
        {
            if (_tokens.Count != 1)
            {
                return _tokens[_position - 1].Location;
            }
            throw new InvalidDataException("cannot get a source location.");
        }
        return Peek()!.Location;
    }

    private Token Expect(TokenKind kind, ErrorMessage message)
    {
        if (_position >= _tokens.Count)
        {
            ParseError(GetErrorBuilder()
                .WithMessage("unexpected eof.")
                .WithSourceLocation(_tokens[_position - 1].Location)
                .Build());
            return null!;
        }

        var next = Peek()!;
        if (next.Kind != kind)
        {
            ParseError(message);
            return null!;
        }

        _ = MoveNext();
        return next;
    }
    private bool Matches(TokenKind kind)
    {
        if (Peek() is not null)
            return Peek()!.Kind == kind;
        return false;
    }

    /// <summary>
    /// Creates a parser error then exits if asked to.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="exitNow">Should we exit on this error?</param>
    /// <returns>An exception for control flow.</returns>
    private Exception ParseError(ErrorMessage message, bool exitNow = true)
    {
        ErrorHandler.QueueNow(message);
        
        if (exitNow)
            ErrorHandler.DisplayThenExitIfAny();

        return new Exception();
    }
    private void ParseWarning(WarningMessage message)
    {
        ErrorHandler.DisplayWarning(message);
    }

    private void MoveForwardBy(int count)
    {
        var wantedPosition = _position + count;
        if (wantedPosition >= _tokens.Count)
        {
            throw new ArgumentException("cannot set position past token count.");
        }
        _position += count;
    }

    private Token? PeekThenAdvance()
    {
        if (_position >= _tokens.Count)
        { return null; }

        var token = _tokens[_position];
        MoveNext();
        return token;
    }

    private Token MoveNext()
    {
        if (++_position < _tokens.Count)
        {
            return _tokens[_position];
        }
        var error = GetErrorBuilder()
            .WithMessage("expected another token, but reached EOF.")
            .WithSourceLocation(_tokens[_position - 2].Location)
            .WithNote("Add something after the previous token.")
            .Build();
        ErrorHandler.QueueNow(error);
        // Always exits.
        ErrorHandler.DisplayThenExitIfAny();
        return null!;
    }
    private ErrorBuilder GetErrorBuilder()
    {
        return new ErrorBuilder(_source).WithSourceLocation(GetCurrentSourceLocation());
    }
    private WarningBuilder GetWarningBuilder()
    {
        return new WarningBuilder(_source).WithSourceLocation(GetCurrentSourceLocation());
    }
    /// <summary>
    /// Returns the current token, if there is one.
    /// </summary>
    /// <returns></returns>
    private Token? Peek()
    {
        if (_tokens.Count <= _position)
            return null;
        return _tokens[_position];
    }
    private Token? PeekNext()
    {
        if (_position + 1 < _tokens.Count)
        {
            return _tokens[_position + 1];
        }

        return null;
    }
}
