
using System.Numerics;

namespace Language.Api;

public class TypeInformation
{
    public string Name { get; internal set; }
    public List<TypeInformation>? Generics { get; internal set; }

    public uint Size { get; set; } = 0;

    public bool IsArray { get; set; } = false;

    public TypeInformation(string name, uint size)
    {
        Name = name;
    }
    public TypeInformation(string name, List<TypeInformation>? generics)
    {
        Name = name;
        Generics = generics;
    }

    public static bool IsBuiltinTypeIdentifier(string ident)
        => ident == "u32" || ident == "i32"
        || ident == "u64" || ident == "i64"
        || ident == "bool" || ident == "String"
        || ident == "f32" || ident == "f64"
        || ident == "i8" || ident == "u8"
        || ident == "i16" || ident == "u16"
        || ident == "char";

    public bool IsMathematicallySupported()
    {
        if (!IsBuiltinType())
            return false;
        if (IsArray)
            return false;
        if (Name == "String" || Name == "bool")
            return false;
        return true;
    }

    public static TypeInformation GetBuiltinFrom(string ident)
    {
        if (ident == "u32") return U32();
        if (ident == "i32") return I32();
        if (ident == "u64") return U64();
        if (ident == "i64") return I64();
        if (ident == "bool") return Bool();
        if (ident == "String") return String();
        if (ident == "f32") return F32();
        if (ident == "f64") return F64();
        if (ident == "i8") return I8();
        if (ident == "u8") return U8();
        if (ident == "i16") return I16();
        if (ident == "u16") return U16();
        if (ident == "char") return Char();
        return Void();
    }

    public bool SignatureMatches(TypeInformation other)
    {
        if (other.Name != Name) return false;
        if (other.IsArray && !IsArray) return false;
        if (other.Generics is null && Generics is not null) return false;
        if (Generics is null && other.Generics is not null) return false;
        if (Generics is null && other.Generics is null) return true;
        if (Generics.Count != other.Generics.Count) return false;

        for (int i = 0; i < Generics.Count; ++i)
        {
            if (!Generics[i].SignatureMatches(other.Generics[i]))
                return false;
        }

        return true;
    }

    public bool IsBuiltinType()
        => IsBuiltinTypeIdentifier(Name);

    public static TypeInformation Bool() => new("bool", 1);
    public static TypeInformation F32() => new("f32", 4);
    public static TypeInformation F64() => new("f64", 8);
    public static TypeInformation U32() => new("u32", 4);
    public static TypeInformation U64() => new("u64", 8);
    public static TypeInformation I32() => new("i32", 4);
    public static TypeInformation I64() => new("i64", 4);
    public static TypeInformation U16() => new("u16", 2);
    public static TypeInformation I16() => new("i16", 2);
    public static TypeInformation U8() => new("u8", 1);
    public static TypeInformation I8() => new("i8", 1);
    public static TypeInformation Char() => new("char", 1);
    public static TypeInformation Void() => new("void", 0);

    public static TypeInformation SizeT() => new("size_t", Environment.Is64BitProcess ? 8u : 4u);

    // STRING_TYPE: size is set as 8 currently as the builtin will contain
    // a single field. That will be a pointer to an array of characters.
    public static TypeInformation String() => new("String", SizeT().Size);

    public static TypeInformation Array(TypeInformation inner, uint count)
        => new("array", inner.Size * count) { IsArray = true };

    public static bool operator==(TypeInformation left, TypeInformation right)
    {
        return left.SignatureMatches(right);
    }

    public static bool operator!=(TypeInformation left, TypeInformation right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return IsArray ? $"{Name}[]" : $"{Name}";
    }
}
