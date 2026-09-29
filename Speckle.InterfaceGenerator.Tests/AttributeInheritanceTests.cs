using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Xunit;

namespace Speckle.InterfaceGenerator.Tests;

public class AttributeInheritanceTests
{
    private static MethodInfo GetMethod(string name) =>
        typeof(IAttributesTestService).GetMethod(name) ?? throw new InvalidOperationException();

    private static PropertyInfo GetProperty(string name) =>
        typeof(IAttributesTestService).GetProperty(name) ?? throw new InvalidOperationException();

    [Fact]
    public void ObsoleteClass_CopiedToInterface()
    {
#pragma warning disable CS0618
        var attribute =
            typeof(IObsoleteAttributesTestService).GetCustomAttribute<ObsoleteAttribute>();
#pragma warning restore CS0618

        attribute.Should().NotBeNull();
        attribute!.Message.Should().Be("gone");
    }

    [Fact]
    public void ObsoleteMethod_CopiedWithNamedArgs()
    {
        var attribute = GetMethod("OldMethod").GetCustomAttribute<ObsoleteAttribute>();

        attribute.Should().NotBeNull();
        attribute!.Message.Should().Be("old");
        attribute.DiagnosticId.Should().Be("X1");
    }

    [Fact]
    public void NotNullWhenParam_Copied()
    {
        var attribute = GetMethod(nameof(AttributesTestService.TryGet))
            .GetParameters()
            .Single()
            .GetCustomAttribute<NotNullWhenAttribute>();

        attribute.Should().NotBeNull();
        attribute!.ReturnValue.Should().BeTrue();
    }

    [Fact]
    public void ReturnAttribute_Copied()
    {
        var attribute = GetMethod(nameof(AttributesTestService.Echo))
            .ReturnParameter.GetCustomAttribute<NotNullIfNotNullAttribute>();

        attribute.Should().NotBeNull();
        attribute!.ParameterName.Should().Be("input");
    }

    [Fact]
    public void DoesNotReturn_Copied()
    {
        GetMethod(nameof(AttributesTestService.Fail))
            .GetCustomAttribute<DoesNotReturnAttribute>()
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void CallerMemberNameParam_Copied()
    {
        var param = GetMethod(nameof(AttributesTestService.Log)).GetParameters()[1];

        param.GetCustomAttribute<CallerMemberNameAttribute>().Should().NotBeNull();
        param.DefaultValue.Should().Be("");
    }

    [Fact]
    public void EnumArgument_Copied()
    {
        var attribute = GetMethod(nameof(AttributesTestService.Hidden))
            .GetCustomAttribute<EditorBrowsableAttribute>();

        attribute.Should().NotBeNull();
        attribute!.State.Should().Be(EditorBrowsableState.Never);
    }

    [Fact]
    public void PropertyAttributes_Copied()
    {
        GetProperty(nameof(AttributesTestService.Prop))
            .GetMethod!.ReturnParameter.GetCustomAttribute<MaybeNullAttribute>()
            .Should()
            .NotBeNull();

        GetProperty("OldProp").GetCustomAttribute<ObsoleteAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void IndexerParamAttribute_Copied()
    {
        typeof(IAttributesTestService)
            .GetProperties()
            .Single(x => x.GetIndexParameters().Length == 1)
            .GetIndexParameters()
            .Single()
            .GetCustomAttribute<AllowNullAttribute>()
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void NonAllowlistedAttribute_NotCopied()
    {
        GetMethod(nameof(AttributesTestService.Stepped))
            .GetCustomAttribute<DebuggerStepThroughAttribute>()
            .Should()
            .BeNull();
    }
}

[Obsolete]
internal class ObsoleteType { }

[Obsolete("gone")]
[GenerateAutoInterface]
internal class ObsoleteAttributesTestService : IObsoleteAttributesTestService
{
    public void Method(ObsoleteType value) { }
}

[GenerateAutoInterface]
internal class AttributesTestService : IAttributesTestService
{
    [Obsolete("old", DiagnosticId = "X1")]
    public void OldMethod(ObsoleteType value) { }

    public bool TryGet([NotNullWhen(true)] out string? value)
    {
        value = null;
        return false;
    }

    [return: NotNullIfNotNull(nameof(input))]
    public string? Echo(string? input) => input;

    [DoesNotReturn]
    public void Fail() => throw new InvalidOperationException();

    public void Log(string message, [CallerMemberName] string caller = "") { }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Hidden() { }

    [DebuggerStepThrough]
    public void Stepped() { }

    [MaybeNull]
    public string Prop { get; set; } = "";

    [Obsolete]
    public int OldProp { get; set; }

    public string this[[AllowNull] string key]
    {
        get => "";
        set { }
    }
}
