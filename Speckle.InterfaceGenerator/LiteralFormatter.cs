using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Speckle.InterfaceGenerator;

internal static class LiteralFormatter
{
    public static string FormatPrimitive(object value) =>
        SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false);

    public static string FormatEnum(ITypeSymbol enumType, object value) =>
        $"({enumType.ToTypeReference()})({FormatPrimitive(value)})";
}
