using DestallMaterials.WheelProtection.Extensions.Objects;
using DestallMaterials.WheelProtection.Extensions.Strings;
using DestallMaterials.WheelProtection.Linq;
using Microsoft.CodeAnalysis;
using System.Collections;

namespace DestallMaterials.CodeGeneration.Utilities;

public static class SemanticsExtensions
{
    static SemanticsExtensions()
    {
        FractionalTypes = new Type[]
        {
                typeof(double), typeof(decimal), typeof(float)
        };
        WholeNumberTypes = new Type[]
        {
                typeof(uint), typeof(int), typeof(long), typeof(ulong), typeof(short), typeof(ushort), typeof(byte)
        };
        NumberTypes = WholeNumberTypes.Concat(FractionalTypes).ToArray();
        PrimitiveTypes = new Type[]
        {
                typeof(string), typeof(DateTime), typeof(bool)
        }.Union(NumberTypes).ToArray();
    }

    public static List<ITypeSymbol> AllBaseClassesHierarchy(this ITypeSymbol symbol)
    {
        var res = new List<ITypeSymbol>();
        var cur = symbol?.BaseType;
        while (cur != null)
        {
            res.Add(cur);
            cur = cur.BaseType;
        }
        return res;
    }

    public static List<INamedTypeSymbol> AllBaseClassesHierarchyIncludingSelf(this ITypeSymbol symbol)
    {
        var res = new List<INamedTypeSymbol>();
        var cur = symbol;
        while (cur != null && cur is INamedTypeSymbol nts)
        {
            res.Add(nts);
            cur = cur.BaseType;
        }
        return res;
    }


    public static bool IsPrimitive(this ITypeSymbol typeSymbol)
    {
        string fullTypeName = typeSymbol.ToDisplayString(_format);
        return fullTypeName.IsOneOf(PrimitiveTypes.Select(pt => pt.FullName));
    }

    public static bool IsCapital(this char c)
        => c.ToString().ToUpper() == c.ToString();

    public static bool IsPrimitiveOrEnumerableOfPrimitive(this ITypeSymbol type)
    {
        if (type.IsPrimitive())
        {
            return true;
        }
        if (type.IsSemanticEnumerable(out var underEnumerable))
        {
            return underEnumerable.IsPrimitive();
        }
        return false;
    }

    public static bool IsPrimitiveOrNullablePrimitive(this ITypeSymbol typeSymbol)
    {
        string fullTypeName = typeSymbol.ToFullDisplayString();
        return fullTypeName.IsOneOf(PrimitiveTypes.Select(pt => pt.FullName));
    }

    public static bool IsHeirOf(this ITypeSymbol source, ITypeSymbol parent)
    {
        var res = source.AllBaseClassesHierarchy().Any(bc => SymbolEqualityComparer.Default.Equals(bc, parent));
        return res;
    }

    static readonly SymbolDisplayFormat _format = new SymbolDisplayFormat(
                            SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                            SymbolDisplayGenericsOptions.IncludeTypeParameters
            );

    public static bool Is<T>(this ITypeSymbol typeSymbol)
           => typeSymbol.Is(typeof(T));

    public static bool Is(this ITypeSymbol typeSymbol, Type type)
    {
        if (!type.IsGenericType)
        {
            if (typeSymbol is null)
            {
                return false;
            }
            var signature = type.FullName;
            var result = typeSymbol.ToFullDisplayString() == signature;
            return result;
        }

        if (!typeSymbol.IsGenericType())
        {
            return false;
        }

        return typeSymbol.ToFullDisplayString().Replace("?", "") == type.ToDisplayString().Replace("?", "");
    }

    public static bool IsNumber(this ITypeSymbol typeSymbol)
        => NumberTypes.Any(nt => typeSymbol.Is(nt));

    public static readonly IEnumerable<Type> WholeNumberTypes;

    public static readonly IEnumerable<Type> FractionalTypes;

    public static readonly IEnumerable<Type> PrimitiveTypes;

    public static readonly IEnumerable<Type> NumberTypes;

    public static string ToFullDisplayString(this ISymbol symbol) => symbol.ToDisplayString(_format);

    public static bool RelatesToAttribute<TAttribute>(this ITypeSymbol symbol) where TAttribute : Attribute
        => symbol.RelatesToAttribute(typeof(TAttribute));

    public static bool RelatesToAttribute(this ITypeSymbol symbol, Type attribute)
        => symbol.RelatesToAttribute(attribute.FullName);

    public static bool RelatesToAttribute(this ITypeSymbol symbol, INamedTypeSymbol attribute)
        => symbol.RelatesToAttribute(attribute.ToDisplayString());

    public static bool RelatesToAttribute(this ITypeSymbol type, string attribute)
    {
        var allBases = type.AllBaseClassesHierarchyIncludingSelf().Concat(type.AllInterfaces);

        bool result = allBases.Any(c => c.GetAttributes().Any(a => a.AttributeClass.AllBaseClassesHierarchyIncludingSelf().Any(ac => ac.ToDisplayString() == attribute)));

        return result;
    }

    public static bool HasAttribute<TAttr>(this ISymbol symbol)
            where TAttr : Attribute => symbol.HasAttribute<TAttr>(out var _);

    public static bool HasAttribute(this ISymbol symbol, string attr)
            => symbol.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == attr);

    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
        => symbol.HasAttribute(attribute.ToDisplayString());

    public static bool HasAttribute<TAttr>(this ISymbol symbol, out AttributeData? attributeSymbol)
        where TAttr : Attribute
    {
        attributeSymbol = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToFullDisplayString() == typeof(TAttr).FullName);
        return attributeSymbol is not null;
    }

    public static IEnumerable<TypedConstant>? AttributeParameters<TAttribute>(this ISymbol symbol) where TAttribute : Attribute
    {
        if (!symbol.HasAttribute<TAttribute>())
        {
            return Enumerable.Empty<TypedConstant>();
        }
        var attributeData = symbol.GetAttributes().First(a => a.AttributeClass.ToFullDisplayString() == typeof(TAttribute).FullName);

        var result = attributeData.ConstructorArguments;

        return result;
    }

    public static ITypeSymbol GetTypeUnderTask(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return typeSymbol;
        }
        if (!namedTypeSymbol.ToFullDisplayString().StartsWith(typeof(Task).FullName) || namedTypeSymbol.ToFullDisplayString().StartsWith(typeof(ValueTask).FullName))
        {
            return namedTypeSymbol;
        }
        var result = namedTypeSymbol.TypeArguments.Single();
        return result;
    }

    public static bool IsEnumerable(this ITypeSymbol type, out ITypeSymbol enumerableItemType)
    {
        IEnumerable<INamedTypeSymbol> interfaces = type.AllInterfaces;
        if (type is INamedTypeSymbol namedTypeSymbol)
        {
            interfaces = interfaces.Append(namedTypeSymbol);
        }
        var enumerableInterface = interfaces.FirstOrDefault(i => i.ToFullDisplayString().StartsWith("System.Collections.Generic.IEnumerable<"));
        if (enumerableInterface != null)
        {
            enumerableItemType = enumerableInterface.TypeArguments.Single();
            return true;
        }
        else
        {
            enumerableItemType = null;
            return false;
        }
    }

    public static bool IsSemanticEnumerable(this ITypeSymbol type)
        => type.IsSemanticEnumerable(out var _);

    static readonly IEnumerable<string> ReadOnlyEnumerables = (
            "IReadOnlyList",
            "IEnumerable",
            "IReadOnlyCollection"
    )
        .Select(s => $"System.Collections.Generic.{s}")
        .ToArray();

    public static bool IsReadOnlyEnumerable(this ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol)
        {
            return false;
        }
        return new string(type.ToDisplayString().TakeWhile(c => c != '<').ToArray()).IsOneOf(ReadOnlyEnumerables);
    }

    public static bool IsArray(this ITypeSymbol typeSymbol) => typeSymbol.TypeKind == TypeKind.Array;

    public static bool IsImplementedByArray(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol.IsArray())
        {
            return true;
        }

        var nts = typeSymbol as INamedTypeSymbol;
        if (nts is null)
        {
            return false;
        }
        if (nts.TypeParameters.IsEmpty)
        {
            return typeSymbol.Is<IEnumerable>() || typeSymbol.Is<ICollection>() || typeSymbol.Is<IList>();
        }
        if (nts.TypeParameters.Length > 1)
        {
            return false;
        }
        var typeParameter = nts.TypeParameters.Single() as INamedTypeSymbol;
        if (typeParameter is null)
        {
            return false;
        }
        return ReadOnlyEnumerables.Select(e => $"{e}<{typeParameter.ToDisplayString()}>").Contains(typeSymbol.ToDisplayString());
    }

    public static bool IsSemanticEnumerable(this ITypeSymbol type, out ITypeSymbol underEnumerable)
    {
        if (type.Is<string>())
        {
            underEnumerable = null;
            return false;
        }
        return type.IsEnumerable(out underEnumerable);
    }

    public static bool IsGenericType(this ITypeSymbol type) => (type as INamedTypeSymbol)?.IsGenericType == true;

    public static string ToDisplayString(this Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName;
        }
        if (type.GenericTypeArguments.Length == 1)
        {
            if (type.FullName.StartsWith("System.Nullable`"))
            {
                return $"{type.GenericTypeArguments[0]}?";
            }
        }
        return $"{type.FullName.Split('`')[0]}<{type.GenericTypeArguments.Select(t => t.ToDisplayString()).Join(", ")}>";
    }

    public static bool IsEqualTo(this ITypeSymbol first, ITypeSymbol second)
        => first.ToDisplayString() == second.ToDisplayString();
}
