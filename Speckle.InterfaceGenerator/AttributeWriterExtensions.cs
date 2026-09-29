using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Speckle.InterfaceGenerator;

internal static class AttributeWriterExtensions
{
    private static readonly HashSet<string> s_copiedAttributes =
    [
        "System.ObsoleteAttribute",
        "System.ComponentModel.EditorBrowsableAttribute",
        "System.Diagnostics.CodeAnalysis.ExperimentalAttribute",
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
        string? target = null,
        bool inline = false
    )
    {
        foreach (var attribute in attributes)
        {
            var attributeClass = attribute.AttributeClass;
            if (
                attributeClass is null
                || attributeClass.TypeKind == TypeKind.Error
                || !s_copiedAttributes.Contains(
                    $"{attributeClass.ContainingNamespace.ToDisplayString()}.{attributeClass.MetadataName}"
                )
            )
            {
                continue;
            }

            writer.Write("[");
            if (target is not null)
            {
                writer.Write("{0}: ", target);
            }

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

            if (inline)
            {
                writer.Write(" ");
            }
            else
            {
                writer.WriteLine();
            }
        }
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
            TypedConstantKind.Enum
                => $"({constant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({FormatPrimitive(constant.Value)})",
            _
                => throw new NotSupportedException(
                    $"Attribute argument of kind {constant.Kind} is not supported"
                ),
        };
    }

    private static string FormatPrimitive(object? value) =>
        SymbolDisplay.FormatPrimitive(value!, quoteStrings: true, useHexadecimalNumbers: false);
}
