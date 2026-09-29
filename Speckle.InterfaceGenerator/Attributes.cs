namespace Speckle.InterfaceGenerator;

internal class Attributes
{
    public const string ATTRIBUTES_NAMESPACE = "Speckle.InterfaceGenerator";

    public const string GENERATE_AUTO_INTERFACE_CLASSNAME = "GenerateAutoInterfaceAttribute";
    public const string AUTO_INTERFACE_IGNORE_ATTRIBUTE_CLASSNAME = "AutoInterfaceIgnoreAttribute";

    public const string VISIBILITY_MODIFIER_PROP_NAME = "VisibilityModifier";
    public const string INTERFACE_NAME_PROP_NAME = "Name";

    public const string ATTRIBUTES_SOURCE_CODE = $$"""


        #pragma warning disable IDE0005
        using System;
        using System.Diagnostics;

        #nullable enable

        namespace {{ATTRIBUTES_NAMESPACE}}
        {
            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
            [Conditional("CodeGeneration")]
            internal sealed class {{GENERATE_AUTO_INTERFACE_CLASSNAME}} : Attribute
            {
                public string? {{VISIBILITY_MODIFIER_PROP_NAME}} { get; init; }
                public string? {{INTERFACE_NAME_PROP_NAME}} { get; init; }

                public {{GENERATE_AUTO_INTERFACE_CLASSNAME}}()
                {
                }
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false)]
            [Conditional("CodeGeneration")]
            internal sealed class {{AUTO_INTERFACE_IGNORE_ATTRIBUTE_CLASSNAME}} : Attribute
            {
            }
        }

        #pragma warning restore IDE0005

        """;
}
