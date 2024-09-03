// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using Speckle.InterfaceGenerator;

namespace InterfaceGenerator.Tests.SameName_1;

/// <summary>
/// A class with the same name as <see cref="SameName_2.SameNameClass"/>. It exists to test if the generated source units have fully
/// qualified names.
/// </summary>
[GenerateAutoInterface]
public class SameNameClass : ISameNameClass { }

[GenerateAutoInterface]
public class SameNameClass2 : ISameNameClass2
{
    public ISameNameClass Return() => throw new InvalidOperationException();

    public SymbolToken Return2() => throw new InvalidOperationException();

    public T GetRequiredService<T>()
        where T : class => throw new InvalidOperationException();

    public void TestGenericParameter(List<SameNameClass> x) =>
        throw new InvalidOperationException();
}
