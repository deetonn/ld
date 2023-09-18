

using System.Security.AccessControl;
using Language.Api;
using Language.ErrorHandling;
using Language.Lexing;
using Language.Parsing;
using Language.Parsing.Productions;
using Language.Parsing.Productions.Conditional;
using Language.Parsing.Productions.Literals;
using Language.Parsing.Productions.Math;
using Language.TypeChecking;

public class ExpressionEvaluator : IAstVisitor
{
    private ScopeInfo _scope;
    private readonly string _source;
    private readonly TypeChecker _context;

    public ExpressionEvaluator(string source, ScopeInfo scope, TypeChecker context)
    {
        _scope = scope;
        _source = source;
        _context = context;
    }

    public object AndExpression(AndExpression andExpression)
    {
        return TypeInformation.Bool();
    }

    public object CopyVariable(CopiedVariable copiedVariable)
    {
        if (_scope.Contains(copiedVariable.Identifier, out var type))
        {
            return type;
        }

        throw new NotImplementedException("you must verify a variable exists before attempting to infer its type.");
    }

    public LdScope ExitContext()
    {
        // NOTE: this should not be used while evaluating expressions.
        throw new NotImplementedException();
    }

    public object ReferenceVariable(ReferencedVariable referencedVariable)
    {
        if (_scope.Contains(referencedVariable.Identifier, out var type))
        {
            return type;
        }

        throw new NotImplementedException("verify that the variable exists before attempting to infer its type.");
    }

    public void SwitchContext(LdScope newContext)
    {
        // NOTE: this should not be used while evaluating expressions.
        throw new NotImplementedException();
    }

    public LdScope ThisScope()
    {
        // NOTE: this should not be used while evaluating expressions.
        throw new NotImplementedException();
    }

    public object Visit32BitInteger(Number32Bit number)
    {
        return number.IsUnsigned ? 
            TypeInformation.U32()
            : TypeInformation.I32();
    }

    public object Visit64BitInteger(Number64Bit number)
    {
        return number.IsUnsigned ? 
            TypeInformation.U64()
            : TypeInformation.I64();
    }

    public object VisitAddition(AdditionExpression additionExpression)
    {
        // The expression will evaluate to whatever "right" is.
        // This is the way expressions usually work, so I want to keep
        // this behaviour.
        return additionExpression.Right.Visit(this);
    }

    public object VisitArrayLiteralExpression(ArrayLiteralExpression arrayLiteralExpression)
    {
        // we will just lazily assume the type of the array. This will be 
        // inferred to the type of the first element. We expect there to at least
        // be elements here.

        var innerType = arrayLiteralExpression.Initializer.First().Visit(this);
        return TypeInformation.Array((TypeInformation)innerType, (uint)arrayLiteralExpression.Initializer.Count);
    }

    public object VisitAssignment(Assignment assignment)
    {
        // Ld allows expressions such as: 
        // a = let b = 2;
        // This will assign "a" to "b" which is assigned to "2".

        if (assignment.Expression is null)
        {
            if (assignment.AnnotatedType is null)
            {
                Error(GetErrorBuilder()
                    .WithSourceLocation(assignment.Location)
                    .WithCode(LdErrorCode.CouldNotInferType)
                    .WithMessage("could not infer type of \"{}\" because there is no annotation or initializer.")
                    .Build());
            }

            // The expressed type will take precedence here.
            // (because its literally all we have.)
            return assignment.AnnotatedType!;
        }

        // otherwise the expression itself takes precedence, this is because
        // the annotation is just expressing desire, rather than what is actually
        // happening. The type checker will verify against any hints.
        return assignment.Expression.Visit(this);
    }

    public object VisitBitwiseAndExpression(BitwiseAndExpression bitwiseAndExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitBitwiseOrExpression(BitwiseOrExpression bitwiseOrExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitBlock(Block block)
    {
        // We lazily evaluate this. The first "return" statement we come across
        // is the inferred type. Otherwise, its void. We must evaluate this way
        // because blocks are assignable. They are expressions.

        object? type = null;
        foreach (var statement in block.Nodes)
        {
            if (statement is ReturnStatement ret)
            {
                type = ret.Return?.Visit(this);
            }
        }

        return type ?? TypeInformation.Void();
    }

    public object VisitBooleanExpression(BooleanExpression booleanExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitColonNotation(ColonNotation colonNotation)
    {
        // TODO: check what is going on.
        // If the first is an enum, this is that enum.
        // If the first is a struct, this is one of its members.
        // Otherwise, fuck knows.
        throw new NotImplementedException();
    }

    public object VisitDivisionExpression(DivisionExpression divisionExpression)
    {
        return divisionExpression.Right.Visit(this);
    }

    public object VisitDotNotation(VariableDotNotation variableDotNotation)
    {
        // This expression is based on whatever is being done.
        // If its something like "a.b" then the type is of whatever "b" is.
        // if its something like "a.b()" then type is whatever "b" returns.
        throw new NotImplementedException();
    }

    public object VisitEnumDeclaration(EnumDeclaration enumDeclaration)
    {
        throw Error(GetErrorBuilder()
            .WithSourceLocation(enumDeclaration.Location)
            .WithCode(LdErrorCode.NotAnExpression)
            .WithMessage("an enum declaration is not an expression.")
            .Build());
    }

    public object VisitEnumVariantDeclaration(EnumVariantDeclaration enumVariant)
    {
        // This will never happen, the parser filters out stuff like this.
        throw new NotImplementedException();
    }

    public object VisitEnumVariantExpression(EnumVariantExpression enumVariantExpression)
    {
        if (_scope.Contains(enumVariantExpression.EnumName, out var type))
        {
            return type;
        }

        // not sure how this could ever happen.
        throw Error(GetErrorBuilder()
            .WithCode(LdErrorCode.UndefinedType)
            .WithSourceLocation(enumVariantExpression.Location)
            .WithMessage($"undefined enum \"{enumVariantExpression.EnumName}\"")
            .Build());
    }

    public object VisitFloatingPointNumber(FloatingPointNumber floatingPointNumber)
    {
        // TODO: add different lengths (f32, f64)
        return TypeInformation.F64();
    }

    public object VisitFunctionCall(FunctionCall functionCall)
    {
       var definedFunctions = _context.GetFunctionRegistry();
       var function = definedFunctions.GetDeclarationOf(functionCall.Identifier) ?? throw Error(GetErrorBuilder()
                .WithSourceLocation(functionCall.Location)
                .WithCode(LdErrorCode.CannotCallNonFunctionType)
                .WithMessage($"\"{functionCall.Identifier}\" is not a function.")
                .WithNote("only functions can be called with the \"ident()\" syntax.")
                .Build());

        return function.ReturnType ?? TypeInformation.Void();
    }

    public object VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
    {
        throw Error(GetErrorBuilder()
            .WithCode(LdErrorCode.NotAnExpression)
            .WithSourceLocation(functionDeclaration.Location)
            .WithMessage($"a function declaration is not an expression.")
            .Build());
    }

    public object VisitGrouping(GroupingExpression groupingExpression)
    {
        return groupingExpression.Expressions.Last().Visit(this);
    }

    public object VisitIfStatement(IfStatement ifStatement)
    {
        throw Error(GetErrorBuilder()
            .WithCode(LdErrorCode.NotAnExpression)
            .WithSourceLocation(ifStatement.Location)
            .WithMessage("\"if\" statements are not expressions on their own.")
            .WithNote("hint: blocks are expressions. use an if inside a block and return conditional data there.")
            .Build());
    }

    public object VisitImplStatement(ImplStatement implStatement)
    {
        // this can't happen. The parser catches this.
        throw new NotImplementedException();
    }

    public object VisitIsEqualToExpression(IsEqualToExpression isEqualToExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitIsGreaterEqualToExpression(IsGreaterEqualToExpression isGreaterEqualToExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitIsGreaterThanExpression(IsGreaterThanExpression isGreaterThanExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitIsLesserEqualToExpression(IsLesserEqualToExpression isLesserEqualToExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitIsLesserThanExpression(IsLesserThanExpression isLesserThanExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitIsNotEqualToExpression(IsNotEqualToExpression isNotEqualToExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitModuloExpression(ModuloExpression moduloExpression)
    {
        return TypeInformation.SizeT();
    }

    public object VisitMultiplicationExpression(MultiplicationExpression multiplicationExpression)
    {
        return multiplicationExpression.Right.Visit(this);
    }

    public object VisitNotExpression(NotExpression notExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitOrExpression(OrExpression orExpression)
    {
        return TypeInformation.Bool();
    }

    public object VisitReturnStatement(ReturnStatement returnStatement)
    {
        // This cannot happen. The parser catches this.
        throw new NotImplementedException();
    }

    public object VisitStaticStructAccessExpression(StaticStructFunctionCallExpression staticStructAccessExpression)
    {
        var name = staticStructAccessExpression.StructName;
        var access = staticStructAccessExpression.FunctionName;

        var @struct = 
            _context.GetStorageDeclarationsRegistry().GetDeclarationOf(name) as StructDeclaration
            ?? throw Error(GetErrorBuilder()
                .WithCode(LdErrorCode.UndefinedType)
                .WithSourceLocation(staticStructAccessExpression.Location)
                .WithMessage($"the type \"{name}\" has not been declared.")
                .Build());

        // NOTE: the parser does the checks for us and makes sure the referenced
        //       type is a struct.

        // now we try to find the function found inside of an "impl"
        // TODO: find the impl for the struct and look for "access".

        var impls = _context.GetImplBlockRegistry();
        var allImplementedFunctions = impls.GetDeclarationsOf(name) ?? throw Error(GetErrorBuilder()
            .WithCode(LdErrorCode.UndefinedType)
            .WithSourceLocation(staticStructAccessExpression.Location)
            .WithMessage($"the struct \"{name}\" has no implementations.")
            .WithNote($"cannot call \"{name}::{access}\" because it is not defined.")
            .Build());

        var selectedFunction = allImplementedFunctions.Where(x => x.Identifier == access);
        var containsThatFunction = selectedFunction.Any();

        if (!containsThatFunction)
        {
            throw Error(GetErrorBuilder()
                .WithCode(LdErrorCode.UndefinedImplFunction)
                .WithSourceLocation(staticStructAccessExpression.Location)
                .WithMessage($"the function \"{access}\" is undefined on the struct \"{name}\"")
                .WithNote($"no \"impl\" block defines this function.")
                .Build());
        }

        return selectedFunction.First().ReturnType ?? TypeInformation.Void();
    }

    public object VisitStringLiteral(StringLiteral stringLiteral)
    {
        return TypeInformation.String();
    }

    public object VisitStructDeclaration(StructDeclaration structDeclaration)
    {
        // NOTE: this cannot happen. The parser makes sure this doesn't happen.
        throw new NotImplementedException();
    }

    public object VisitStructFieldDeclaration(StructFieldDeclaration structFieldDeclaration)
    {
        // NOTE: this cannot happen. The parser makes sure this doesn't happen.
        throw new NotImplementedException();
    }

    public object VisitStructInitialization(StructInitializationExpression structInitializationExpression)
    {
        var typename = structInitializationExpression.Identifier;
        var declarations = _context.GetStorageDeclarationsRegistry();

        var declaration = declarations.GetDeclarationOf(typename) ?? throw Error(GetErrorBuilder()
            .WithCode(LdErrorCode.UndefinedType)
            .WithSourceLocation(structInitializationExpression.Location)
            .WithMessage($"the type \"{typename}\" is not defined.")
            .WithNote("cannot initialize a struct that does not exist.")
            .Build());

        if (declaration is not StructDeclaration @struct)
        {
            throw Error(GetErrorBuilder()
                .WithCode(LdErrorCode.EnumInitializedAsStruct)
                .WithSourceLocation(structInitializationExpression.Location)
                .WithMessage($"cannot initialize an enum with struct initialization syntax.")
                .WithNote("enums are initialized like this: Enum::Variant(...)")
                .WithNote("structs are initialized like this: Struct { field: data }")
                .Build());
        }

        return CalculateStructSize(_context.GetDeclarations(), @struct);
    }

    public object VisitSubtractionExpression(SubtractionExpression subtractionExpression)
    {
        return subtractionExpression.Right.Visit(this);
    }

    private ErrorBuilder GetErrorBuilder()
    {
        return new ErrorBuilder(_source);
    }

    private Exception Error(ErrorMessage message)
    {
        ErrorHandler.QueueNow(message);
        ErrorHandler.DisplayThenExitIfAny();
        return new();
    }

    public TypeInformation CalculateStructSize(StorageDeclarationsRegistry registry, StructDeclaration declaration)
    {
        uint size = 0;
        foreach (var property in declaration.Fields)
        {
            var type = CalculateType(property.Location, property.Type.Name);
            size += type.Size;
        }

        return new TypeInformation(declaration.Identifier, size);
    }

    public TypeInformation CalculateType(SourceLocation location, string identifier)
    {
        if (TypeInformation.IsBuiltinTypeIdentifier(identifier)) {
            return TypeInformation.GetBuiltinFrom(identifier);
        }

        var possibleUdType = _context.GetDeclarations().GetDeclarationOf(identifier)
            ?? throw Error(GetErrorBuilder()
                .WithSourceLocation(location)
                .WithCode(LdErrorCode.UndefinedType)
                .WithMessage($"undefined type \"{identifier}\"")
                .Build());

        if (possibleUdType is StructDeclaration @struct)
            return CalculateStructSize(_context.GetDeclarations(), @struct);
        if (possibleUdType is EnumDeclaration @enum)
            return CalculateEnumSize(_context.GetDeclarations(), @enum);

        throw new NotImplementedException($"the declaration of type \"{possibleUdType.GetType().Name}\" is unhandled.");
    }

    public TypeInformation CalculateEnumSize(StorageDeclarationsRegistry registry, EnumDeclaration @enum)
    {
        if (@enum.Variants is null || @enum.Variants.Count == 0) {
            return new TypeInformation(@enum.Identifier, 0);
        }
        uint size = 0;

        foreach (var variant in @enum.Variants)
        {
            if (variant.TaggedTypes is null || variant.TaggedTypes.Count == 0)
            {
                continue;
            }

            variant.TaggedTypes.ForEach(x => size += CalculateType(variant.Location, x.Name).Size);
        }

        return new TypeInformation(@enum.Identifier, size);
    }

    public void ReloadScope(ScopeInfo scope)
    {
        _scope = scope;
    }
}