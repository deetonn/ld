using Language;
using Language.Lexing;
using Language.Parsing;
using Language.TypeChecking;
using Spectre.Console;
using System.Runtime.InteropServices;

var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? $"C:\\Users\\{Environment.UserName}\\source\\repos\\ld\\playground\\test_script.ld"
    : throw new NotImplementedException("add your path.");

var fileInfo = new FileInfo(path);
var lexer = new Lexer(fileInfo);

var projectDetails = new ProjectDetails($"C:\\Users\\{Environment.UserName}\\source\\repos\\ld\\playground",
    $"C:\\Users\\{Environment.UserName}\\source\\repos\\ld\\playground\\deps");

var tokens = TimedExpression("[yellow]lexer[/]", lexer.LexTokens);
var fileContents = File.ReadAllText(fileInfo.FullName);
var ast = TimedExpression("[cyan]parser[/]", () => new Parser(fileInfo.Name, projectDetails, tokens, fileContents).ParseTokens());

foreach (var node in ast)
{
    node.Visualize(string.Empty, false);
}

var checker = TimedExpression("[red]typechecker[/]", () => new TypeChecker(ast.ToList(), fileContents));
checker.Check();

static T TimedExpression<T>(string name, Func<T> f)
{
    var start = DateTime.Now;
    var expr = f();
    var end = DateTime.Now;
    AnsiConsole.MarkupLine($"{name} took [blue]{(end - start).TotalMilliseconds}[/]ms to run");
    return expr;
}