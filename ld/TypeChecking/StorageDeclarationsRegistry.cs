
using Language.Parsing.Productions;

namespace Language.TypeChecking;

/// <summary>
/// This class holds all declarations that are in any way
/// a storage type. As it stands, this is structs and enums.
/// </summary>
public class StorageDeclarationsRegistry
{
    private readonly List<Declaration> _declarations;

    public StorageDeclarationsRegistry(List<Declaration> declarations)
    {
        _declarations = declarations;
    }

    public Declaration? GetDeclarationOf(string ident)
    {
        foreach (var decl in _declarations)
        {
            if (decl is StructDeclaration @struct)
            {
                if (@struct.Identifier == ident)
                    return decl;
                return null;
            }
            if (decl is EnumDeclaration @enum)
            {
                if (@enum.Identifier == ident) return decl;
                return null;
            }
        }

        return null;
    }
}
