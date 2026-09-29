using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Speckle.InterfaceGenerator.Tests;

public class GeneratorDriverTests
{
    [Fact]
    public void TypeInRegularNamespace_GeneratesCompilableInterface()
    {
        const string SOURCE = """
            namespace Sample.Events
            {
                public class EventType { }
            }

            namespace Sample
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    public Events.EventType Get() => new();
                }
            }
            """;

        GetProblems(SOURCE).Should().BeEmpty();
    }

    [Fact]
    public void TypeInKeywordNamespace_GeneratesCompilableInterface()
    {
        const string SOURCE = """
            namespace Sample.@event
            {
                public class EventType { }
            }

            namespace Sample
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    public @event.EventType Get() => new();
                }
            }
            """;

        GetProblems(SOURCE).Should().BeEmpty();
    }

    [Fact]
    public void ClassInKeywordNamespace_GeneratesCompilableInterface()
    {
        const string SOURCE = """
            namespace Sample.@event
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    public int Get() => 0;
                }
            }
            """;

        GetProblems(SOURCE).Should().BeEmpty();
    }

    [Fact]
    public void GenericClassTypeParameter_GeneratesCompilableInterface()
    {
        const string SOURCE = """
            namespace Sample
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service<T> : IService<T>
                {
                    public T Get() => default!;

                    public void Set(T value) { }
                }
            }
            """;

        GetProblems(SOURCE).Should().BeEmpty();
    }

    [Fact]
    public void NullableTypeParameter_GeneratesMatchingInterface()
    {
        const string SOURCE = """
            namespace Sample
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service<T> : IService<T>
                    where T : class
                {
                    public T? Resolve(string name) => null;
                }
            }
            """;

        GetProblems(SOURCE).Should().BeEmpty();
    }

    [Fact]
    public void TypeArgumentShadowedByNamespace_GeneratesCompilableInterface()
    {
        const string SOURCE = """
            namespace Rhino.Geometry
            {
                public class Mesh { }
            }

            namespace Speckle.Converters.Rhino
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class MeshConverter : IMeshConverter
                {
                    public System.Collections.Generic.List<global::Rhino.Geometry.Mesh> Convert() => [];
                }
            }
            """;

        GetProblems(SOURCE).Should().BeEmpty();
    }

    [Fact]
    public void NestedNullableTypes_GenerateMatchingInterface()
    {
        const string SOURCE = """
            using System.Collections.Generic;
            using System.Threading.Tasks;

            namespace Sample
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    public Task<string?> Get() => Task.FromResult<string?>(null);

                    public void Set(List<string?> values) { }

                    public List<string?> Values { get; set; } = [];
                }
            }
            """;

        GetProblems(SOURCE).Should().BeEmpty();
    }

    private static string[] GetProblems(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(x => MetadataReference.CreateFromFile(x));

        var compilation = CSharpCompilation.Create(
            nameof(GeneratorDriverTests),
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable
            )
        );

        CSharpGeneratorDriver
            .Create(new AutoInterfaceGenerator())
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var output,
                out var generatorDiagnostics
            );

        return generatorDiagnostics
            .Concat(output.GetDiagnostics())
            .Where(x => x.Severity >= DiagnosticSeverity.Warning)
            .Select(x => x.ToString())
            .ToArray();
    }
}
