
using Language.Api;
using Language.ErrorHandling;
using Language.Extensions;
using Language.Lexing;
using Language.Parsing.Productions;
using Language.Parsing.Productions.Conditional;
using Language.Parsing.Productions.Debugging;
using Language.Parsing.Productions.Literals;
using Language.Parsing.Productions.Math;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Language.Parsing;

public class ParserDoneException : Exception { }

public class Parser
{
    private readonly List<Token> _tokens;
    private readonly HashSet<AstNode> _ast;
    private int _position;
    private readonly string _source;
    private readonly HashSet<string> _enumNames;
    private readonly HashSet<string> _structNames;
    private readonly ProjectDetails _project;
    private readonly string _fileName;

    public Parser(string fileName, ProjectDetails project, List<Token> tokens, string source)
    {
        _fileName = fileName;
        _project = project;
        _tokens = tokens;
        _position = 0;
        _ast = new HashSet<AstNode>();
        _source = source;
        _enumNames = new HashSet<string>();
        _structNames = new HashSet<string>();
    }

    public void RememberEnumDeclaration(string name) 
    {
        _enumNames.Add(name);
    }
    public void RememberStructDeclaration(string name)
    {
        _structNames.Add(name);
    }

    public bool EnumExists(string name)
    {
        return _enumNames.Contains(name);
    }
    public bool StructExists(string name)
    {
        return _structNames.Contains(name);
    }

    /// <summary>
    /// This function will parse a typename. This includes generic parameters.
    /// Example input: "Option&lt;T, E>".
    /// All of the possible error scenarios have been tested for this function.
    /// </summary>
    /// <returns>The type information. If the parsing fails, an error will happen.</returns>
    public TypeInformation ParseTypename()
    {
        var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
            .WithMessage("expected an identifier. (while parsing type)")
            .WithNote("specify a typename.")
            .WithNote($"expected identifier, got {Peek()!.Kind}")
            .WithCode(LdErrorCode.ExpectedIdentifier)
            .Build());

        var typeInfo = new TypeInformation(identifier.Lexeme!);

        if (Matches(TokenKind.Lt))
        {
            _ = MoveNext(); // Skip the "<"
            typeInfo.Generics = new List<TypeInformation>();

            while (!Matches(TokenKind.Gt))
            {
                var genericParam = ParseTypename();
                var nextToken = Peek() ?? throw ParseError(GetErrorBuilder()
                    .WithMessage("unexpected EOF while parsing generic parameters.")
                    .WithCode(LdErrorCode.UnexpectedEOF)
                    .Build());

                typeInfo.Generics.Add(genericParam);

                if (nextToken is not null && nextToken.Kind == TokenKind.Comma)
                {
                    _ = MoveNext();
                    continue;
                }
                else if (nextToken is not null && nextToken.Kind == TokenKind.Gt)
                {
                    _ = MoveNext();
                    break;
                }
                else
                {
                    throw ParseError(GetErrorBuilder()
                        .WithMessage("expected comma seperating generic parameter.")
                        .WithNote($"add a comma after \"{genericParam.Name}\"")
                        .WithCode(LdErrorCode.ExpectedComma)
                        .Build());
                }
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

    /// <summary>
    /// Any tokens that are conditional operators.
    /// </summary>
    private readonly TokenKind[] _conditionalOperators = new TokenKind[]
    {
        TokenKind.EqualEqual, TokenKind.NotEqual,
        TokenKind.Gt, TokenKind.Lt,
        TokenKind.GreaterEquals, TokenKind.LesserEquals,
        TokenKind.And, TokenKind.Or,
    };

    /// <summary>
    /// This function will parse an inline struct initialization expression.
    /// Example input: "StructName { field0: 20, field2: 32 }".
    /// The errors in this function have been stress tested.
    /// </summary>
    /// <returns></returns>
    public StructInitializationExpression ParseInlineStructInitialization()
    {
        var ident = Expect(TokenKind.Identifier, GetErrorBuilder()
            .WithMessage("expected identifier")
            .WithCode(LdErrorCode.ExpectedIdentifier)
            .Build());

        // NOTE: this error is very unlikely to happen due to the fact 
        //       that the only way to known this needs to be parsed is
        //       if this curly is here.
        var _ = Expect(TokenKind.LeftBrace, GetErrorBuilder()
            .WithMessage("expected left brace for struct initialization.")
            .WithCode(LdErrorCode.ExpectedLeftBrace)
            .Build());

        var nextPeeked = Peek();

        if (nextPeeked is not null && nextPeeked.Kind is TokenKind.RightBrace)
        {
            // short-circuit.
            return new StructInitializationExpression(ident.Lexeme!, null, ident.Location);
        }

        var initializer = new List<InlineStructInitializationParameter>();
        while (!Matches(TokenKind.RightBrace))
        {
            // NOTE: This error is very unlikely to happen, this is because
            //       just above there is a short-circuit above that returns
            //       an empty initializer if this doesn't exist.
            var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
                .WithMessage("expected identifier for struct-field initializer.")
                .WithCode(LdErrorCode.ExpectedIdentifier)
                .WithNote("syntax is like: field: <expr>")
                .Build());

            _ = Expect(TokenKind.Colon, GetErrorBuilder()
                .WithMessage("expected colon after identifier.")
                .WithCode(LdErrorCode.ExpectedColon)
                .WithNote("the colon is there to seperate the name from the expression.")
                .WithNote($"example: {identifier.Lexeme}: 1")
                .Build());

            var expression = ParseExpression() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("expected expression after colon.")
                .WithCode(LdErrorCode.ExpectedExpression)
                .WithNote($"after the colon should be a value to initialize \"{identifier.Lexeme}\" to.")
                .WithNote($"example: {identifier.Lexeme}: 69")
                .Build());

            initializer.Add(
                new InlineStructInitializationParameter(identifier.Lexeme!, expression, identifier.Location)
            );

            nextPeeked = Peek();

            if (nextPeeked is not null && nextPeeked.Kind is TokenKind.Comma)
            {
                _ = MoveNext(); // skip comma
                continue;
            }
            else if (nextPeeked is not null && nextPeeked.Kind is TokenKind.RightBrace)
                break;
            else
            {
                throw ParseError(GetErrorBuilder()
                    .WithMessage("unknown token during inline struct initialization.")
                    .WithCode(LdErrorCode.UnexpectedToken)
                    .WithNote($"expected comma or right brace, but got \"{nextPeeked?.Kind}\"")
                    .Build());
            }
        }

        _ = MoveNext(); // skip right brace.

        return new StructInitializationExpression(ident.Lexeme!, initializer, ident.Location);
    }

    /// <summary>
    /// This function handles all expressions. This is regardless of type,
    /// function calls, literals, math, etc... all handled in here.
    /// </summary>
    /// <returns>An expression if there was one, otherwise null.</returns>
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

        if (current.Kind == TokenKind.Identifier &&
            next.Kind == TokenKind.LeftBrace)
        {
            return ParseInlineStructInitialization();
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

    /// <summary>
    /// This function will parse a conditional expression.
    /// Example input: 243 == functionCall()
    /// The errors in this function have been stress tested.
    /// </summary>
    /// <returns>The expression, will do a parse error if no conditional exists</returns>
    public Expression ParseConditional()
    {
        var opString = PeekNext()?.Kind.ToString() ?? "unknown-op";

        // NOTE: for now just handle basic conditional operations.
        // NOTE[2]: This error is very unlikely to happen. This is because
        //          ParseExpression uses this to know to call into this
        //          function.
        var leftExpr = ParsePrimaryExpression() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected expression on the left of conditional operator.")
            .WithCode(LdErrorCode.ExpectedExpression)
            .WithNote($"the operator \"{opString}\" takes one argument on the left and another on the right.")
            .Build());

        // NOTE: This error is VERY (maybe impossible) to happen. This is
        //       because ParseExpression uses this operator to decide if it
        //       should call ParseConditional().
        var @operator = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected operand left of expression.")
            .WithCode(LdErrorCode.ExpectedExpression)
            .WithNote("expected an operator here.")
            .Build());

        // NOTE: This error catches a missing expression.
        var rightExpr = ParsePrimaryExpression() ?? throw ParseError(GetErrorBuilder()
            .WithMessage($"expected expression right of operator \"{opString}\".")
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
            // NOTE: This error will only happen when an operator has been added to
            ///      <see cref="_conditionalOperators"/>, but it isn't handled here.
            _ => throw ParseError(GetErrorBuilder()
                .WithMessage($"unknown operand type \"{@operator.Kind}\".")
                .WithCode(LdErrorCode.UnknownOperand)
                .WithNote($"supported operands are: [[{string.Join(", ", _conditionalOperators.Select(x => x.ToString()))}]]")
                .Build())
        };
    }

    /// <summary>
    /// This will parse a basic mathematical expression.
    /// Example input: 19 + 248472
    /// </summary>
    /// <returns></returns>
    public Expression ParseMathExpression()
    {
        // NOTE: math expressions just boil down to
        //       groupings always. This keeps code
        //       evaluating things consistent with precidence.

        // NOTE[2]: Actual groupings are not supported yet.

        // NOTE[3]: This error is very unlikely to happen. This is because
        //          ParseExpression uses this to know to call into this
        //          function.
        var leftExpr = ParsePrimaryExpression() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected expression due to mathematical operator.")
            .WithCode(LdErrorCode.ExpectedExpressionReasonMathematical)
            .Build());

        // NOTE: This error is very unlikely to happen. This is because
        //       ParseExpression uses this to know to call into this
        //       function.
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
            // NOTE: This error will only happen when a new TokenKind has been added to
            ///      <see cref="_mathTokens"/>, and it hasn't been implemented here.
            _ => throw ParseError(GetErrorBuilder()
                .WithMessage("unrecognized operator.")
                .WithNote($"The operator \"{@operator.Kind}\" is unknown in this context.")
                .WithCode(LdErrorCode.UnrecognizedOperator)
                .Build())
        };
    }

    /// <summary>
    /// Parse a function call. This is just an expression.
    /// Example input: myFunction(expr1, ..., exprN)
    /// </summary>
    /// <returns>The function call. This function will not return null, a parse error will happen if anything goes wrong.</returns>
    public Expression ParseFunctionCall()
    {
        // The "!" here is because the caller should
        // not have called this function if there is no
        // identifier here.
        var identifierToken = PeekThenAdvance()!;
        // Again, the caller should have checked for this.
        _ = PeekThenAdvance()!;

        {
            // FIXME: This error is not reached with this basic example:
            // "let data = func("
            var possiblyRightParen = Peek() ?? throw ParseError(GetErrorBuilder()
                    .WithMessage("expected expression or \")\" after \"(\".")
                    .WithNote("reached EOF while parsing function call.")
                    .WithCode(LdErrorCode.UnexpectedEOF)
                    .Build());

            if (possiblyRightParen.Kind == TokenKind.RightParen)
            {
                _ = MoveNext();
                // nice little short-circuit.
                return new FunctionCall(identifierToken.Lexeme!, null, null, identifierToken.Location);
            }
        } // end short-circuit.

        var arguments = new List<Expression>();

        while (!Matches(TokenKind.RightParen))
        {
            var expression = ParseExpression() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("function arguments can only be expressions.")
                .WithNote("this is not an expression")
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
                if (nextToken.Kind is TokenKind.Eof)
                {
                    throw ParseError(GetErrorBuilder()
                        .WithMessage("expected an expression or \")\" after function argument")
                        .WithCode(LdErrorCode.UnexpectedEOF)
                        .WithNote("add \")\" after the previous expression, or add more.")
                        .Build());
                }

                throw ParseError(GetErrorBuilder()
                    .WithMessage($"expected a comma after this expression.")
                    .WithNote("because there is another argument, you must add a comma to seperate them.")
                    .WithCode(LdErrorCode.ExpectedComma)
                    .Build());
            }

            arguments.Add(expression);
        }

        return new FunctionCall(identifierToken.Lexeme!, null, arguments, identifierToken.Location);
    }

    /// <summary>
    /// Parses dot notation. This is any expression that is using dots
    /// to annotate itself accessing another variables fields.
    /// </summary>
    /// <returns>The expression, this function will use ParseError</returns>
    public Expression ParseDotNotation()
    {
        // We expect the current token to be an identifier followed by
        // a dot.

        var firstIdentifier = Peek()!;
        var identifiers = new List<string>() { firstIdentifier.Lexeme! };
        _ = MoveNext();

        while (Matches(TokenKind.Dot))
        {
            _ = MoveNext();

            var nextIdentifier = Expect(TokenKind.Identifier, GetErrorBuilder()
                    .WithMessage("expected an identifier after \".\"")
                    .WithNote("did you forget to reference something?")
                    .WithCode(LdErrorCode.ExpectedIdentifier)
                    .Build());
            identifiers.Add(nextIdentifier.Lexeme!);
        }

        return new VariableDotNotation(identifiers, firstIdentifier.Location);
    }

    /// <summary>
    /// Parses any kind of number. This will parse signed, unsigned, 
    /// 64/32bit, floating point numbers.
    /// </summary>
    /// <returns>The expression, this function uses ParseError.</returns>
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

        var extraNote = number.Length > long.MaxValue.ToString().Length
            ? "this number is too large to fit in any storage type."
            : "this number is likely an invalid format.";

        throw ParseError(GetErrorBuilder()
            .WithMessage("invalid number literal.")
            .WithNote($"the literal \"[red italic]{number}[/]\" is not recognized as")
            .WithNote("any of the following types: (u32, i32, u64, i64, f64, f32)")
            .WithNote(extraNote)
            .WithCode(LdErrorCode.UnknownNumberLiteral)
            .Build());
    }

    /// <summary>
    /// Parses an argument list. This is a comma seperated list of expressions.
    /// This function will parse expressions seperated by commas until there isn't
    /// a comma or expression.
    /// </summary>
    /// <returns>The expressions. This function returns null if there is no expression to start with.</returns>
    public List<Expression>? ParseArgumentList()
    {
        List<Expression>? expressions = null;

        while (true)
        {
            var expression = ParseExpression();
            if (expression is null && expressions is null)
                return null;
            if (expression is null)
                return expressions;
            expressions ??= new List<Expression>();
            expressions.Add(expression);
            var next = Peek();

            if (next is not null && next.Kind == TokenKind.Comma)
            {
                _ = MoveNext();
                continue;
            }
            else
            {
                break;
            }
        }

        return expressions;
    }

    public FunctionCall ParseGenericFunctionCall()
    {
        var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
            .WithMessage("expected identifier at start of generic function call")
            .WithCode(LdErrorCode.ExpectedIdentifier)
            .Build());

        var _ = Expect(TokenKind.ColonColon, GetErrorBuilder()
            .WithMessage("expected \"::\" after identifier.")
            .WithCode(LdErrorCode.ExpectedDoubleColon)
            .Build());

        _ = Expect(TokenKind.Lt, GetErrorBuilder()
            .WithMessage("expected \"<\" to start generic argument list.")
            .WithCode(LdErrorCode.ExpectedLt)
            .Build());

        var suppliedTypes = new List<TypeInformation>();
        while (true)
        {
            var typeName = ParseTypename();
            suppliedTypes.Add(typeName);
            var next = Peek();

            if (next is not null && next.Kind == TokenKind.Comma)
            {
                _ = MoveNext();
                continue;
            }
            else if (next is not null && next.Kind == TokenKind.Gt)
            {
                _ = MoveNext();
                break;
            }

            throw ParseError(GetErrorBuilder()
                .WithMessage("expected \",\" or \">\" after generic parameter.")
                .WithCode(LdErrorCode.ExpectedCommaOrCloseCroc)
                .Build());
        }

        _ = Expect(TokenKind.LeftParen, GetErrorBuilder()
            .WithMessage("expect left paren to begin function arguments.")
            .WithCode(LdErrorCode.ExpectedLeftParen)
            .Build());

        var argumentList = ParseArgumentList();

        _ = Expect(TokenKind.RightParen, GetErrorBuilder()
            .WithMessage("expect right paren to end function arguments.")
            .WithCode(LdErrorCode.ExpectedRightParen)
            .Build());

        return new FunctionCall(identifier.Lexeme!, suppliedTypes, argumentList, identifier.Location);
    }

    /// <summary>
    /// This will parse a primary expression. A primary is expression is any of the follwing.
    /// EnumVariantExpression (Enum::Variant(...)), StaticStructAccessExpression (Struct::function(...)),
    /// Number (any), An array literal, String literal, Identifier, Function Call,
    /// True and False.
    /// </summary>
    /// <returns>The expression, if there isn't a primary expression here null is returned.</returns>
    public Expression? ParsePrimaryExpression()
    {
        var current = Peek();
        var next = PeekNext();

        if (current is not null && current.Kind == TokenKind.Identifier
            && next is not null && next.Kind == TokenKind.ColonColon)
        {
            var possiblyStartOfGenericFunctionCall = PeekAheadBy(2);

            if (possiblyStartOfGenericFunctionCall is not null
                && possiblyStartOfGenericFunctionCall.Kind == TokenKind.Lt)
            {
                return ParseGenericFunctionCall();
            }

            var colonNotation = ParseColonNotation();

            if (colonNotation.Path.Count != 2)
            {
                return colonNotation;
            }

            // check for parens.
            current = Peek();
            if (current is not null && current.Kind == TokenKind.LeftParen)
            {
                var firstInColonNotation = colonNotation.Path.First();
                if (EnumExists(firstInColonNotation))
                {
                    _ = MoveNext();
                    var argumentList = ParseArgumentList();
                    _ = Expect(TokenKind.RightParen, GetErrorBuilder()
                        .WithMessage("expected right paren after enum argument list.")
                        .WithCode(LdErrorCode.ExpectedRightParen)
                        .Build());

                    return new EnumVariantExpression(colonNotation.Path[0],
                        colonNotation.Path[1], argumentList, colonNotation.Location);
                }

                if (StructExists(firstInColonNotation))
                {
                    _ = MoveNext();
                    var argumentList = ParseArgumentList();
                    _ = Expect(TokenKind.RightParen, GetErrorBuilder()
                        .WithMessage("expected right paren after function argument list.")
                        .WithCode(LdErrorCode.ExpectedRightParen)
                        .Build());

                    return new StaticStructFunctionCallExpression(colonNotation.Path[0],
                        colonNotation.Path[1], null, argumentList, colonNotation.Location);
                }

                throw ParseError(GetErrorBuilder()
                    .WithMessage($"colon notation is only supported on structs and enums.")
                    .WithCode(LdErrorCode.ColonNotationOnStructsAndEnums)
                    .WithNote($"\"{firstInColonNotation}\" is not an enum or struct.")
                    .Build());
            }
            // TODO: change this syntax to be
            //       Struct::function<T1, ..., TN>::(args...)
            if (current is not null && current.Kind == TokenKind.Lt)
            {
                _ = MoveNext(); // consume "<"

                var types = new List<TypeInformation>();

                while (true)
                {
                    var typeName = ParseTypename();
                    types.Add(typeName);

                    var nextInGi = Peek();

                    if (nextInGi is not null && nextInGi.Kind == TokenKind.Comma)
                    {
                        _ = MoveNext();
                        continue;
                    }
                    else if (nextInGi is not null && nextInGi.Kind == TokenKind.Gt)
                    {
                        _ = MoveNext();
                        break;
                    }
                    else
                    {
                        throw ParseError(GetErrorBuilder()
                            .WithMessage("expected comma after generic argument")
                            .WithNote($"add a comma after parameter {types.Count}")
                            .WithCode(LdErrorCode.ExpectedCommaOrCloseCroc)
                            .Build());
                    }
                }

                _ = Expect(TokenKind.ColonColon, GetErrorBuilder()
                    .WithMessage("expected \"::\" after function generic arguments")
                    .WithCode(LdErrorCode.ExpectedLeftParen)
                    .WithNote("generic struct function syntax is like this:")
                    .WithNote("Struct::function<Ts...>::(args...)")
                    .Build());

                _ = Expect(TokenKind.LeftParen, GetErrorBuilder()
                    .WithMessage("expected left paren to begin function arguments")
                    .WithCode(LdErrorCode.ExpectedLeftParen)
                    .Build());

                var argumentList = ParseArgumentList();

                _ = Expect(TokenKind.RightParen, GetErrorBuilder()
                    .WithMessage("expected a right paren to end generic function call")
                    .WithCode(LdErrorCode.ExpectedLeftParen)
                    .WithNote("add a closing \")\"")
                    .Build());

                return new StaticStructFunctionCallExpression(
                    colonNotation.Path[0],
                    colonNotation.Path[1],
                    types,
                    argumentList,
                    colonNotation.Location);
            }

            return colonNotation;
        }

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
            next = PeekNext();

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

    /// <summary>
    /// Parses an array literal expression. This is a comma seperated list
    /// of expressions within brackets.
    /// Example input: [1, 2, get_int()]
    /// </summary>
    /// <returns>The expression, if there isn't an ArrayLiteralExpression here ParseError is called.</returns>
    public ArrayLiteralExpression ParseArrayLiteralExpression()
    {
        // NOTE: This error is very unlikely to happen.
        //       This is because the caller usually checks what expression
        //       is next. (I.E ParseExpression).
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

        _ = Expect(TokenKind.CloseBracket, GetErrorBuilder()
            .WithMessage("expected \"]\" to close array literal.")
            .WithCode(LdErrorCode.ExpectedRightBracket)
            .Build());

        return new ArrayLiteralExpression(expressions, leftBracket.Location);
    }

    /// <summary>
    /// Parses a "let" statement. This includes the let keyword, mut keyword,
    /// type information and expression.
    /// </summary>
    /// <returns>The assignment. This function calles ParseError on failure.</returns>
    public Assignment ParseAssignment()
    {
        // NOTE: This never happens.
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

        // This is just for the error message to be more concise.
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

    /// <summary>
    /// Parses a block of code. This block can only contain statements
    /// and expressions. 
    /// </summary>
    /// <returns>The block, this function uses ParseError</returns>
    public Block ParseBlock()
    {
        var shouldBeleftBrace = Expect(TokenKind.LeftBrace, GetErrorBuilder()
                .WithMessage("expected left brace to begin block.")
                .WithCode(LdErrorCode.ExpectedLeftBrace)
                .Build());

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


    /// <summary>
    /// Parses a function declaration parameter. This includes the identifier, colon 
    /// and type information.
    /// </summary>
    /// <returns>The parameter, this function uses ParseError.</returns>
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

    /// <summary>
    /// Parses a function declaration. This include the fn keyword,
    /// the identifier, generic parameters, function arguments,
    /// return type and body.
    /// </summary>
    /// <returns>The declaration, this function uses ParseError</returns>
    public FunctionDeclaration ParseFunctionDeclaration()
    {
        _ = Expect(TokenKind.Fn, GetErrorBuilder()
            .WithMessage("expected \"fn\" keyword but encountered EOF.")
            .WithCode(LdErrorCode.UnexpectedEOF)
            .Build());

        var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
                .WithMessage("expected identifier after \"fn\" keyword.")
                .WithNote("example: fn identifier() {}")
                .WithCode(LdErrorCode.ExpectedIdentifier)
                .Build());

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

    /// <summary>
    /// Parses a return statement. This is the return keyword, and then
    /// an expression.
    /// </summary>
    /// <returns>The return statement, this function uses ParseError.</returns>
    public ReturnStatement ParseReturnStatement()
    {
        var retKeyword = PeekThenAdvance() ?? throw ParseError(GetErrorBuilder()
            .WithMessage("expected return keyword")
            .WithNote("got EOF while parsing return statement.")
            .WithCode(LdErrorCode.UnexpectedEOF)
            .Build());

        // NOTE: if this returns null, that fine. There is no expression
        // and that's cosher for a return statement.
        var expression = ParseExpression();

        return new ReturnStatement(expression, retKeyword.Location);
    }

    /// <summary>
    /// Parses an if statement. This is the if keyword, the condition,
    /// the success block, the else keyword and optionally the
    /// else block.
    /// </summary>
    /// <returns>The if statement, this function uses ParseError</returns>
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

    /// <summary>
    /// Parses any statement. This includes assignments, returns statements,
    /// if statements. This function will handle EOF by throwing a <see cref="ParserDoneException"/>
    /// This causes the "Parse" function to finish.
    /// DO NOT HANDLE THIS EXCEPTION.
    /// </summary>
    /// <returns>The statement, functions called from here use ParseError</returns>
    /// <exception cref="ParserDoneException"></exception>
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

        // Just handles function calls right now.
        if (Matches(TokenKind.Identifier))
        {
            var identifier = Peek()!;
            var next = PeekNext();
            // TODO: handle dot notation, colon notation aswell here.

            // NOTE: Assignment.Discard as an identifier means the interpreter should
            //       discard the assignment and just evaluate the expression.
            return new Assignment(Assignment.Discard, false, null, ParseFunctionCall(), identifier.Location);
        }

        ParseError(GetErrorBuilder()
            .WithMessage($"The token type \"{Peek()?.Kind}\" was unexpected here.")
            .WithNote("This is either invalid syntax, or the token kind is not handled yet.")
            .WithCode(LdErrorCode.UnexpectedToken)
            .Build());

        return null!;
    }

    /// <summary>
    /// This will parse a struct declaration. This includes the struct
    /// keyword, the identifier and all of it's contained fields. It also
    /// parses the braces (obviously.) 
    /// </summary>
    /// <returns>The struct declaration, this function uses ParseError.</returns>
    public StructDeclaration ParseStructDeclaration()
    {
        _ = Expect(TokenKind.Struct, GetErrorBuilder()
            .WithMessage("expected \"struct\" keyword but encountered EOF.")
            .WithCode(LdErrorCode.UnexpectedEOF)
            .Build());

        var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
                .WithMessage("expected identifier after \"struct\" keyword.")
                .WithNote("example: struct identifier { field: type, ... }")
                .WithCode(LdErrorCode.ExpectedIdentifier)
                .Build());

        _ = Expect(TokenKind.LeftBrace, GetErrorBuilder()
            .WithMessage("expected \"{\" after struct identifier.")
            .WithCode(LdErrorCode.ExpectedLeftBrace)
            .Build());

        var fields = new List<StructFieldDeclaration>();
        while (!Matches(TokenKind.RightBrace))
        {
            var fieldIdentifier = Expect(TokenKind.Identifier, GetErrorBuilder()
                .WithMessage("expected an identifier for struct field.")
                .WithCode(LdErrorCode.ExpectedIdentifier)
                .Build());

            _ = Expect(TokenKind.Colon, GetErrorBuilder()
                .WithMessage("expected \":\" after struct field identifier.")
                .WithCode(LdErrorCode.ExpectedColonStructField)
                .Build());

            var fieldType = ParseTypename();

            fields.Add(new StructFieldDeclaration(fieldIdentifier.Lexeme!, fieldType, fieldIdentifier.Location));

            if (Matches(TokenKind.Comma))
            {
                _ = MoveNext();
                continue;
            }
            else if (Matches(TokenKind.RightBrace))
            {
                break;
            }
            else
            {
                throw ParseError(GetErrorBuilder()
                    .WithMessage("expected comma or closing brace.")
                    .WithCode(LdErrorCode.ExpectedCommaOrCloseBrace)
                    .Build());
            }
        }

        _ = Expect(TokenKind.RightBrace, GetErrorBuilder()
            .WithMessage("expected \"}\" to close struct declaration.")
            .WithCode(LdErrorCode.ExpectedRightBrace)
            .Build());

        // NOTE: If a struct is re-defined, this doesn't matter because
        //       the backer type is SortedSet.
        RememberStructDeclaration(identifier.Lexeme!);
        return new StructDeclaration(identifier.Lexeme!, fields, identifier.Location);
    }

    /// <summary>
    /// This parses a base impl statement. This does not parse an "impl X for X"
    /// statement. This will parse the impl keyword, the identifier and all of it's
    /// contained functions. It also parses the braces (obviously.)
    /// </summary>
    /// <returns>The impl statement, this function uses ParseError.</returns>
    public ImplStatement ParseImplStatement()
    {
        var implKeyword = Expect(TokenKind.Impl, GetErrorBuilder()
            .WithMessage("expected \"impl\" keyword.")
            .WithCode(LdErrorCode.ExpectedKeyword)
            .Build());

        var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
            .WithMessage("expected an identifier after \"impl\".")
            .WithCode(LdErrorCode.ExpectedKeyword)
            .WithNote("you must \"impl\" something, specify a struct name.")
            .Build());

        _ = Expect(TokenKind.LeftBrace, GetErrorBuilder()
            .WithMessage($"expected \"{{\" after \"{identifier}\".")
            .WithCode(LdErrorCode.ExpectedLeftBrace)
            .WithNote("\"impl\" statements require a block with function declarations.")
            .WithNote("these functions extend the struct declaration.")
            .Build());

        var declarations = new List<FunctionDeclaration>();
        while (true)
        {
            var next = Peek() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("reached EOF during impl body.")
                .WithCode(LdErrorCode.UnexpectedEOF)
                .Build());

            if (next.Kind == TokenKind.RightBrace)
            {
                break;
            }

            if (next.Kind != TokenKind.Fn)
            {
                throw ParseError(GetErrorBuilder()
                    .WithMessage("only functions are permitted within an impl statement.")
                    .WithCode(LdErrorCode.OnlyFunctionsInImplStmts)
                    .WithNote($"the next token is \"{next.Kind}\", this is not permitted here.")
                    .Build());
            }

            var functionDecl = ParseFunctionDeclaration();
            declarations.Add(functionDecl);
        }

        _ = MoveNext(); // consume "}"

        return new ImplStatement(identifier.Lexeme!, declarations, implKeyword.Location);
    }

    /// <summary>
    /// Parses an enum variant declaration. This includes it's keywords,
    /// and optionally the parenthesis and tagged types.
    /// </summary>
    /// <returns>The enum variant declaration, this function uses ParseError</returns>
    public EnumVariantDeclaration ParseEnumVariantDeclaration()
    {
        var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
            .WithMessage("expected the identifier of an enum variant.")
            .WithCode(LdErrorCode.ExpectedIdentifier)
            .WithNote("while parsing an enum declaration, an identifier was expected.")
            .WithNote("but was not present.")
            .Build());

        if (!Matches(TokenKind.LeftParen))
        {
            // NOTE: This is short-circuit. This case is for when there is n
            //       tagged types, so we can just exit without allocating
            //       extra memory.
            return new EnumVariantDeclaration(identifier.Lexeme!, null, identifier.Location);
        }

        _ = PeekThenAdvance(); // consume "(", we know it exists.

        // NOTE: with the parens we expect at least one typename.
        var taggedTypes = new List<TypeInformation>();

        // These messages exist to help people within errors.
        var messageWithNoTaggedTypes = $"when \"()\" is present on an enum variant, we expect at least one type.";
        var messageWithTaggedTypes = "expected typename due to comma.";

        while (true)
        {
            var typeName = ParseTypename() ?? throw ParseError(GetErrorBuilder()
                    .WithMessage(taggedTypes.Any() ? messageWithTaggedTypes : messageWithNoTaggedTypes)
                    .WithCode(LdErrorCode.EnumVariantExpectedType)
                    .WithNote("if you wish to have no type here, remove the parenthesis or comma.")
                    .Build());
            taggedTypes.Add(typeName);
            var next = Peek();

            if (next is not null && next.Kind == TokenKind.Comma)
            {
                _ = MoveNext();
                continue;
            }
            else if (next is not null && next.Kind == TokenKind.RightParen)
            {
                _ = MoveNext();
                break;
            }
        }

        return new EnumVariantDeclaration(identifier.Lexeme!, taggedTypes, identifier.Location);
    } 

    /// <summary>
    /// This will parse colon notation. So anything such as "Ident::Ident::..."
    /// Example input: "Enum::Variant".
    /// This will parse endlessly until there are no more double colons.
    /// </summary>
    /// <returns>The colon notation, this function uses ParseError.</returns>
    public ColonNotation ParseColonNotation()
    {
        // NOTE: for things like (Enum::Variant)
        // NOTE: also for things like: use std::path::read_file

        var path = new LeftToRightList<string>();
        SourceLocation? startLocation = null;

        while (true)
        {
            var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
                .WithMessage("expected identifier during colon notation.")
                .WithCode(LdErrorCode.ExpectedIdentifier)
                .Build());
            startLocation ??= identifier.Location;
            path.Add(identifier.Lexeme!);

            var next = Peek();

            if (next is not null && next.Kind == TokenKind.ColonColon)
            {
                _ = MoveNext();
                continue;
            }

            break;
        }

        return new ColonNotation(path, startLocation);
    }

    /// <summary>
    /// Parses an enum declaration. This includes the enum keyword,
    /// its identifier, the braces and all of its variants.
    /// </summary>
    /// <returns>The enum declaration, this function uses ParseError</returns>
    public EnumDeclaration ParseEnumDeclaration()
    {
        var enumKeyword = Expect(TokenKind.Enum, GetErrorBuilder()
            .WithMessage("expected enum keyword.")
            .WithCode(LdErrorCode.ExpectedKeyword)
            .Build());

        var typeName = ParseTypename();
        var identifier = typeName.Name;

        var _ = Expect(TokenKind.LeftBrace, GetErrorBuilder()
            .WithMessage($"expect a left-brace after \"{identifier}\"")
            .WithCode(LdErrorCode.ExpectedLeftBrace)
            .WithNote("this is because enums require a body.")
            .Build());

        var variants = new List<EnumVariantDeclaration>();
        var next = Peek();

        if (next is not null && next.Kind == TokenKind.RightBrace)
        {
            return new EnumDeclaration(identifier, typeName.Generics, null, enumKeyword.Location);
        }

        while (true)
        {
            var variant = ParseEnumVariantDeclaration();
            variants.Add(variant);

            next = Peek() ?? throw ParseError(GetErrorBuilder()
                .WithMessage("expected comma or right-brace.")
                .WithCode(LdErrorCode.ExpectedCommaOrCloseBrace)
                .Build());

            if (next.Kind == TokenKind.Comma)
            {
                _ = MoveNext();
                continue;
            }
            else if (next.Kind == TokenKind.RightBrace)
            {
                _ = MoveNext();
                break;
            }
            else
            {
                throw ParseError(GetErrorBuilder()
                    .WithMessage("expected comma or closing-brace after enum variant declaration.")
                    .WithCode(LdErrorCode.ExpectedCommaOrCloseBracket)
                    .WithNote("due to above definition.")
                    .Build());
            }
        }

        RememberEnumDeclaration(identifier);
        return new EnumDeclaration(identifier, typeName.Generics, variants, enumKeyword.Location);
    }

    public List<string> ParseUseStatementSelectedDeclList()
    {
        // example input: {get_data, SomeName, _AnotherName}
        _ = Expect(TokenKind.LeftBrace, GetErrorBuilder()
            .WithMessage("there is no selected members to import. use \"*\" wildcard to include all.")
            .WithCode(LdErrorCode.NoUseMembersSelected)
            .WithNote("if you don't want to import anything, remove this statement.")
            .Build());

        var identifiers = new List<string>();

        while (!Matches(TokenKind.RightBrace))
        {
            var identifier = Expect(TokenKind.Identifier, GetErrorBuilder()
                .WithMessage("expected identifier within use statement selected declaration list")
                .WithCode(LdErrorCode.ExpectedIdentifier)
                .WithNote("add an identifier here after the comma.")
                .Build());
            identifiers.Add(identifier.Lexeme!);

            var next = Peek();
            if (!Matches(TokenKind.Comma) 
                && next is not null
                && next.Kind != TokenKind.RightBrace)
            {
                throw ParseError(GetErrorBuilder()
                    .WithMessage($"expected comma or right brace after \"{identifier.Lexeme}\"")
                    .WithCode(LdErrorCode.ExpectedCommaOrCloseBrace)
                    .WithNote("add a comma after this identifier or remove it.")
                    .WithNote($"got {next.Kind}")
                    .Build());
            }
            else if (next is not null 
                && next.Kind == TokenKind.RightBrace)
            {
                _ = MoveNext(); // consume right brace.
                break;
            }
            _ = MoveNext(); // consume comma.
        }

        return identifiers;
    }

    /// <summary>
    /// This function parses a use statement.
    /// This entire process happens in here, instead of
    /// there being a production for this, at parse time
    /// the module is imported, lexed and then parsed. We then
    /// process what members are wanted an insert them
    /// directly into the AST.
    /// </summary>
    public void ParseUseStatement()
    {
        var useKeyword = Expect(TokenKind.Use, GetErrorBuilder()
            .WithMessage("expected \"use\" keyword.")
            .WithCode(LdErrorCode.ExpectedKeyword)
            .Build());

        var firstIdentifier = Expect(TokenKind.Identifier, GetErrorBuilder()
            .WithMessage("expected at least one identifier after \"use\" keyword.")
            .WithCode(LdErrorCode.ExpectedIdentifier)
            .WithNote("you cannot \"use\" nothing.")
            .Build());

        var path = new List<string>() { firstIdentifier.Lexeme! };
        bool wildcard = false;
        List<string>? selectedDeclarations = null;

        while (Matches(TokenKind.ColonColon))
        {
            _ = MoveNext(); // skip "::"

            if (Matches(TokenKind.Star))
            {
                _ = MoveNext();
                wildcard = true;
                break;
            }

            if (Matches(TokenKind.LeftBrace))
            {
                selectedDeclarations = ParseUseStatementSelectedDeclList();
                break;
            }

            path.Add(Expect(TokenKind.Identifier, GetErrorBuilder()
                .WithMessage("expected identifier \"::\".")
                .WithCode(LdErrorCode.ExpectedIdentifier)
                .Build()).Lexeme!);
        }

        var systemSeperator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "\\" : "/";
        // NOTE: effectively rebuild the original but instead of
        //       "::" we use the systems path seperator.
        var simplifiedPath = string.Join(systemSeperator, path);
        // NOTE: if the module is invalid or does not exist, this function
        //       will deal with that. We are only still here if the module is
        //       valid.
        var (moduleAst, parser) = ParseOtherModule(simplifiedPath);

        if (wildcard)
        {
            // dump the entire modules AST into the current
            // ast.
            _ast.AddRange(moduleAst);
        }
        else
        {
            // We need to filter through the selected
            // declarations and add them.
            var selectedAstNodes = new List<AstNode>();

            ParseAssert(selectedDeclarations is not null, GetErrorBuilder()
                .WithMessage("there are no selected declarations to import from this module.")
                .WithCode(LdErrorCode.ExpectedIdentifier)
                .Build());

            foreach (var selected in selectedDeclarations!)
            {
                var declarationsWithMatchingName = moduleAst
                    .Where(x => x is Declaration)
                    .Where(x => (x as Declaration)!.Identifier == selected);

                if (!declarationsWithMatchingName.Any())
                {
                    throw ParseError(GetErrorBuilder()
                        .WithMessage($"the module \"{simplifiedPath}\" contains no declaration \"{selected}\".")
                        .WithCode(LdErrorCode.NoDeclarationFound)
                        .Build());
                }

                var declaration = (declarationsWithMatchingName.First() as Declaration)!;

                ImportDeclarationWithRequirements(parser, declaration);
            }
        }


    }

    public void ImportDeclarationWithRequirements(Parser module, Declaration declaration)
    {
        if (declaration is StructDeclaration @struct)
        {
            if (StructExists(@struct.Identifier))
            {
                ParseWarning(GetWarningBuilder()
                    .WithMessage($"the type \"{@struct.Identifier}\" has already been imported.")
                    .WithNote("remove this from the use statement.")
                    .Build());
                return; // the type already exists.
            }
            RememberStructDeclaration(@struct.Identifier);

            foreach (var property in @struct.Fields)
            {
                if (property.Type.IsBuiltinType())
                    continue;
                var type = property.Type;

                // already exists in this scope.
                if (StructExists(type.Name) || EnumExists(type.Name))
                    continue;

                if (module.StructExists(type.Name))
                {
                    var referencedStruct = GetNodeWithName(type.Name)!;
                    ImportDeclarationWithRequirements(module, (Declaration)referencedStruct);
                    _ast.Add(referencedStruct);
                    continue;
                }

                if (module.EnumExists(type.Name))
                {
                    var referencedEnum = GetNodeWithName(type.Name)!;
                    ImportDeclarationWithRequirements(module, (Declaration)referencedEnum);
                    _ast.Add(referencedEnum);
                    continue;
                }

                throw ParseError(GetErrorBuilder()
                    .WithMessage($"the module \"{module.GetFileName()}\" has errors.")
                    .WithCode(LdErrorCode.ModuleHadErrors)
                    .WithNote($"[[{module.GetFileName()}]]::{type.Name} was not found.")
                    .WithNote($"this type is referenced inside if the struct \"{declaration.Identifier}\".")
                    .Build());
            }

            // once we are here, all of the required declarations
            // are within our AST. Now we can safely add the declaration.
            _ast.Add(declaration);
            return;
        }
        if (declaration is EnumDeclaration @enum)
        {
            if (EnumExists(@enum.Identifier))
            {
                ParseWarning(GetWarningBuilder()
                    .WithMessage("this enum has already been imported.")
                    .WithNote("consider remove this from your use statement.")
                    .Build());
                return; // the enum has already been imported.
            }

            RememberEnumDeclaration(@enum.Identifier);

            if (@enum.Variants is null)
            {
                // the enum is empty.
                _ast.Add(@enum);
                return;
            }

            bool hasGenericParameters = @enum.Generics?.Count > 0;

            foreach (var variant in @enum.Variants)
            {
                if (variant.TaggedTypes is null)
                    continue;

                foreach (var taggedType in variant.TaggedTypes)
                {
                    if (taggedType.IsBuiltinType())
                        continue;

                    var identifier = taggedType.Name;

                    // already exists in this scope.
                    if (StructExists(identifier) || EnumExists(identifier))
                        continue;

                    if (hasGenericParameters)
                    {
                        if (@enum.Generics!.Any(x => x.Name == identifier))
                            continue;
                    }

                    if (module.StructExists(identifier))
                    {
                        var structDeclaration = module.GetNodeWithName(identifier)!;
                        ImportDeclarationWithRequirements(module, (Declaration)structDeclaration!);
                        _ast.Add(structDeclaration);
                        continue;
                    }

                    if (module.EnumExists(identifier))
                    {
                        var enumDeclaration = module.GetNodeWithName(identifier)!;
                        ImportDeclarationWithRequirements(module, (Declaration)enumDeclaration);
                        _ast.Add(enumDeclaration);
                        continue;
                    }

                    throw ParseError(GetErrorBuilder()
                        .WithMessage($"the module \"{module.GetFileName()}\" has errors.")
                        .WithCode(LdErrorCode.ModuleHadErrors)
                        .WithNote($"[[{module.GetFileName()}]]::{identifier} was not found.")
                        .WithNote($"this type is referenced inside if the enum \"{declaration.Identifier}\".")
                        .Build());
                }
            }

            _ast.Add(@enum);
            return;
        }
        if (declaration is FunctionDeclaration function)
        {
            bool hasGenerics = function.GenericParams?.Count > 0;

            // first check the return type.
            // NOTE: the "?? true" is because when ReturnType
            //       is null, it is void. That is fine.
            if (function.ReturnType is not null
                && !function.ReturnType.IsBuiltinType()
                && !EnumExists(function.ReturnType.Name)
                && !StructExists(function.ReturnType.Name))
            {
                if (module.EnumExists(function.ReturnType.Name)
                    || module.StructExists(function.ReturnType.Name))
                {
                    var decl = module.GetNodeWithName(function.ReturnType.Name);
                    ImportDeclarationWithRequirements(module, (Declaration)decl!);
                    _ast.Add(decl!);
                }
                else
                {
                    throw ParseError(GetErrorBuilder()
                        .WithMessage($"the function \"{function.Identifier}\" in module \"{module.GetFileName()}\" has an invalid return type.")
                        .WithNote($"the type \"{function.ReturnType.Name}\" is unknown")
                        .WithCode(LdErrorCode.ModuleHadErrors)
                        .Build());
                }
            }

            if (function.Parameters is null)
            {
                // no parameters.
                _ast.Add(function);
                return;
            }

            foreach (var parameter in function.Parameters)
            {
                if (!parameter.Type.IsBuiltinType()
                    && !EnumExists(parameter.Identifier)
                    && !StructExists(parameter.Identifier))
                {
                    if (!module.StructExists(parameter.Identifier)
                        && !module.EnumExists(parameter.Identifier))
                    {
                        throw ParseError(GetErrorBuilder()
                            .WithMessage($"the function \"{function.Identifier}\" has an invalid parameter.")
                            .WithCode(LdErrorCode.ModuleHadErrors)
                            .WithNote($"its parameter \"{parameter.Identifier}\" uses an unknown type.")
                            .WithNote($"the unknown type is \"{parameter.Type.Name}\"")
                            .Build());
                    }

                    var node = module.GetNodeWithName(parameter.Type.Name)!;
                    ImportDeclarationWithRequirements(module, (Declaration)node!);
                    _ast.Add(node!);
                }
            }

            _ast.Add(function);
            return;
        }

        throw ParseError(GetErrorBuilder()
            .WithMessage($"you cannot import \"{declaration.Identifier}\"")
            .WithCode(LdErrorCode.UnsupportedImport)
            .WithNote("this is because it is not a struct, enum or function.")
            .Build());
    }

    /// <summary>
    /// This will parse another module then return the state.
    /// </summary>
    /// <param name="relativePath">The path, relative to the project</param>
    /// <returns>The AST and the parser state</returns>
    private (HashSet<AstNode>, Parser) ParseOtherModule(string relativePath)
    {
        var deps = _project.DependenciesDir;
        var pathToModule = Path.Combine(deps, relativePath) + ".ld";

        if (!File.Exists(pathToModule))
        {
            throw ParseError(GetErrorBuilder()
                .WithMessage($"no such module \"{relativePath}\"")
                .WithNote($"install dependencies under \"{deps}\" for this project.")
                .WithCode(LdErrorCode.NoModuleFound)
                .Build());
        }

        var fileContents = File.ReadAllText(pathToModule);
        var fileInfo = new FileInfo(pathToModule);
        // lex then parse contents.
        // these will exit on error so not a problem.

        var moduleTokens = new Lexer(fileInfo).LexTokens();
        var parser = new Parser(fileInfo.Name, _project, moduleTokens, fileContents);
        return (parser.ParseTokens(), parser);
    }

    /// <summary>
    /// This will just attempt to parse SOMETHING. This is top level,
    /// so it will try to parse declarations. The name "ParseSomething"
    /// is because we cannot only return Declarations here because
    /// impl statements aren't technically declarations. If all fails,
    /// it will just attempt to parse a statement.
    /// 
    /// NOTE: Please do not catch a <see cref="ParserDoneException"/>.
    /// This error signals the parser to stop.
    /// </summary>
    /// <returns>An AstNode, any inner functions use ParseError.</returns>
    /// <exception cref="ParserDoneException"></exception>
    public AstNode ParseSomething()
    {
        if (Matches(TokenKind.DebugBreak))
        {
            _ = MoveNext();
            _ = new ParserDebugBreakExpression();
        }

        if (Matches(TokenKind.Use))
        {
            // NOTE: 
            ParseUseStatement();
        }

        if (Matches(TokenKind.Fn))
        {
            return ParseFunctionDeclaration();
        }

        if (Matches(TokenKind.Struct))
        {
            return ParseStructDeclaration();
        }

        if (Matches(TokenKind.Impl))
        {
            return ParseImplStatement();
        }

        if (Matches(TokenKind.Enum))
        {
            return ParseEnumDeclaration();
        }

        if (Matches(TokenKind.Eof))
        {
            throw new ParserDoneException();
        }

        return ParseStatement();
    }

    /// <summary>
    /// Parse the tokens into an abstract syntax tree. If this function
    /// fails it will not return.
    /// </summary>
    /// <returns>The Ast.</returns>
    public HashSet<AstNode> ParseTokens()
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

    /// <summary>
    /// Get the current source location based on the position we are
    /// currently at. If the position is too far ahead, this returns
    /// the last tokens source location. If there are no tokens, an exception
    /// is thrown.
    /// </summary>
    /// <returns>The most recent, or possible source location.</returns>
    /// <exception cref="InvalidDataException"></exception>
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

    /// <summary>
    /// This function will check if the current token matches
    /// <paramref name="kind"/>. If it doesn't, an error with
    /// the contents of <paramref name="message"/> will occur.
    /// This function then increments the position.
    /// </summary>
    /// <param name="kind">The kind of token you are expecting</param>
    /// <param name="message">The error message to display if the token you're expecting is not present.</param>
    /// <returns>The token if <paramref name="kind"/> matches. Otherwise, this function does not return.</returns>
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

    /// <summary>
    /// This function checks if the current tokens <see cref="Token.Kind"/>
    /// matches <paramref name="kind"/>.
    /// </summary>
    /// <param name="kind">The kind you want to match</param>
    /// <returns>True if the current token is of that kind, otherwise false.</returns>
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

    /// <summary>
    /// Creates a parser warning and displays it.
    /// </summary>
    /// <param name="message">The warning message</param>
    private static void ParseWarning(WarningMessage message)
    {
        ErrorHandler.DisplayWarning(message);
    }

    /// <summary>
    /// This function will increment the position by <paramref name="count"/>
    /// if this wouldn't go past the total number of tokens. If it would,
    /// a <see cref="ArgumentException"/> is thrown.
    /// </summary>
    /// <param name="count">The number to increment by</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="count"/> would exceed to total number of tokens</exception>
    private void MoveForwardBy(int count)
    {
        var wantedPosition = _position + count;
        if (wantedPosition >= _tokens.Count)
        {
            throw new ArgumentException("cannot set position past token count.");
        }
        _position += count;
    }

    /// <summary>
    /// This function will peek, then advance the position.
    /// If it is not possible to peek, for example the position would
    /// exceed the total number of tokens, null is returned.
    /// </summary>
    /// <returns>The token, or null when impossible to advance.</returns>
    private Token? PeekThenAdvance()
    {
        if (_position >= _tokens.Count)
        { return null; }

        var token = _tokens[_position];
        MoveNext();
        return token;
    }

    /// <summary>
    /// This function will increment the position and return the next token.
    /// If it's not possible to move forward, the last token is returned.
    /// (The last token will always be EOF).
    /// </summary>
    /// <returns>The next token, or EOF.</returns>
    private Token MoveNext()
    {
        if (++_position < _tokens.Count)
        {
            return _tokens[_position];
        }
        // just return the last token so EOF can be handled.
        return _tokens[^1];
    }

    /// <summary>
    /// This gives you an error builder with the source and location
    /// already supplied so you can only worry about the message.
    /// </summary>
    /// <returns>A primed <see cref="ErrorBuilder"/></returns>
    private ErrorBuilder GetErrorBuilder()
    {
        return new ErrorBuilder(_source).WithSourceLocation(GetCurrentSourceLocation());
    }

    /// <summary>
    /// This gives you an warning builder with the source and location
    /// already supplied so you can only worry about the message.
    /// </summary>
    /// <returns>A primed <see cref="WarningBuilder"/></returns>
    private WarningBuilder GetWarningBuilder()
    {
        return new WarningBuilder(_source).WithSourceLocation(GetCurrentSourceLocation());
    }
    /// <summary>
    /// Returns the current token, if there is one.
    /// </summary>
    /// <returns>The current token, if there is nothing to return, null.</returns>
    private Token? Peek()
    {
        if (_tokens.Count <= _position)
            return null;
        return _tokens[_position];
    }

    /// <summary>
    /// Returns the next token, if there is one.
    /// </summary>
    /// <returns>The next token, if there is nothing return, null.</returns>
    private Token? PeekNext()
    {
        if (_position + 1 < _tokens.Count)
        {
            return _tokens[_position + 1];
        }

        return null;
    }

    /// <summary>
    /// Assert a condition. If condition fails, <paramref name="message"/> is displayed as an error.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    private void ParseAssert(bool condition, ErrorMessage message)
    {
        if (!condition)
            throw ParseError(message);
    }

    private AstNode? GetNodeWithName(string identifier)
    {
        return _ast.Where(x => x is Declaration)
            .Where(x => (x as Declaration)!.Identifier == identifier)
            .FirstOrDefault();
    }

    public Token? PeekAheadBy(int by)
    {
        var requestedPosition = _position + by;
        if (requestedPosition >= _tokens.Count)
            return null;
        return _tokens[requestedPosition];
    }

    /// <summary>
    /// Get the source file that this parser is parsing.
    /// </summary>
    /// <returns></returns>
    public string GetFileName()
    {
        return _fileName;
    }
}
