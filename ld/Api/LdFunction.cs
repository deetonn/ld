using Language.Parsing;
using Language.Parsing.Productions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FunctionArguments = System.Collections.Generic.List<Language.Parsing.Productions.FunctionDeclarationParameter>;

namespace Language.Api;

public class LdFunction : LdObject
{
    public string Name { get; }
    public FunctionArguments ExpectedParams { get; }
    public TypeInformation? ReturnType { get; }

    public LdFunction(
        string name, 
        Block body, 
        FunctionArguments expectedParams,
        TypeInformation? returnType)
    {
        Underlying = body;
        Name = name;
        ExpectedParams = expectedParams;
        ReturnType = returnType;
    }

    public LdObject? Call(IAstVisitor context, List<Expression> arguments)
    {
        var currentScope = new LdScope();
        context.SwitchContext(currentScope);

        context.ExitContext();

        throw new NotImplementedException();
    }
}
