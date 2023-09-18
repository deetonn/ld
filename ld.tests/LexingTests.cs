namespace ld.tests;

using Language.Lexing;

[TestClass]
public class LexerTests
{
    // This class must test every single token.

    private List<Token> GetTokens(string source)
    {
        return new Lexer(source).LexTokens();
    }

    private void TestForSingle(TokenKind kind, string source)
    {
        var tokens = GetTokens(source);

        // 2 because of Eof.
        Assert.AreEqual(2, tokens.Count);
        Assert.AreEqual(kind, tokens[0].Kind);
    }

    private void VerifyIdentifier(Token token, string expectedIdentifier)
    {
        Assert.AreEqual(TokenKind.Identifier, token.Kind);
        Assert.AreEqual(expectedIdentifier, token.Lexeme);
    }

    [TestMethod]
    public void TestEquals()
    {
        TestForSingle(TokenKind.Equals, "=");
        TestForSingle(TokenKind.EqualEqual, "==");

        var tokens = GetTokens("a == 2");

        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual(TokenKind.EqualEqual, tokens[1].Kind);
    }

    [TestMethod]
    public void TestBang()
    {
        TestForSingle(TokenKind.Bang, "!");
        TestForSingle(TokenKind.NotEqual, "!=");

        var tokens = GetTokens("a != \"apples\"");

        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual(TokenKind.NotEqual, tokens[1].Kind);
    }

    [TestMethod]
    public void TestGreaterThan()
    {
        TestForSingle(TokenKind.Gt, ">");

        var lte_tokens = GetTokens(">=");

        Assert.AreEqual(2, lte_tokens.Count);
        Assert.AreEqual(TokenKind.GreaterEquals, lte_tokens[0].Kind);
    }

    [TestMethod]
    public void TestLesserThan()
    {
        TestForSingle(TokenKind.Lt, "<");

        var gte_tokens = GetTokens("<=");

        Assert.AreEqual(2, gte_tokens.Count);
        Assert.AreEqual(TokenKind.LesserEquals, gte_tokens[0].Kind);
    }

    [TestMethod]
    public void TestParens()
    {
        var tokens = GetTokens("()");

        Assert.AreEqual(3, tokens.Count);
        Assert.AreEqual(TokenKind.LeftParen, tokens[0].Kind);
        Assert.AreEqual(TokenKind.RightParen, tokens[1].Kind);
    }

    [TestMethod]
    public void TestBraces()
    {
        var tokens = GetTokens("{}");

        Assert.AreEqual(3, tokens.Count);
        Assert.AreEqual(TokenKind.LeftBrace, tokens[0].Kind);
        Assert.AreEqual(TokenKind.RightBrace, tokens[1].Kind);
    }

    [TestMethod]
    public void TestQuestionMark()
    {
        TestForSingle(TokenKind.QuestionMark, "?");
    }

    [TestMethod]
    public void TestColons()
    {
        var tokens = GetTokens(": ::");

        Assert.AreEqual(3, tokens.Count);
        Assert.AreEqual(TokenKind.Colon, tokens[0].Kind);
        Assert.AreEqual(TokenKind.ColonColon, tokens[1].Kind);
    }

    [TestMethod]
    public void TestCommas()
    {
        TestForSingle(TokenKind.Comma, ",");

        var tokens = GetTokens("(2, 3, 0)");
        Assert.AreEqual(8, tokens.Count);

        Assert.AreEqual(TokenKind.Comma, tokens[2].Kind);
        Assert.AreEqual(TokenKind.Comma, tokens[4].Kind);
    }

    [TestMethod]
    public void TestSemiColon()
    {
        TestForSingle(TokenKind.Semi, ";");

        var tokens = GetTokens("let a = 2;");

        Assert.AreEqual(6, tokens.Count);
        Assert.AreEqual(TokenKind.Semi, tokens[4].Kind);
    }

    // Plus, Minus, Slash, Star

    [TestMethod]
    public void TestPlus()
    {
        TestForSingle(TokenKind.Plus, "+");
        TestForSingle(TokenKind.PlusEquals, "+=");

        var tokens = GetTokens("a + 2");

        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual(TokenKind.Plus, tokens[1].Kind);
    }

    [TestMethod]
    public void TestMinus()
    {
        TestForSingle(TokenKind.Minus, "-");
        TestForSingle(TokenKind.MinusEquals, "-=");

        var tokens = GetTokens("a - 2");

        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual(TokenKind.Minus, tokens[1].Kind);
    }

    [TestMethod]
    public void TestSlash()
    {
        TestForSingle(TokenKind.Slash, "/");

        var tokens = GetTokens("a / 2");

        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual(TokenKind.Slash, tokens[1].Kind);
    }

    [TestMethod]
    public void TestStar()
    {
        TestForSingle(TokenKind.Star, "*");

        var tokens = GetTokens("a * 2");

        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual(TokenKind.Star, tokens[1].Kind);
    }

    [TestMethod]
    public void TestModulo()
    {
        TestForSingle(TokenKind.Modulo, "%");

        var tokens = GetTokens("a % \"wtf\"");

        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual(TokenKind.Modulo, tokens[1].Kind);
    }

    [TestMethod]
    public void TestPipe()
    {
        TestForSingle(TokenKind.Pipe, "|");

        var tokens = GetTokens("a | 2");

        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual(TokenKind.Pipe, tokens[1].Kind);
    }

    [TestMethod]
    public void TestAmpersand()
    {
        TestForSingle(TokenKind.Ampersand, "&");

        var tokens = GetTokens("a & 2");

        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual(TokenKind.Ampersand, tokens[1].Kind);
    }

    [TestMethod]
    public void TestAnd()
    {
        TestForSingle(TokenKind.And, "&&");
        TestForSingle(TokenKind.And, "and");

        var tokens = GetTokens("a && 2 and 3");

        Assert.AreEqual(6, tokens.Count);
        Assert.AreEqual(TokenKind.And, tokens[1].Kind);
        Assert.AreEqual(TokenKind.And, tokens[3].Kind);
    }

    [TestMethod]
    public void TestOr()
    {
        TestForSingle(TokenKind.Or, "||");

        var tokens = GetTokens("a == 3 || a == 2");

        Assert.AreEqual(8, tokens.Count);
        Assert.AreEqual(TokenKind.Or, tokens[3].Kind);
    }

    [TestMethod]
    public void TestUnsignedInteger()
    {
        TestForSingle(TokenKind.Number, "69");
        TestForSingle(TokenKind.Number, "3924425425");
        TestForSingle(TokenKind.Number, $"{int.MaxValue}");
        TestForSingle(TokenKind.Number, $"{ulong.MaxValue}");
        TestForSingle(TokenKind.Number, $"{uint.MaxValue}");
    }

    [TestMethod]
    public void TestFloatingPointInteger()
    {
        TestForSingle(TokenKind.Number, "21.234");
        TestForSingle(TokenKind.Number, "9.923842025");
        TestForSingle(TokenKind.Number, $"{short.MaxValue}.99");
    }

    [TestMethod]
    public void TestIdentifiers()
    {
        TestForSingle(TokenKind.Identifier, "identifier");
        TestForSingle(TokenKind.Identifier, "an_id_");
        TestForSingle(TokenKind.Identifier, "identifier2");
        TestForSingle(TokenKind.Identifier, "_99248");
    }

    [TestMethod]
    public void TestDots()
    {
        TestForSingle(TokenKind.Dot, ".");

        var tokens = GetTokens("a.b.c");

        Assert.AreEqual(6, tokens.Count);

        VerifyIdentifier(tokens[0], "a");
        Assert.AreEqual(TokenKind.Dot, tokens[1].Kind);
        VerifyIdentifier(tokens[2], "b");
        Assert.AreEqual(TokenKind.Dot, tokens[3].Kind);
        VerifyIdentifier(tokens[4], "c");
    }

    [TestMethod]
    public void TestSingleLineComments()
    {
        TestForSingle(TokenKind.Eof, "// hello world");
    }

    [TestMethod]
    public void TestMultilineComments()
    {
        TestForSingle(TokenKind.Eof, "/* this is a comment */");
        var tokens = GetTokens("let a /*comment*/ = 2");

        Assert.AreEqual(5, tokens.Count);
        Assert.AreEqual(TokenKind.Let, tokens[0].Kind);
        Assert.AreEqual(TokenKind.Identifier, tokens[1].Kind);
        VerifyIdentifier(tokens[1], "a");

        Assert.AreEqual(TokenKind.Equals, tokens[2].Kind);
        Assert.AreEqual(TokenKind.Number, tokens[3].Kind);
    }
}