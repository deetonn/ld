using Language.Lexing;
using Language.Optimization;
using Language.Parsing;
using Language.Parsing.Productions;
using Language.TypeChecking;

namespace Language;

public struct LdContext
{
    public List<Token> Tokens;
    public List<AstNode> Ast;
}

public class CompileException : Exception 
{
    public CompileException(string message)
        : base(message)
    { }
}

public static class Ld
{
    public static LdContext Compile(ProjectDetails project)
    {
        var baseDir = project.RootDir;
        var srcFolder = Path.Combine(baseDir, "src");

        if (!Directory.Exists(srcFolder))
        {
            throw new CompileException("invalid project directory structure. (expect a src folder)");
        }

        if (File.Exists(Path.Combine(srcFolder, "main.ld")))
        {
            throw new CompileException("no main file found.");
        }

        var mainFile = new FileInfo(Path.Combine(srcFolder, "main.ld"));
        var source = File.ReadAllText(mainFile.FullName);

        var lexer = new Lexer(mainFile);
        var tokens = lexer.LexTokens();

        var parser = new Parser("main.dl", project, tokens, source);
        var ast = parser.ParseTokens();

        var typeChecker = new TypeChecker(ast.ToList(), source);
        var optimizedAst = new Optimizer(typeChecker.Check()).Optimize();

        // TODO: add compile step

        return new LdContext
        {
            Tokens = tokens,
            Ast = optimizedAst,
        };
    }
}