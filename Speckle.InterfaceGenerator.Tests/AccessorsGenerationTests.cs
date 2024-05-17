﻿using System;
using System.Runtime.CompilerServices;
using FluentAssertions;
using FluentAssertions.Common;
using Xunit;

namespace Speckle.InterfaceGenerator.Tests;

public class AccessorsGenerationTests
{
    private readonly IAccessorsTestsService _sut;

    public AccessorsGenerationTests()
    {
        _sut = new AccessorsTestsService();
    }

    [Fact]
    public void GetSetIndexer_IsImplemented()
    {
        var indexer = typeof(IAccessorsTestsService).GetIndexerByParameterTypes(new[] { typeof(string) });

        indexer.Should().NotBeNull();

        indexer.GetMethod.Should().NotBeNull();
        indexer.SetMethod.Should().NotBeNull();

        var _ = _sut[string.Empty];
        _sut[string.Empty] = 0;
    }

    [Fact]
    public void PublicProperty_IsImplemented()
    {
        var prop = typeof(IAccessorsTestsService)
            .GetProperty(nameof(IAccessorsTestsService.PublicProperty)) ?? throw new InvalidOperationException();

        prop.Should().NotBeNull();

        prop.GetMethod.Should().NotBeNull();
        prop.SetMethod.Should().NotBeNull();

        var _ = _sut.PublicProperty;
        _sut.PublicProperty = string.Empty;
    }

    [Fact]
    public void InitProperty_IsImplemented()
    {
        var prop = typeof(IAccessorsTestsService)
            .GetProperty(nameof(IAccessorsTestsService.InitOnlyProperty)) ?? throw new InvalidOperationException();

        prop.Should().NotBeNull();

        prop.GetMethod.Should().NotBeNull();
        prop.SetMethod.Should().NotBeNull();

        prop.SetMethod?.ReturnParameter?.GetRequiredCustomModifiers().Should().Contain(typeof(IsExternalInit));

        var _ = _sut.InitOnlyProperty;
    }

    [Fact]
    public void PrivateSetter_IsOmitted()
    {
        var prop = typeof(IAccessorsTestsService)
            .GetProperty(nameof(IAccessorsTestsService.PropertyWithPrivateSetter)) ?? throw new InvalidOperationException();

        prop.Should().NotBeNull();

        prop.GetMethod.Should().NotBeNull();
        prop.SetMethod.Should().BeNull();

        var _ = _sut.PropertyWithPrivateSetter;
    }

    [Fact]
    public void PrivateGetter_IsOmitted()
    {
        var prop = typeof(IAccessorsTestsService)
            .GetProperty(nameof(IAccessorsTestsService.PropertyWithPrivateGetter)) ?? throw new InvalidOperationException();

        prop.Should().NotBeNull();

        prop.SetMethod.Should().NotBeNull();
        prop.GetMethod.Should().BeNull();

        _sut.PropertyWithPrivateGetter = string.Empty;
    }

    [Fact]
    public void ProtectedSetter_IsOmitted()
    {
        var prop = typeof(IAccessorsTestsService)
            .GetProperty(nameof(IAccessorsTestsService.PropertyWithProtectedSetter)) ?? throw new InvalidOperationException();

        prop.Should().NotBeNull();

        prop.GetMethod.Should().NotBeNull();
        prop.SetMethod.Should().BeNull();

        var _ = _sut.PropertyWithProtectedSetter;
    }

    [Fact]
    public void ProtectedGetter_IsOmitted()
    {
        var prop = typeof(IAccessorsTestsService)
            .GetProperty(nameof(IAccessorsTestsService.PropertyWithProtectedGetter)) ?? throw new InvalidOperationException();

        prop.Should().NotBeNull();

        prop.SetMethod.Should().NotBeNull();
        prop.GetMethod.Should().BeNull();

        _sut.PropertyWithProtectedGetter = string.Empty;
    }

    [Fact]
    public void IgnoredProperty_IsOmitted()
    {
        var prop = typeof(IAccessorsTestsService)
            .GetProperty(nameof(AccessorsTestsService.IgnoredProperty));

        prop.Should().BeNull();
    }

    [Fact]
    public void StaticProperty_IsOmitted()
    {
        var prop = typeof(IAccessorsTestsService)
            .GetProperty(nameof(AccessorsTestsService.StaticProperty));

        prop.Should().BeNull();
    }
}

// ReSharper disable UnusedMember.Local, ValueParameterNotUsed
[GenerateAutoInterface]
internal class AccessorsTestsService : IAccessorsTestsService
{
    public int this[string x]
    {
        get => 0;
        set
        {
        }
    }

    public string PublicProperty { get; set; }

    public string InitOnlyProperty { get; init; }

    public string PropertyWithPrivateSetter { get; private set; }

    public string PropertyWithPrivateGetter { private get; set; }

    public string PropertyWithProtectedSetter { get; protected set; }

    public string PropertyWithProtectedGetter { protected get; set; }

    [AutoInterfaceIgnore] public string IgnoredProperty { get; set; }

    public static string StaticProperty { get; set; }
}
// ReSharper enable UnusedMember.Local, ValueParameterNotUsed