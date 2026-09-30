using System;
using System.Globalization;
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

    [Fact]
    public void EnumDefaultValue_GeneratesCompilableInterface()
    {
        const string source = """
            namespace Sample
            {
                public enum LogLevel { Debug, Info, Warning }

                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    public void Log(LogLevel level = LogLevel.Warning) { }

                    public void LogMaybe(LogLevel? level = LogLevel.Info) { }
                }
            }
            """;

        GetProblems(source).Should().BeEmpty();
    }

    [Fact]
    public void StringAndCharDefaultValues_AreEscaped()
    {
        const string source = """
            namespace Sample
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    public void Quote(string text = "say \"hi\"") { }

                    public void Path(string path = @"C:\temp") { }

                    public void Char(char c = '\'') { }
                }
            }
            """;

        var (problems, generated) = RunGenerator(source);

        problems.Should().BeEmpty();
        generated.Should().Contain("""string text = "say \"hi\"" """.TrimEnd());
        generated.Should().Contain("""string path = "C:\\temp" """.TrimEnd());
        generated.Should().Contain("""char c = '\''""");
    }

    [Fact]
    public void NumericDefaultValues_AreCultureInvariant()
    {
        const string source = """
            namespace Sample
            {
                [Speckle.InterfaceGenerator.GenerateAutoInterface]
                public class Service : IService
                {
                    public void Scale(double factor = 1.5, float ratio = 0.25f, decimal price = 9.99m) { }
                }
            }
            """;

        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            var (problems, generated) = RunGenerator(source);

            problems.Should().BeEmpty();
            generated
                .Should()
                .Contain("double factor = 1.5d, float ratio = 0.25f, decimal price = 9.99m");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private const string CACHED_SERVICE_SOURCE = """
        namespace Sample
        {
            [Speckle.InterfaceGenerator.GenerateAutoInterface]
            public class Service : IService
            {
                public int Get() => 0;
            }
        }
        """;

    private const string CACHED_OTHER_SOURCE = """
        namespace Sample
        {
            public class Other
            {
                public int Value() => 1;
            }
        }
        """;

    [Fact]
    public void UnrelatedEdit_ReusesCachedInterfaces()
    {
        var service = CSharpSyntaxTree.ParseText(CACHED_SERVICE_SOURCE, PARSE_OPTIONS);
        var other = CSharpSyntaxTree.ParseText(CACHED_OTHER_SOURCE, PARSE_OPTIONS);
        var compilation = CreateCompilation(service, other);
        var driver = CreateTrackingDriver().RunGenerators(compilation);

        var edited = compilation.ReplaceSyntaxTree(
            other,
            CSharpSyntaxTree.ParseText(CACHED_OTHER_SOURCE.Replace("=> 1", "=> 2"), PARSE_OPTIONS)
        );
        var result = driver.RunGenerators(edited).GetRunResult().Results.Single();

        result
            .TrackedSteps["RenderInterface"]
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .Should()
            .OnlyContain(x =>
                x == IncrementalStepRunReason.Cached || x == IncrementalStepRunReason.Unchanged
            );
        result
            .TrackedOutputSteps.SelectMany(x => x.Value)
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .Should()
            .OnlyContain(x => x == IncrementalStepRunReason.Cached);
    }

    [Fact]
    public void AttributedClassEdit_RegeneratesInterface()
    {
        var service = CSharpSyntaxTree.ParseText(CACHED_SERVICE_SOURCE, PARSE_OPTIONS);
        var compilation = CreateCompilation(service);
        var driver = CreateTrackingDriver().RunGenerators(compilation);

        var edited = compilation.ReplaceSyntaxTree(
            service,
            CSharpSyntaxTree.ParseText(
                CACHED_SERVICE_SOURCE.Replace("public int Get()", "public long Get()"),
                PARSE_OPTIONS
            )
        );
        var result = driver.RunGenerators(edited).GetRunResult().Results.Single();

        result
            .TrackedSteps["RenderInterface"]
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .Should()
            .Equal(IncrementalStepRunReason.Modified);
    }

    private static readonly CSharpParseOptions PARSE_OPTIONS = new(LanguageVersion.Latest);

    private static GeneratorDriver CreateTrackingDriver() =>
        CSharpGeneratorDriver.Create(
            [new AutoInterfaceGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true
            )
        );

    private static CSharpCompilation CreateCompilation(params SyntaxTree[] syntaxTrees)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(x => MetadataReference.CreateFromFile(x));

        return CSharpCompilation.Create(
            nameof(GeneratorDriverTests),
            syntaxTrees,
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable
            )
        );
    }

    private static string[] GetProblems(string source) => RunGenerator(source).Problems;

    private static (string[] Problems, string Generated) RunGenerator(string source)
    {
        var compilation = CreateCompilation(CSharpSyntaxTree.ParseText(source, PARSE_OPTIONS));

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
