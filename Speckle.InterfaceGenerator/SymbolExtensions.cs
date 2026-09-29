using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Speckle.InterfaceGenerator;

internal static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat TYPE_FORMAT =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

    public static string ToTypeReference(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(TYPE_FORMAT);

    public static bool HasAttribute(this ISymbol symbol, string attributeMetadataName) =>
        symbol
            .GetAttributes()
            .Any(x => x.AttributeClass?.ToDisplayString() == attributeMetadataName);

    //Ref: https://stackoverflow.com/questions/27105909/get-fully-qualified-metadata-name-in-roslyn
    public static string GetFullMetadataName(this ISymbol symbol, bool useNameWhenNotFound = false)
    {
        if (IsRootNamespace(symbol))
        {
            return useNameWhenNotFound ? symbol.Name : string.Empty;
        }

        var stringBuilder = new StringBuilder(symbol.MetadataName);
        var last = symbol;

        symbol = symbol.ContainingSymbol;

        while (!IsRootNamespace(symbol))
        {
            if (symbol is ITypeSymbol && last is ITypeSymbol)
            {
                stringBuilder.Insert(0, '+');
            }
            else
            {
                stringBuilder.Insert(0, '.');
            }

            stringBuilder.Insert(0, symbol.MetadataName);
            symbol = symbol.ContainingSymbol;
        }

        var retVal = stringBuilder.ToString();
        if (string.IsNullOrWhiteSpace(retVal) && useNameWhenNotFound)
        {
            return symbol.Name;
        }

        return retVal;
    }

    private static bool IsRootNamespace(ISymbol symbol)
    {
        return symbol is INamespaceSymbol { IsGlobalNamespace: true };
    }
}
