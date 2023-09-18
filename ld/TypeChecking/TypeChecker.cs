
using System.Reflection;
using System.Text;
using Language.Api;
using Language.ErrorHandling;
using Language.Parsing.Productions;
using Language.Parsing.Productions.Math;

namespace Language.TypeChecking;

// public record ScopeInformation() 

public class TypeChecker
{
    private readonly List<AstNode> _ast;
    private readonly List<AstNode> _ourAst;
    private readonly string _source;

    private StorageDeclarationsRegistry _declarations;
    private DefinedFunctionRegistry _functions;
    private ImplBlockRegistry _impls;

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
        // Initialized in Pass2_GetFunctionDeclarations()
        _functions = new(null!);
        // Initialized in Pass3_GetImplStatements()
        _impls = new(null!);
    }

    public List<AstNode> Check()
    {
        Pass1_GetStorageDeclarations();
        Pass2_GetImplStatements();
        Pass3_GetFunctionDeclarations();
        Pass4_CheckImplFunctions();
        Pass5_CheckNormalFunctions();

        var ast = RebuildAst();

        ErrorHandler.DisplayThenExitIfAny();

        return ast;
    }

    public List<AstNode> RebuildAst()
    {
        // NOTE: all declarations inside of this registry have been modified
        //       if that was required.
        // NOTE[2]: The order doesn't matter, because the interpreter
        //          or code generator must round up declarations before
        //          beginning to evaluate expressions.

        foreach (var storageDeclaration in GetStorageDeclarationsRegistry().Declarations())
        {
            _ourAst.Add(storageDeclaration);
        }

        foreach (var functionDeclaration in GetFunctionRegistry().Functions())
        {
            _ourAst.Add(functionDeclaration);
        }

        foreach (var implBlock in GetImplBlockRegistry().Impls())
        {
            _ourAst.Add(implBlock);
        }

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
    public void Pass3_GetFunctionDeclarations()
    {
        var functions = new List<FunctionDeclaration>();

        foreach (var thing in _ast)
        {
            if (thing is FunctionDeclaration function)
            {
                CheckFunctionDeclaration(function);
                CheckFunctionDeclarationBody(function);
                functions.Add(function);
            }
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

    public void Pass2_GetImplStatements()
    {
        var implStatements = _ast
            .Where(x => x is ImplStatement)
            .Select(x => (x as ImplStatement)!);

        if (implStatements is null || !implStatements.Any())
        {
            _impls = new ImplBlockRegistry(new());
            LogDebug(this, "no impl statements, skipping pass3");
            return;
        }

        _impls = new ImplBlockRegistry(implStatements.ToList());
    }

    public void Pass4_CheckImplFunctions() 
    {
        // NOTE: This function will verify all impl functions
        //       are correct.
    }

    public void Pass5_CheckNormalFunctions()
    {
        // The declaration has been handled, we just need to 
        // check the block.
        foreach (var declaration in GetFunctionRegistry().Functions())
        {
            var arguments = declaration.Parameters is null
                ? Array.Empty<ScopedObject>().ToList()
                : declaration.Parameters.Select(x => new ScopedObject
                {
                    // FIXME: change this once "mut" keyword is supported on
                    //        arguments.
                    Constant = true,
                    Identifier = x.Identifier,
                    Type = x.Type
                }).ToList();
            var scope = new ScopeInfo(arguments);
            scope.SetReturnType(declaration.ReturnType);

            CheckBlock(declaration.Body, scope);
        }
    }

    public void CheckAssignment(AstNode node, ScopeInfo scope)
    {
        var assignment = (node as Assignment)!;

        // NOTE: "scope" is used for a block. So we can mutate it here,
        //       then further statements can access it.

        // make sure the annotation type is valid, if it exists.
        if (assignment.AnnotatedType is not null)
        {
            if (_declarations.GetDeclarationOf(assignment.AnnotatedType.Name) is null)
            {
                Error(GetErrorBuilder()
                    .WithCode(LdErrorCode.UndefinedType)
                    .WithSourceLocation(assignment.Location)
                    .WithMessage($"the type \"{assignment.AnnotatedType.Name}\" is undefined.")
                    .WithNote($"used on assignment of \"{assignment.Identifier}\"")
                    .Build());
            }
        }

        if (assignment.Expression is null)
        {
            // There is no expression. This is an uninitialized
            // instance. 
            return;
        }

        CheckExpression(scope, assignment.Expression);

        var expressionType = InferType(scope, assignment.Expression);
        if (assignment.AnnotatedType is null)
        {
            SetReadonlyProperty(assignment, nameof(assignment.AnnotatedType), expressionType);
        }
        else {
            if (assignment.AnnotatedType != expressionType)
            {
                Error(GetErrorBuilder()
                    .WithCode(LdErrorCode.InvalidTypeAssignment)
                    .WithSourceLocation(assignment.Location)
                    .WithMessage($"cannot assign an entity of type \"{expressionType.Name}\" to an entity of type \"{assignment.AnnotatedType.Name}\".")
                    .WithNote("the assigned expression evaluates to a different type.")
                    .Build());
            }
        }
    }

    public bool IsValidTypeForMathematics(TypeInformation left, TypeInformation right)
    {
        return left.IsMathematicallySupported() && right.IsMathematicallySupported();
    }

    public void CheckMathematicExpression(ScopeInfo scope, Expression expr)
    {
        if (expr is MathematicalExpression math)
        {
            var (left, right) = (InferType(scope, math.Left), InferType(scope, math.Right));
            if (left != right)
            {
                // very strict maths, because implicit casting causes bugs.
                // just be explicit with types.
                Error(GetErrorBuilder()
                    .WithSourceLocation(math.Location)
                    .WithCode(LdErrorCode.NoImplicitCasts)
                    .WithMessage($"cannot perform addition between \"{left.Name}\" and \"{right.Name}\".")
                    .WithNote("mathematic operators are reserved for the same types.")
                    .WithNote("use an explicit cast with the \"as\" keyword.")
                    .Build());
            }

            if (!IsValidTypeForMathematics(left, right))
            {
                Error(GetErrorBuilder()
                    .WithSourceLocation(math.Location)
                    .WithCode(LdErrorCode.InvalidMathContext)
                    .WithMessage($"this operator cannot be applied between \"{left.Name}\" & \"{right.Name}\"")
                    .WithNote("mathematic operators cannot be overriden.")
                    .WithNote($"search for docs on \"{left.Name}\" or \"{right.Name}\" to find out the equivilent.")
                    .Build());
            }
        }
    }

    public void CheckGroupingExpression(ScopeInfo scope, GroupingExpression expr)
    {
        foreach (var equation in expr.Expressions)
        {
            CheckMathematicExpression(scope, equation);
        }
    }

    public void CheckExpression(ScopeInfo scope, Expression expr)
    {
        if (expr is FunctionCall call)
        {
            CheckFunctionCall(scope, call);
        }
        if (expr is GroupingExpression grouping)
        {
            CheckGroupingExpression(scope, grouping);
        }
        if (expr is MathematicalExpression math)
        {
            CheckMathematicExpression(scope, math);
        }
    }

    public TypeInformation InferType(ScopeInfo scope, Expression expr)
    {
        var evaluator = CreateExpressionEvaluator(scope);
        return (TypeInformation) expr.Visit(evaluator);
    }

    public void CheckFunctionCall(ScopeInfo scope, FunctionCall call)
    {
        var declaration = _functions.GetDeclarationOf(call.Identifier) ?? throw Error(GetErrorBuilder()
                .WithSourceLocation(call.Location)
                .WithCode(LdErrorCode.UndefinedIdentifier)
                .WithMessage($"no function named \"{call.Identifier}\" exists.")
                .Build());

        if (declaration.Parameters?.Count != call.Arguments?.Count)
        {
            var formatForFunctionDecl = GetExpectedArgumentsFormat(declaration.Parameters?.Select(x => x as object).ToList());
            var formatForFunctionCall = GetExpectedArgumentsFormat(call.Arguments?.Select(x => x as object).ToList());

            Error(GetErrorBuilder()
                .WithSourceLocation(call.Location)
                .WithCode(LdErrorCode.NoExpectedParameters)
                .WithMessage($"function \"{declaration.Identifier}\" expects {formatForFunctionDecl}.")
                .WithNote($"{formatForFunctionCall} were supplied.")
                .WithNote($"function defined here: {declaration.Location.ToUserString()}")
                .Build());
        }

        if (declaration.Parameters is null || call.Arguments is null)
            return;

        for (int i = 0; i < call.Arguments.Count; i++)
        {
            var expected = declaration.Parameters[i];
            var expression = call.Arguments[i];

            var expressionType = InferType(scope, expression);

            if (expressionType != expected.Type)
            {
                Error(GetErrorBuilder()
                    .WithSourceLocation(expression.Location)
                    .WithCode(LdErrorCode.InvalidTypeAssignment)
                    .WithMessage($"argument \"{expected.Identifier}\" expects type \"{expected.Type}\" but got \"{expressionType}\".")
                    .Build());
            }
        }
    }

    public void CheckBlock(Block block, ScopeInfo scope)
    {
        foreach (var statement in block.Nodes)
        {
            switch (statement.GetType().Name)
            {
                case nameof(Assignment):
                    CheckAssignment(statement, scope);
                    break;
                case nameof(ReturnStatement):
                    CheckReturnStatement(statement, scope);
                    break;
                case nameof(FunctionCall):
                    CheckFunctionCall(scope, (FunctionCall)statement);
                    break;
                default:
                    throw new NotImplementedException($"type check node of type: {statement.GetType().Name}");
            }
        }
    }

    public void CheckReturnStatement(AstNode statement, ScopeInfo scope)
    {
        var @return = (statement as ReturnStatement)!;
        var expectedType = scope.GetReturnType();

        if (@return.Return is null && expectedType is null)
        {
            // OK! The return is not attempt to return anything, 
            // and the body expects nothing to be returned.
            return;
        }

        if (expectedType is null && @return.Return is not null)
        {
            Error(GetErrorBuilder()
                .WithCode(LdErrorCode.InvalidReturn)
                .WithSourceLocation(@return.Location)
                .WithMessage($"the specified return type is void, but here a value is returned.")
                .Build());
            return;
        }

        if (expectedType is not null && @return.Return is null)
        {
            Error(GetErrorBuilder()
                .WithCode(LdErrorCode.InvalidReturn)
                .WithSourceLocation(@return.Location)
                .WithMessage($"expected an expression of type \"{expectedType.Name}\" to be returned, but got void.")
                .Build());
            return;
        }

        var evaluator = CreateExpressionEvaluator(scope);
        CheckExpression(scope, @return.Return!);
        var exprType = (TypeInformation)@return.Return!.Visit(evaluator);

        if (!exprType.SignatureMatches(expectedType))
        {
            Error(GetErrorBuilder()
                .WithCode(LdErrorCode.InvalidReturn)
                .WithSourceLocation(@return.Location)
                .WithMessage($"expected type \"{expectedType?.Name}\", but got an expression of type \"{exprType.Name}\"")
                .WithNote($"you are attempting to return a different type than what is hinted.")
                .Build());
        }

        // Okay, no problems.
    }

    public void CheckImplFunctionDeclaration(ImplStatement parent, FunctionDeclaration fn)
    {
        // NOTE: when we meet a "self" parameter, we must resolve the type
        //       here.
    }

    public void CheckFunctionDeclarationBody(FunctionDeclaration fn)
    {

    }

    public void CheckBlock(Block block)
    {

    }

    public void CheckFunctionDeclaration(FunctionDeclaration function)
    {
        // This function is called after all structs have been
        // located. So we can make sure the declaration is not invalid.

        if (function.Parameters is not null)
        {
            foreach (var parameter in function.Parameters)
            {
                // builtin in types are guaranteed to be imported.
                if (parameter.Type.IsBuiltinType())
                {
                    continue;
                }

                if (parameter.Identifier == "self")
                {
                    // a function that is not within an "impl"
                    // cannot contain the self keyword.

                    Error(GetErrorBuilder()
                        .WithMessage("functions that are not attached to an entity cannot contain a \"self\" parameter.")
                        .WithSourceLocation(parameter.Location)
                        .WithCode(LdErrorCode.SelfOnUnattachedFunction)
                        .WithNote($"\"{function.Identifier}\" is a stand-alone function, therefore self would be impossible to infer.")
                        .Build());
                    continue;
                }

                var isGenericParam = function.GenericParams?.Any(x => x.Name == parameter.Type.Name) ?? false;

                if (!isGenericParam && _declarations.GetDeclarationOf(parameter.Type.Name) is null)
                {
                    Error(GetErrorBuilder()
                        .WithMessage($"the type \"{parameter.Type.Name}\" is not defined.")
                        .WithSourceLocation(parameter.Location)
                        .WithCode(LdErrorCode.UndefinedType)
                        .WithNote($"this type is used on the parameter named \"{parameter.Identifier}\"")
                        .Build());
                }
            }
        }
        if (function.ReturnType is null) 
        {
            // return type is void, no need for more checking.
            return;
        }
        if (function.ReturnType.IsBuiltinType())
            return;

        var returnTypeIsGeneric = function.GenericParams?.Any(x => x.Name == function.ReturnType.Name) ?? false;

        if (!returnTypeIsGeneric && _declarations.GetDeclarationOf(function.ReturnType.Name) is null)
        {
            Error(GetErrorBuilder()
                .WithSourceLocation(function.Location)
                .WithCode(LdErrorCode.UndefinedType)
                .WithMessage($"the function \"{function.Identifier}\" has an undeclared type as its return type.")
                .Build());
        }
    }

    public DefinedFunctionRegistry GetFunctionRegistry()
        => _functions;

    public StorageDeclarationsRegistry GetStorageDeclarationsRegistry()
        => _declarations;

    public ImplBlockRegistry GetImplBlockRegistry() 
        => _impls;

    /// <summary>
    /// Push an error into the queue, this does not exit.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static Exception Error(ErrorMessage message)
    {
        ErrorHandler.QueueNow(message);
        return new();
    }

    public ErrorBuilder GetErrorBuilder()
    {
        return new ErrorBuilder(_source);
    }

    public ExpressionEvaluator CreateExpressionEvaluator(ScopeInfo scope)
    {
        return new(_source, scope, this);
    }

    public static void SetReadonlyProperty(object instance, string identifier, object value)
    {
        var propertys = instance.GetType().GetProperties();
        var wantedProperty = propertys.Where(x => x.Name == identifier).FirstOrDefault();

        ArgumentNullException.ThrowIfNull(wantedProperty);

        wantedProperty.SetValue(instance, value);
    }

    public static string GetExpectedArgumentsFormat(List<object>? dec)
    {
        if (dec == null)
        {
            return "no arguments";
        }
        return dec.Count switch
        {
            0 => "zero arguments",
            1 => "one argument",
            _ => $"{dec.Count} arguments"
        };
    }
}
