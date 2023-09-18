using Language.Parsing.Productions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Language.TypeChecking;

public class DefinedFunctionRegistry
{
    private readonly List<FunctionDeclaration> _functions;

    public DefinedFunctionRegistry(List<FunctionDeclaration> functions)
    {
        _functions = functions;
    }

    public FunctionDeclaration? GetDeclarationOf(string ident)
    {
        return _functions.FirstOrDefault(x => x.Identifier == ident);   
    }

    public List<FunctionDeclaration> Functions() => _functions;
}
