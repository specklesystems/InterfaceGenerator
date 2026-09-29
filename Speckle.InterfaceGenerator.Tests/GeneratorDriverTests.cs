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
        const string source = """
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

        GetProblems(source).Should().BeEmpty();
    }

    [Fact]
    public void TypeInKeywordNamespace_GeneratesCompilableInterface()
    {
        const string source = """
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

        GetProblems(source).Should().BeEmpty();
    }

    [Fact]
    public void ClassInKeywordNamespace_GeneratesCompilableInterface()
    {
        const string source = """
            namespace Sample.@event
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    public int Get() => 0;
                }
            }
            """;

        GetProblems(source).Should().BeEmpty();
    }

    [Fact]
    public void GenericClassTypeParameter_GeneratesCompilableInterface()
    {
        const string source = """
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

        GetProblems(source).Should().BeEmpty();
    }

    [Fact]
    public void NullableTypeParameter_GeneratesMatchingInterface()
    {
        const string source = """
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

        GetProblems(source).Should().BeEmpty();
    }

    [Fact]
    public void TypeArgumentShadowedByNamespace_GeneratesCompilableInterface()
    {
        const string source = """
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

        GetProblems(source).Should().BeEmpty();
    }

    [Fact]
    public void GenericMethodConstraintAfterUnconstrainedParameter_GeneratesMatchingInterface()
    {
        const string source = """
            namespace Sample
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    public void Register<TKey, TValue>()
                        where TValue : class { }
                }
            }
            """;

        GetProblems(source).Should().BeEmpty();
    }

    [Fact]
    public void PureMethod_IsCopiedToInterface()
    {
        const string source = """
            namespace Sample
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    [System.Diagnostics.Contracts.Pure]
                    public int Get() => 0;
                }
            }
            """;

        var (problems, generated) = RunGenerator(source);

        problems.Should().BeEmpty();
        generated.Should().Contain("[global::System.Diagnostics.Contracts.PureAttribute]");
    }

    [Fact]
    public void PureClass_IsNotCopiedToInterface()
    {
        const string source = """
            namespace Sample
            {
                [System.Diagnostics.Contracts.Pure]
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    public int Get() => 0;
                }
            }
            """;

        var (problems, generated) = RunGenerator(source);

        problems.Should().BeEmpty();
        generated.Should().NotContain("PureAttribute");
    }

    [Fact]
    public void NestedNullableTypes_GenerateMatchingInterface()
    {
        const string source = """
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

        GetProblems(source).Should().BeEmpty();
    }

    private static string[] GetProblems(string source) => RunGenerator(source).Problems;

    private static (string[] Problems, string Generated) RunGenerator(string source)
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

        var driver = CSharpGeneratorDriver
            .Create(new AutoInterfaceGenerator())
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var output,
                out var generatorDiagnostics
            );

        var problems = generatorDiagnostics
            .Concat(output.GetDiagnostics())
            .Where(x => x.Severity >= DiagnosticSeverity.Warning)
            .Select(x => x.ToString())
            .ToArray();

        var generated = string.Concat(
            driver
                .GetRunResult()
                .GeneratedTrees.Where(x =>
                    x.FilePath.EndsWith("_AutoInterface.g.cs", StringComparison.Ordinal)
                )
                .Select(x => x.ToString())
        );

        return (problems, generated);
    }
}
