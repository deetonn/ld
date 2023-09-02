
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
}
