using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Speckle.InterfaceGenerator;

[Generator]
public class AutoInterfaceGenerator : IIncrementalGenerator
{
    private const string GENERATE_AUTO_INTERFACE_METADATA_NAME =
        $"{Attributes.ATTRIBUTES_NAMESPACE}.{Attributes.GENERATE_AUTO_INTERFACE_CLASSNAME}";

    private const string AUTO_INTERFACE_IGNORE_METADATA_NAME =
        $"{Attributes.ATTRIBUTES_NAMESPACE}.{Attributes.AUTO_INTERFACE_IGNORE_ATTRIBUTE_CLASSNAME}";

    private static readonly DiagnosticDescriptor EXCEPTION_DESCRIPTOR = new(
        "SIG0001",
        "Exception thrown in InterfaceGenerator",
        "{0}",
        "Speckle.InterfaceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.AnalyzerException
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
            ctx.AddSource(
                $"{Attributes.GENERATE_AUTO_INTERFACE_CLASSNAME}.g.cs",
                SourceText.From(Attributes.ATTRIBUTES_SOURCE_CODE, Encoding.UTF8)
            )
        );

        var interfaces = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                GENERATE_AUTO_INTERFACE_METADATA_NAME,
                static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                static (ctx, _) => RenderInterface(ctx)
            )
            .WithTrackingName("RenderInterface");

        context.RegisterSourceOutput(interfaces, static (ctx, source) => AddInterface(ctx, source));
    }

    private static InterfaceSource RenderInterface(GeneratorAttributeSyntaxContext context)
    {
        var implTypeSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var hintName =
            $"{implTypeSymbol.GetFullMetadataName(useNameWhenNotFound: true)}_AutoInterface.g.cs";

        try
        {
            var source = GenerateInterfaceCode(implTypeSymbol, context.Attributes.Single());
            return new InterfaceSource(hintName, source, null);
        }
        catch (Exception exception)
        {
            var error =
                $"{exception.GetType().FullName} {exception.Message} {exception.StackTrace?.Trim()}";
            return new InterfaceSource(hintName, null, error);
        }
    }

    private static void AddInterface(SourceProductionContext context, InterfaceSource source)
    {
        if (source.Source is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(EXCEPTION_DESCRIPTOR, null, source.Error));
            return;
        }

        context.AddSource(source.HintName, SourceText.From(source.Source, Encoding.UTF8));
    }

    private static string InferVisibilityModifier(
        ISymbol implTypeSymbol,
        AttributeData attributeData
    )
    {
        var result = attributeData.GetNamedParamValue(Attributes.VISIBILITY_MODIFIER_PROP_NAME);
        if (!string.IsNullOrEmpty(result))
        {
            return result ?? throw new NullReferenceException("result is null");
        }

        return implTypeSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            _ => "internal",
        };
    }

    private static string InferInterfaceName(ISymbol implTypeSymbol, AttributeData attributeData)
    {
        return attributeData.GetNamedParamValue(Attributes.INTERFACE_NAME_PROP_NAME)
            ?? $"I{implTypeSymbol.Name}";
    }

    private static string GenerateInterfaceCode(
        INamedTypeSymbol implTypeSymbol,
        AttributeData attributeData
    )
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var codeWriter = new IndentedTextWriter(stringWriter, "    ");

        var namespaceName = implTypeSymbol.ContainingNamespace.ToDisplayString();
        var interfaceName = InferInterfaceName(implTypeSymbol, attributeData);
        var visibilityModifier = InferVisibilityModifier(implTypeSymbol, attributeData);

        //https://stackoverflow.com/questions/55492214/the-annotation-for-nullable-reference-types-should-only-be-used-in-code-within-a fix for nullable

        codeWriter.WriteLine("#nullable enable");
        codeWriter.WriteLine("namespace {0}", namespaceName);
        codeWriter.WriteLine("{");

        ++codeWriter.Indent;
        WriteSymbolDocsIfPresent(codeWriter, implTypeSymbol);
        codeWriter.WriteAttributes(implTypeSymbol.GetAttributes(), AttributeTargets.Interface);
        codeWriter.Write("{0} partial interface {1}", visibilityModifier, interfaceName);
        WriteTypeGenericsIfNeeded(codeWriter, implTypeSymbol);
        codeWriter.WriteLine();
        codeWriter.WriteLine("{");

        ++codeWriter.Indent;
        GenerateInterfaceMemberDefinitions(codeWriter, implTypeSymbol);
        --codeWriter.Indent;

        codeWriter.WriteLine("}");
        --codeWriter.Indent;

        codeWriter.WriteLine("}");
        codeWriter.WriteLine("#nullable restore");

        codeWriter.Flush();
        return stringWriter.ToString();
    }

    private static void WriteTypeGenericsIfNeeded(
        TextWriter writer,
        INamedTypeSymbol implTypeSymbol
    )
    {
        if (!implTypeSymbol.IsGenericType)
        {
            return;
        }

        writer.Write("<");
        writer.WriteJoin(", ", implTypeSymbol.TypeParameters.Select(x => x.Name));
        writer.Write(">");

        WriteTypeParameterConstraints(writer, implTypeSymbol.TypeParameters);
    }

    private static void GenerateInterfaceMemberDefinitions(
        TextWriter writer,
        INamedTypeSymbol implTypeSymbol
    )
    {
        foreach (var member in implTypeSymbol.GetMembers())
        {
            if (
                member.DeclaredAccessibility != Accessibility.Public
                || member.HasAttribute(AUTO_INTERFACE_IGNORE_METADATA_NAME)
            )
            {
                continue;
            }

            GenerateInterfaceMemberDefinition(writer, member);
        }
    }

    private static void GenerateInterfaceMemberDefinition(TextWriter writer, ISymbol member)
    {
        switch (member)
        {
            case IPropertySymbol propertySymbol:
                GeneratePropertyDefinition(writer, propertySymbol);
                break;
            case IMethodSymbol methodSymbol:
                GenerateMethodDefinition(writer, methodSymbol);
                break;
        }
    }

    private static void WriteSymbolDocsIfPresent(TextWriter writer, ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml))
        {
            return;
        }

        // omit the fist and last lines to skip the <member> tag

        var reader = new StringReader(xml);
        var lines = new List<string>();

        while (true)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            lines.Add(line);
        }

        for (var i = 1; i < lines.Count - 1; i++)
        {
            var line = lines[i].TrimStart(); // for some reason, 4 spaces are inserted to the beginning of the line
            writer.WriteLine("/// {0}", line);
        }
    }

    private static bool IsPublicOrInternal(ISymbol symbol)
    {
        return symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
    }

    private static void GeneratePropertyDefinition(
        TextWriter writer,
        IPropertySymbol propertySymbol
    )
    {
        if (propertySymbol.IsStatic)
        {
            return;
        }

        var hasPublicGetter =
            propertySymbol.GetMethod is not null && IsPublicOrInternal(propertySymbol.GetMethod);

        var hasPublicSetter =
            propertySymbol.SetMethod is not null && IsPublicOrInternal(propertySymbol.SetMethod);

        if (!hasPublicGetter && !hasPublicSetter)
        {
            return;
        }

        WriteSymbolDocsIfPresent(writer, propertySymbol);
        writer.WriteAttributes(propertySymbol.GetAttributes(), AttributeTargets.Property);

        if (propertySymbol.IsIndexer)
        {
            writer.Write("{0} this[", propertySymbol.Type.ToTypeReference());
            writer.WriteJoin(", ", propertySymbol.Parameters, WriteMethodParam);
            writer.Write("] ");
        }
        else
        {
            writer.Write("{0} {1} ", propertySymbol.Type.ToTypeReference(), propertySymbol.Name);
        }

        writer.Write("{ ");

        if (hasPublicGetter)
        {
            writer.Write("get; ");
        }

        if (hasPublicSetter)
        {
            if (propertySymbol.SetMethod?.IsInitOnly ?? false)
            {
                writer.Write("init; ");
            }
            else
            {
                writer.Write("set; ");
            }
        }

        writer.WriteLine("}");
    }

    private static void GenerateMethodDefinition(TextWriter writer, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.MethodKind != MethodKind.Ordinary || methodSymbol.IsStatic)
        {
            return;
        }

        if (methodSymbol.IsImplicitlyDeclared && methodSymbol.Name != "Deconstruct")
        {
            // omit methods that are auto generated by the compiler (eg. record's methods),
            // except for the record Deconstruct method
            return;
        }

        WriteSymbolDocsIfPresent(writer, methodSymbol);
        writer.WriteAttributes(methodSymbol.GetAttributes(), AttributeTargets.Method);
        writer.WriteAttributes(
            methodSymbol.GetReturnTypeAttributes(),
            AttributeTargets.ReturnValue
        );

        writer.Write("{0} {1}", methodSymbol.ReturnType.ToTypeReference(), methodSymbol.Name);

        if (methodSymbol.IsGenericMethod)
        {
            writer.Write("<");
            writer.WriteJoin(", ", methodSymbol.TypeParameters.Select(x => x.Name));
            writer.Write(">");
        }

        writer.Write("(");
        writer.WriteJoin(", ", methodSymbol.Parameters, WriteMethodParam);

        writer.Write(")");

        if (methodSymbol.IsGenericMethod)
        {
            WriteTypeParameterConstraints(writer, methodSymbol.TypeParameters);
        }

        writer.WriteLine(";");
    }

    private static void WriteMethodParam(TextWriter writer, IParameterSymbol param)
    {
        writer.WriteAttributes(param.GetAttributes(), AttributeTargets.Parameter);

        if (param.IsParams)
        {
            writer.Write("params ");
        }

        switch (param.RefKind)
        {
            case RefKind.Ref:
                writer.Write("ref ");
                break;
            case RefKind.Out:
                writer.Write("out ");
                break;
            case RefKind.In:
                writer.Write("in ");
                break;
        }

        writer.Write(param.Type.ToTypeReference());
        writer.Write(" ");

        if (StringExtensions.IsCSharpKeyword(param.Name))
        {
            writer.Write("@");
        }

        writer.Write(param.Name);

        if (param.HasExplicitDefaultValue)
        {
            WriteParamExplicitDefaultValue(writer, param);
        }
    }

    private static void WriteParamExplicitDefaultValue(TextWriter writer, IParameterSymbol param)
    {
        writer.Write(" = ");
        writer.Write(FormatDefaultValue(param.Type, param.ExplicitDefaultValue));
    }

    private static string FormatDefaultValue(ITypeSymbol type, object? value)
    {
        if (value is null)
        {
            return "default";
        }

        var valueType = type
            is INamedTypeSymbol
            {
                OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
            } nullable
            ? nullable.TypeArguments[0]
            : type;

        if (valueType.TypeKind == TypeKind.Enum)
        {
            return LiteralFormatter.FormatEnum(valueType, value);
        }

        var literal = LiteralFormatter.FormatPrimitive(value);
        return valueType.SpecialType switch
        {
            SpecialType.System_Single => literal + "f",
            SpecialType.System_Double => literal + "d",
            SpecialType.System_Decimal => literal + "m",
            _ => literal,
        };
    }

    private static void WriteTypeParameterConstraints(
        TextWriter writer,
        IEnumerable<ITypeParameterSymbol> typeParameters
    )
    {
        foreach (var typeParameter in typeParameters)
        {
            var constraints = typeParameter.EnumGenericConstraints().ToList();
            if (constraints.Count == 0)
            {
                continue;
            }

            writer.Write(" where {0} : ", typeParameter.Name);
            writer.WriteJoin(", ", constraints);
        }
    }
}
