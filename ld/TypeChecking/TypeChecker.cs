
using Language.Api;
using Language.ErrorHandling;
using Language.Parsing.Productions;

namespace Language.TypeChecking;

public class TypeChecker
{
    private readonly List<AstNode> _ast;
    private readonly List<AstNode> _ourAst;
    private readonly string _source;

    private StorageDeclarationsRegistry _declarations;
    private DefinedFunctionRegistry _functions;

    public StorageDeclarationsRegistry GetDeclarations()
    {
        return _declarations;
    }

    public TypeChecker(List<AstNode> ast, string source)
    {
        _ast = ast;
        _source = source;
        _ourAst = new List<AstNode>();

        // Initialized in Pass1_GetStorageDeclarations()
        _declarations = new(null!);
    }

    public List<AstNode> Check()
    {
        Pass1_GetStorageDeclarations();
        Pass2_GetFunctionDeclarations();

        return _ourAst;
    }

    public void Pass1_GetStorageDeclarations()
    {
        var _storageDecls = new List<Declaration>();

        foreach (var declaration in _ast)
        {
            if (declaration is StructDeclaration @struct)
                _storageDecls.Add(@struct);
            if (declaration is EnumDeclaration @enum)
                _storageDecls.Add(@enum);
        }

        _declarations = new StorageDeclarationsRegistry(_storageDecls);
    }

    public void Pass2_GetFunctionDeclarations()
    {
        var functions = new List<FunctionDeclaration>();

        foreach (var thing in _ast)
        {
            if (thing is FunctionDeclaration function)
                functions.Add(function);
        }

        _functions = new DefinedFunctionRegistry(functions);

        // Just check this is pass 2 to catch it early.

        if (_functions.GetDeclarationOf("main") is null)
        {
            ErrorHandler.QueueNow(
                GetErrorBuilder()
                .WithSourceLocation(_ast[0].Location)
                .WithMessage("this module doesn't have a \"main\" function.")
                .WithNote("define a main function. example: fn main() {}")
                .Build());
            ErrorHandler.DisplayThenExitIfAny();
        }
    }

    public ErrorBuilder GetErrorBuilder()
    {
        return new ErrorBuilder(_source);
    }
}
