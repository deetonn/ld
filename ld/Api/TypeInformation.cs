
namespace Language.Api;

public class TypeInformation
{
    public string Name { get; internal set; }
    public List<TypeInformation>? Generics { get; internal set; }

    public TypeInformation(string name)
    {
        Name = name;
    }
    public TypeInformation(string name, List<TypeInformation>? generics)
    {
        Name = name;
        Generics = generics;
    }

    public bool IsBuiltinType()
        => Name == "u32" || Name == "i32"
        || Name == "u64" || Name == "i64"
        || Name == "bool" || Name == "String"
        || Name == "f32" || Name == "f64";
}
