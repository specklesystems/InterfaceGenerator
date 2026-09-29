using System;
using System.Reflection;
using AwesomeAssertions;
using Xunit;

namespace Speckle.InterfaceGenerator.Tests;

public class GenericInterfaceTests
{
    [Fact]
    public void GenericParametersGeneratedCorrectly()
    {
        var genericArgs = typeof(IGenericInterfaceTestsService<,>).GetGenericArguments();

        genericArgs.Should().HaveCount(2);
        genericArgs[0].Name.Should().Be("TX");
        genericArgs[1].Name.Should().Be("TY");

        genericArgs[0].IsClass.Should().BeTrue();
        genericArgs[0]
            .GenericParameterAttributes.Should()
            .HaveFlag(GenericParameterAttributes.DefaultConstructorConstraint);

        var iEquatableOfTx = typeof(IEquatable<>).MakeGenericType(genericArgs[0]);
        genericArgs[0]
            .GetGenericParameterConstraints()
            .Should()
            .HaveCount(1)
            .And.Contain(iEquatableOfTx);

        genericArgs[1].IsValueType.Should().BeTrue();
    }

    [Fact]
    public void ConstraintAfterUnconstrainedParameter_GeneratedCorrectly()
    {
        var genericArgs = typeof(IConstraintAfterUnconstrainedService<,>).GetGenericArguments();

        genericArgs[0].GenericParameterAttributes.Should().Be(GenericParameterAttributes.None);
        genericArgs[1]
            .GenericParameterAttributes.Should()
            .HaveFlag(GenericParameterAttributes.ReferenceTypeConstraint);
    }
}

[GenerateAutoInterface]
// ReSharper disable once UnusedType.Global
internal class GenericInterfaceTestsService<TX, TY> : IGenericInterfaceTestsService<TX, TY>
    where TX : class, IEquatable<TX>, new()
    where TY : struct { }

[GenerateAutoInterface]
internal class ConstraintAfterUnconstrainedService<TA, TB>
    : IConstraintAfterUnconstrainedService<TA, TB>
    where TB : class { }
