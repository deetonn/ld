
using Language.Api;
using Language.ErrorHandling;
using Language.Lexing;
using Language.Parsing.Productions;
using Language.Parsing.Productions.Conditional;
using Language.Parsing.Productions.Debugging;
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
            .WithCode(LdErrorCode.ExpectedIdentifier)
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
                        .WithCode(LdErrorCode.ExpectedComma)
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
        TokenKind.Modulo, TokenKind.Ampersand,
        TokenKind.Pipe,
    };

    private readonly TokenKind[] _conditionalOperators = new TokenKind[]
    {
        TokenKind.EqualEqual, TokenKind.NotEqual,
        TokenKind.Gt, TokenKind.Lt,
        TokenKind.GreaterEquals, TokenKind.LesserEquals,
        TokenKind.And, TokenKind.Or,
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

        if (current.Kind == TokenKind.Bang)
        {
            _ = MoveNext();
            var expr = ParseExpression() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("expected expression after bang (\"!\") operator.")
                .WithCode(LdErrorCode.ExpectedExpression)
                .WithNote("the bang operator inverts a boolean value, there must be an expression to invert.")
                .Build());
            return new NotExpression(expr, current.Location);
        }

        // If the next token is a mathematical operator, this
        // is a mathematical expression.
        if (_mathTokens.Contains(next.Kind))
        {
            return ParseMathExpression();
        }

        if (_conditionalOperators.Contains(next.Kind))
        {
            return ParseConditional();
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

    public Expression ParseConditional()
    {
        var opString = PeekNext()?.Kind.ToString() ?? "unknown-op";

        // NOTE: for now just handle basic conditional operations.
        var leftExpr = ParsePrimaryExpression() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected expression on the left of conditional operator.")
            .WithCode(LdErrorCode.ExpectedExpression)
            .WithNote($"the operator \"{opString}\" takes one argument on the left and another on the right.")
            .Build());

        var @operator = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected operand left of expression.")
            .WithCode(LdErrorCode.ExpectedExpression)
            .WithNote("expected an operator here.")
            .Build());

        var rightExpr = ParsePrimaryExpression() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected expression right of operator \"{opString}\".")
            .WithCode(LdErrorCode.ExpectedExpression)
            .Build());

        return @operator.Kind switch
        {
            TokenKind.EqualEqual => new IsEqualToExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.NotEqual => new IsNotEqualToExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Gt => new IsGreaterThanExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Lt => new IsLesserThanExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.GreaterEquals => new IsGreaterEqualToExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.LesserEquals => new IsLesserEqualToExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.And => new AndExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Or => new OrExpression(leftExpr, rightExpr, @operator.Location),
            _ => throw ParseError(GetErrorBuilder()
            .WithMessage($"unknown operand type \"{@operator.Kind}\".")
            .WithCode(LdErrorCode.UnknownOperand)
            .WithNote($"supported operands are: [[{string.Join(", ", _conditionalOperators.Select(x => x.ToString()))}]]")
            .Build())
        };
    }

    public Expression ParseMathExpression()
    {
        // NOTE: math expressions just boil down to
        //       groupings always. This keeps code
        //       evaluating things consistent with precidence.

        // NOTE[2]: Actual groupings are not supported yet.

        var leftExpr = ParsePrimaryExpression() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected expression due to mathematical operator.")
            .WithCode(LdErrorCode.ExpectedExpressionReasonMathematical)
            .Build());
        var @operator = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("unexpected EOF during parsing a math expression")
            .WithNote("finish the expression.")
            .WithCode(LdErrorCode.UnexpectedEOF)
            .Build());

        var rightExpr = ParsePrimaryExpression() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected expression due to mathematical operator.")
            .WithNote($"due to operator \"{@operator.Kind}\", an expression was expected.")
            .WithCode(LdErrorCode.ExpectedExpressionReasonMathematical)
            .Build());

        return @operator.Kind switch
        {
            TokenKind.Plus => new AdditionExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Minus => new SubtractionExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Slash => new DivisionExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Star => new DivisionExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Modulo => new ModuloExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Ampersand => new BitwiseAndExpression(leftExpr, rightExpr, @operator.Location),
            TokenKind.Pipe => new BitwiseOrExpression(leftExpr, rightExpr, @operator.Location),
            _ => throw ParseError(GetErrorBuilder()
                .WithMessage("unrecognized operator.")
                .WithNote($"The operator \"{@operator.Kind}\" is unknown in this context.")
                .WithCode(LdErrorCode.UnrecognizedOperator)
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
                    .WithCode(LdErrorCode.UnexpectedEOF)
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
                .WithCode(LdErrorCode.ArgumentsCanOnlyBeExpressions)
                .Build());
            var nextToken = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("expected expression or right-paren within function arguments.")
                .WithCode(LdErrorCode.ExpectedExpressionOrRightParen)
                .Build());

            if (nextToken.Kind != TokenKind.Comma)
            {
                if (nextToken.Kind is TokenKind.RightParen)
                {
                    break;
                }

                throw ParseError(GetErrorBuilder()
                    .WithMessage($"expected a comma after this expression.")
                    .WithNote("because there is another argument, you must add a comma to seperate them.")
                    .WithCode(LdErrorCode.ExpectedComma)
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
                    .WithCode(LdErrorCode.ExpectedIdentifier)
                    .Build());
            _ = MoveNext();
            if (nextIdentifier.Kind != TokenKind.Identifier)
            {
                throw ParseError(GetErrorBuilder()
                    .WithMessage($"expected an identifier, but got \"{nextIdentifier.Kind}\"")
                    .WithCode(LdErrorCode.ExpectedIdentifier)
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
                    .WithCode(LdErrorCode.InvalidFloatLiteral)
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
            .WithCode(LdErrorCode.UnknownNumberLiteral)
            .Build());
    }

    public Expression? ParsePrimaryExpression()
    {
        if (Matches(TokenKind.Number))
        {
            return ParseNumber();
        }

        if (Matches(TokenKind.OpenBracket))
        {
            return ParseArrayLiteralExpression();
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
            var next = PeekNext();

            if (next is not null && next.Kind == TokenKind.LeftParen)
            {
                // TODO: Currently function calls on the left
                //       of any operator cause ParseExpression
                //       to fail.
                return ParseFunctionCall();
            }

            _ = MoveNext();
            return new CopiedVariable(identifier.Lexeme!, identifier.Location);
        }

        if (Matches(TokenKind.True))
        {
            _ = MoveNext();
            return new BooleanExpression(true, Peek()!.Location);
        }

        if (Matches(TokenKind.False))
        {
            _ = MoveNext();
            return new BooleanExpression(false, Peek()!.Location);
        }

        return null;
    }

    public ArrayLiteralExpression ParseArrayLiteralExpression()
    {
        var leftBracket = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected \"[\" to begin array literal.")
            .WithCode(LdErrorCode.ExpectedLeftBracket)
            .WithNote("array syntax is like: [1, 2, 3, 4]")
            .Build());

        var expressions = new List<Expression>();

        while (!Matches(TokenKind.CloseBracket))
        {
            var expression = ParseExpression() ?? throw ParseError(GetErrorBuilder()
                 .WithMessage("expected expression within array literal.")
                 .WithCode(LdErrorCode.ExpectedExpression)
                 .Build());

            expressions.Add(expression);

            if (Matches(TokenKind.Comma))
            {
                _ = MoveNext();
            }
            else if (!Matches(TokenKind.CloseBracket))
            {
                throw ParseError(GetErrorBuilder()
                     .WithMessage("expected comma or closing bracket.")
                     .WithCode(LdErrorCode.ExpectedCommaOrCloseBracket)
                     .Build());
            }
        }

        _ = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected \"]\" to close array literal.")
            .WithCode(LdErrorCode.ExpectedRightBracket)
            .Build());

        return new ArrayLiteralExpression(expressions, leftBracket.Location);
    }

    public Assignment ParseAssignment()
    {
        _ = Expect(TokenKind.Let, GetErrorBuilder()
            .WithMessage("expected \"let\" keyword.")
            .WithSourceLocation(GetCurrentSourceLocation())
            .WithCode(LdErrorCode.ExpectedKeyword)
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
            .WithCode(LdErrorCode.ExpectedIdentifier)
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
        var shouldBeleftBrace = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("unexpected EOF while parsing block.")
            .WithCode(LdErrorCode.UnexpectedEOF)
            .Build());

        if (shouldBeleftBrace.Kind != TokenKind.LeftBrace)
        {
            throw ParseError(GetErrorBuilder()
                .WithMessage("expected left brace to begin block.")
                .WithCode(LdErrorCode.ExpectedLeftBrace)
                .Build());
        }

        var nodes = new List<AstNode>();

        while (true)
        {
            var nextToken = Peek() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("expected a right brace to close block.")
                .WithNote("encountered EOF while parsing a block.")
                .WithCode(LdErrorCode.UnexpectedEOF)
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
            .WithCode(LdErrorCode.ExpectedIdentifier)
            .Build());

        _ = Expect(TokenKind.Colon, GetErrorBuilder()
            .WithMessage("expected \":\" after function parameter identifier.")
            .WithCode(LdErrorCode.ExpectedColonFnArgs)
            .Build());

        var type = ParseTypename();

        return new FunctionDeclarationParameter(identifier.Lexeme!, type, identifier.Location);
    }

    public FunctionDeclaration ParseFunctionDeclaration()
    {
        var fnKeyword = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected \"fn\" keyword but encountered EOF.")
            .WithCode(LdErrorCode.UnexpectedEOF)
            .Build());

        var identifier = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected identifier after \"fn\" keyword.")
            .WithNote("got EOF after function declarator keyword.")
            .WithCode(LdErrorCode.UnexpectedEOF)
            .Build());

        if (identifier.Kind != TokenKind.Identifier)
        {
            throw ParseError(GetErrorBuilder()
                .WithMessage("expected identifier after \"fn\" keyword.")
                .WithNote("example: fn identifier() {}")
                .WithCode(LdErrorCode.ExpectedIdentifier)
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
                .WithCode(LdErrorCode.ExpectedClosingGenericCroc)
                .WithNote("generic parameters must close with a left angle bracket.")
                .Build());
        }

        _ = Expect(TokenKind.LeftParen, GetErrorBuilder()
            .WithMessage("expected \"(\" after function name.")
            .WithCode(LdErrorCode.ExpectedLeftParen)
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
            .WithCode(LdErrorCode.ExpectedRightParen)
            .Build());

        TypeInformation? returnType = null;

        if (Matches(TokenKind.Arrow))
        {
            _ = Expect(TokenKind.Arrow, GetErrorBuilder()
                .WithMessage("expected \"->\" after function parameters.")
                .WithCode(LdErrorCode.ExpectedArrow)
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
            .WithCode(LdErrorCode.UnexpectedEOF)
            .Build());

        var expression = ParseExpression();

        return new ReturnStatement(expression, retKeyword.Location);
    }

    public IfStatement ParseIfStatement()
    {
        var ifKeyword = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected \"if\" keyword but got EOF.")
            .WithCode(LdErrorCode.UnexpectedEOF)
            .Build());

        var expression = ParseExpression() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected expression due to previous \"if\" keyword.")
            .WithCode(LdErrorCode.ExpectedExpression)
            .WithNote("there is nothing to evaluate")
            .Build());

        var ifBlock = ParseBlock() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected block after if statement.")
            .WithNote("there is no point of the \"if\" without conditional code.")
            .WithCode(LdErrorCode.ExpectedBlockAfterIf)
            .Build());

        Block? elseBlock = null;
        if (Matches(TokenKind.Else))
        {
            // NOTE: because of the previous match, this never fails.
            //       that is why the error message is so bleak.
            var _ = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("expected else keyword.")
                .WithCode(LdErrorCode.ExpectedKeyword)
                .Build());

            elseBlock = ParseBlock() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("expected block after \"else\" keyword")
                .WithCode(LdErrorCode.ExpectedBlockAfterElse)
                .WithNote("There is no point of an else statement without a block of conditional code.")
                .Build());
        }

        return new IfStatement(expression, ifBlock, elseBlock, ifKeyword.Location);
    }

    public Statement ParseStatement()
    {
        if (Matches(TokenKind.DebugBreak))
        {
            _ = new ParserDebugBreakExpression();
            _ = MoveNext();
        }

        if (Matches(TokenKind.Let))
        {
            return ParseAssignment();
        }

        if (Matches(TokenKind.Return))
        {
            return ParseReturnStatement();
        }

        if (Matches(TokenKind.If))
        {
            return ParseIfStatement();
        }

        if (Matches(TokenKind.Eof))
        {
            throw new ParserDoneException();
        }

        ParseError(GetErrorBuilder()
            .WithMessage($"The token type \"{Peek()?.Kind}\" was unexpected here.")
            .WithNote("This is either invalid syntax, or the token kind is not handled yet.")
            .WithCode(LdErrorCode.UnexpectedToken)
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
                .WithCode(LdErrorCode.UnexpectedEOF)
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
        // just return the last token so EOF can be handled.
        return _tokens[^1];
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
