using Language.Lexing;
using Language.Parsing;
using System.Runtime.InteropServices;

var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? $"C:\\Users\\{Environment.UserName}\\source\\repos\\ld\\playground\\test_script.ld"
    : throw new NotImplementedException("add your path.");

var fileInfo = new FileInfo(path);
var lexer = new Lexer(fileInfo);

var tokens = lexer.LexTokens();
var ast = new Parser(tokens, File.ReadAllText(fileInfo.FullName)).ParseTokens();

foreach (var node in ast)
{
    node.Visualize(string.Empty, false);
}