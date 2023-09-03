using Language.Lexing;
using Language.Parsing;
using Spectre.Console;
using System.Runtime.InteropServices;

var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? $"C:\\Users\\{Environment.UserName}\\source\\repos\\ld\\playground\\test_script.ld"
    : throw new NotImplementedException("add your path.");

var fileInfo = new FileInfo(path);
var lexer = new Lexer(fileInfo);

var tokens = TimeExpression("lexer", lexer.LexTokens);
var fileContents = File.ReadAllText(fileInfo.FullName);
var ast = TimeExpression("parser", () => new Parser(tokens, fileContents).ParseTokens());

foreach (var node in ast)
{
    node.Visualize(string.Empty, false);
}

static T TimeExpression<T>(string name, Func<T> f)
{
    var start = DateTime.Now;
    var expr = f();
    var end = DateTime.Now;
    AnsiConsole.MarkupLine($"{name} took [blue]{(end - start).TotalMilliseconds}[/]ms to run");
    return expr;
}