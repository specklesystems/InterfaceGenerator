using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Speckle.InterfaceGenerator;

internal static class AttributeWriterExtensions
{
    private static readonly HashSet<string> COPIED_ATTRIBUTES =
    [
        "System.ObsoleteAttribute",
        "System.ComponentModel.EditorBrowsableAttribute",
        "System.Diagnostics.CodeAnalysis.ExperimentalAttribute",
        "System.Diagnostics.Contracts.PureAttribute",
        "System.Diagnostics.CodeAnalysis.AllowNullAttribute",
        "System.Diagnostics.CodeAnalysis.DisallowNullAttribute",
        "System.Diagnostics.CodeAnalysis.MaybeNullAttribute",
        "System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute",
        "System.Diagnostics.CodeAnalysis.NotNullAttribute",
        "System.Diagnostics.CodeAnalysis.NotNullWhenAttribute",
        "System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute",
        "System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute",
        "System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute",
        "System.Runtime.CompilerServices.CallerMemberNameAttribute",
        "System.Runtime.CompilerServices.CallerFilePathAttribute",
        "System.Runtime.CompilerServices.CallerLineNumberAttribute",
        "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute",
    ];

    public static void WriteAttributes(
        this TextWriter writer,
        IEnumerable<AttributeData> attributes,
        AttributeTargets target
    )
    {
        foreach (var attribute in attributes)
        {
            if (
                attribute.AttributeClass is not { } attributeClass
                || !ShouldCopy(attributeClass, target)
            )
            {
                continue;
            }

            writer.Write(target == AttributeTargets.ReturnValue ? "[return: " : "[");
            writer.Write(attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

            var arguments = attribute
                .ConstructorArguments.Select(FormatTypedConstant)
                .Concat(
                    attribute.NamedArguments.Select(x =>
                        $"{x.Key} = {FormatTypedConstant(x.Value)}"
                    )
                )
                .ToList();

            if (arguments.Count > 0)
            {
                writer.Write("(");
                writer.WriteJoin(", ", arguments);
                writer.Write(")");
            }

            writer.Write("]");

            if (target == AttributeTargets.Parameter)
            {
                writer.Write(" ");
            }
            else
            {
                writer.WriteLine();
            }
        }
    }

    private static bool ShouldCopy(INamedTypeSymbol attributeClass, AttributeTargets target) =>
        attributeClass.TypeKind != TypeKind.Error
        && COPIED_ATTRIBUTES.Contains(
            $"{attributeClass.ContainingNamespace.ToDisplayString()}.{attributeClass.MetadataName}"
        )
        && (GetValidTargets(attributeClass) & target) != 0;

    private static AttributeTargets GetValidTargets(INamedTypeSymbol attributeClass)
    {
        var usage = attributeClass
            .GetAttributes()
            .FirstOrDefault(x =>
                x.AttributeClass?.ToDisplayString() == "System.AttributeUsageAttribute"
            );

        return usage?.ConstructorArguments.FirstOrDefault().Value is int validOn
            ? (AttributeTargets)validOn
            : AttributeTargets.All;
    }

    private static string FormatTypedConstant(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        return constant.Kind switch
        {
            TypedConstantKind.Primitive => FormatPrimitive(constant.Value),
            TypedConstantKind.Enum =>
                $"({constant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({FormatPrimitive(constant.Value)})",
            _ => throw new NotSupportedException(
                $"Attribute argument of kind {constant.Kind} is not supported"
            ),
        };
    }

    private static string FormatPrimitive(object? value) =>
        SymbolDisplay.FormatPrimitive(value!, quoteStrings: true, useHexadecimalNumbers: false);
}
