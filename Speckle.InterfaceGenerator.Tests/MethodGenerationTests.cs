using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Speckle.InterfaceGenerator.Tests;

public class MethodGenerationTests
{
    private readonly IMethodsTestService _sut;

    public MethodGenerationTests()
    {
        _sut = new MethodsTestService();
    }

    [Fact]
    public void VoidMethod_IsImplemented()
    {
        var method =
            typeof(IMethodsTestService).GetMethod(nameof(MethodsTestService.VoidMethod))
            ?? throw new InvalidOperationException();

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(void));

        var parameters = method.GetParameters();
        parameters.Should().BeEmpty();

        _sut.VoidMethod();
    }

    [Fact]
    public void VoidMethodWithKeywordParam_IsImplemented()
    {
        var method =
            typeof(IMethodsTestService).GetMethod(
                nameof(MethodsTestService.VoidMethodWithKeywordParam)
            ) ?? throw new InvalidOperationException();

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(void));

        var parameters = method.GetParameters();
        parameters.Select(x => x.ParameterType).Should().AllBeEquivalentTo(typeof(string));
        parameters.Should().HaveCount(1);

        parameters[0].Name.Should().Be("void");

        _sut.VoidMethodWithKeywordParam("");
    }

    [Fact]
    public void VoidMethodWithParams_IsImplemented()
    {
        var method =
            typeof(IMethodsTestService).GetMethod(
                nameof(MethodsTestService.VoidMethodWithParams),
                [typeof(string), typeof(string)]
            ) ?? throw new InvalidOperationException();

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(void));

        var parameters = method.GetParameters();
        parameters.Select(x => x.ParameterType).Should().AllBeEquivalentTo(typeof(string));
        parameters.Should().HaveCount(2);

        _sut.VoidMethodWithParams(string.Empty, string.Empty);
    }

    [Fact]
    public void VoidMethodWithOutParam_IsImplemented()
    {
        var method =
            typeof(IMethodsTestService).GetMethod(
                nameof(MethodsTestService.VoidMethodWithOutParam),
                [typeof(string).MakeByRefType()]
            ) ?? throw new InvalidOperationException();

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(void));

        var parameters = method.GetParameters();
        parameters
            .Select(x => x.ParameterType)
            .Should()
            .AllBeEquivalentTo(typeof(string).MakeByRefType());
        parameters.Should().HaveCount(1);
        parameters[0].IsOut.Should().BeTrue();

        _sut.VoidMethodWithOutParam(out var _);
    }

    [Fact]
    public void VoidMethodWithInParam_IsImplemented()
    {
        var method =
            typeof(IMethodsTestService).GetMethod(
                nameof(MethodsTestService.VoidMethodWithInParam),
                [typeof(string).MakeByRefType()]
            ) ?? throw new InvalidOperationException();

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(void));

        var parameters = method.GetParameters();
        parameters
            .Select(x => x.ParameterType)
            .Should()
            .AllBeEquivalentTo(typeof(string).MakeByRefType());
        parameters.Should().HaveCount(1);
        parameters[0].IsIn.Should().BeTrue();

        var stub = string.Empty;
        _sut.VoidMethodWithInParam(in stub);
    }

    [Fact]
    public void VoidMethodWithRefParam_IsImplemented()
    {
        var method =
            typeof(IMethodsTestService).GetMethod(
                nameof(MethodsTestService.VoidMethodWithRefParam),
                [typeof(string).MakeByRefType()]
            ) ?? throw new InvalidOperationException();

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(void));

        var parameters = method.GetParameters();
        parameters
            .Select(x => x.ParameterType)
            .Should()
            .AllBeEquivalentTo(typeof(string).MakeByRefType());
        parameters.Should().HaveCount(1);
        parameters[0].IsIn.Should().BeFalse();
        parameters[0].IsOut.Should().BeFalse();

        var stub = string.Empty;
        _sut.VoidMethodWithRefParam(ref stub);
    }

    [Fact]
    public void StringMethod_IsImplemented()
    {
        var method =
            typeof(IMethodsTestService).GetMethod(nameof(MethodsTestService.StringMethod))
            ?? throw new InvalidOperationException();

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(string));

        var parameters = method.GetParameters();
        parameters.Should().BeEmpty();

        var _ = _sut.StringMethod();
    }

    [Fact]
    public void StringMethodNullable_IsImplemented()
    {
        var method =
            typeof(IMethodsTestService).GetMethod(nameof(MethodsTestService.StringMethodNullable))
            ?? throw new InvalidOperationException();

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(string));
        IsNullable(method.ReturnType).Should().BeTrue();

        var parameters = method.GetParameters();
        parameters.Should().BeEmpty();

        var _ = _sut.StringMethod();
    }

    private static bool IsNullable(Type type)
    {
        var nullableContextAttribute = type.GetCustomAttribute<NullableContextAttribute>();

        // NullableContextAttribute exists and has a flag indicating nullable annotations
        if (nullableContextAttribute != null && nullableContextAttribute.Flag == 1)
        {
            return true;
        }

        return false;
    }

    [Fact]
    public void GenericVoidMethod_IsImplemented()
    {
        var method = typeof(IMethodsTestService)
            .GetMethods()
            .First(x => x.Name == nameof(MethodsTestService.GenericVoidMethod));

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(void));

        method.GetParameters().Should().BeEmpty();

        var genericArgs = method.GetGenericArguments();
        genericArgs.Should().HaveCount(2);

        _sut.GenericVoidMethod<string, int>();
    }

    [Fact]
    public void GenericVoidMethodWithGenericParam_IsImplemented()
    {
        var method = typeof(IMethodsTestService)
            .GetMethods()
            .First(x => x.Name == nameof(MethodsTestService.GenericVoidMethodWithGenericParam));

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(void));

        var genericArgs = method.GetGenericArguments();
        genericArgs.Should().HaveCount(2);

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(genericArgs[0]);

        _sut.GenericVoidMethodWithGenericParam<string, int>(string.Empty);
    }

    [Fact]
    public void GenericVoidMethodWithConstraints_IsImplemented()
    {
        var method = typeof(IMethodsTestService)
            .GetMethods()
            .First(x => x.Name == nameof(MethodsTestService.GenericVoidMethodWithConstraints));

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(void));

        var genericArgs = method.GetGenericArguments();
        genericArgs.Should().HaveCount(2);

        genericArgs[0].IsClass.Should().BeTrue();
        genericArgs[0].GetGenericParameterConstraints().Should().HaveCount(0);

        genericArgs[1].IsClass.Should().BeTrue();
        genericArgs[1].GetGenericParameterConstraints().Should().HaveCount(1);
        genericArgs[1].GetGenericParameterConstraints()[0].Should().Be(genericArgs[0]);
        genericArgs[1]
            .GenericParameterAttributes.Should()
            .HaveFlag(GenericParameterAttributes.DefaultConstructorConstraint);

        _sut.GenericVoidMethodWithConstraints<object, StringBuilder>();
    }

    [Fact]
    public void VoidMethodWithOptionalParams_IsImplemented()
    {
        var method = typeof(IMethodsTestService)
            .GetMethods()
            .First(x => x.Name == nameof(MethodsTestService.VoidMethodWithOptionalParams));

        method.Should().NotBeNull();
        method.ReturnType.Should().Be(typeof(void));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(10);
        parameters.Select(x => x.IsOptional).Should().AllBeEquivalentTo(true);

        parameters[0].DefaultValue.Should().Be("cGFyYW0=");
        parameters[1].DefaultValue.Should().Be(MethodsTestService.StringConstant);
        parameters[2].DefaultValue.Should().Be(0.1f);
        parameters[3].DefaultValue.Should().Be(0.2d);
        parameters[4].DefaultValue.Should().Be(0.3d);
        parameters[5].DefaultValue.Should().Be(true);
        parameters[6].DefaultValue.Should().Be(false);
        parameters[7].DefaultValue.Should().Be(true);
        parameters[8].DefaultValue.Should().Be(false);
        parameters[9].DefaultValue.Should().Be(null);

        _sut.VoidMethodWithOptionalParams();
    }

    [Fact]
    public void VoidMethodWithExpandingParam_IsImplemented()
    {
        var method = typeof(IMethodsTestService)
            .GetMethods()
            .First(x => x.Name == nameof(MethodsTestService.VoidMethodWithExpandingParam));

        method.ReturnType.Should().Be(typeof(void));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(string[]));
        parameters[0].GetCustomAttribute<ParamArrayAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void IgnoreMethod_IsOmitted()
    {
        var method = typeof(IMethodsTestService)
            .GetMethods()
            .FirstOrDefault(x => x.Name == nameof(MethodsTestService.IgnoredMethod));

        method.Should().BeNull();
    }

    [Fact]
    public void StaticMethod_IsOmitted()
    {
        var method = typeof(IMethodsTestService)
            .GetMethods()
            .FirstOrDefault(x => x.Name == nameof(MethodsTestService.StaticMethod));

        method.Should().BeNull();
    }
}

[GenerateAutoInterface]
internal class MethodsTestService : IMethodsTestService
{
    public const string StringConstant = "Const";

    public void VoidMethod() { }

    public void VoidMethodWithParams(string a, string b) { }

    public void VoidMethodWithKeywordParam(string @void) { }

    public void VoidMethodWithOutParam(out string a)
    {
        a = string.Empty;
    }

    public void VoidMethodWithRefParam(ref string a) { }

    public void VoidMethodWithInParam(in string a) { }

    public string StringMethod()
    {
        return string.Empty;
    }

    public string? StringMethodNullable()
    {
        return null;
    }

    public void GenericVoidMethod<TX, TY>() { }

    public void GenericVoidMethodWithGenericParam<TX, TY>(TX a) { }

    public void GenericVoidMethodWithConstraints<TX, TY>()
        where TX : class
        where TY : class, TX, new() { }

    public void VoidMethodWithOptionalParams(
        string stringLiteral = "cGFyYW0=",
        string stringConstant = StringConstant,
        float floatLiteral = 0.1f,
        double doubleLiteral = 0.2,
        decimal decimalLiteral = 0.3m,
        bool trueLiteral = true,
        bool falseLiteral = false,
        bool? nullableTrueLiteral = true,
        bool? nullableFalseLiteral = false,
        bool? nullableNullBoolLiteral = null
    ) { }

    public void VoidMethodWithExpandingParam(params string[] strings) { }

    [AutoInterfaceIgnore]
    public void IgnoredMethod() { }

    public static void StaticMethod() { }
}

[GenerateAutoInterface]
internal class MethodsTestServiceGeneric<T> : IMethodsTestServiceGeneric<T>
    where T : class
{
    public T? ResolveInstance(string strongName) => throw new NotImplementedException();
}
