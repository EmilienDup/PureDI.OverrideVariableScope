# Two factories that override the same type under one root: the second override variable is not declared (CS0103)

**Pure.DI master (da6574d)** and **2.5.7**, .NET SDK 10.0.403, macOS. Repro project attached (`dotnet build`).

## Minimal code

```csharp
using Pure.DI;

var composition = new Composition();
Console.WriteLine(composition.Root);

public sealed class Name(string value)
{
    public override string ToString() => value;
}

public interface IService;

public sealed class Service(Name name) : IService
{
    public override string ToString() => name.ToString();
}

public sealed class App(IService a, [Tag("b")] IService b)
{
    public override string ToString() => $"{a} {b}";
}

partial class Composition
{
    private static void Setup() => DI.Setup(nameof(Composition))
        .Bind<IService>().To(ctx =>
        {
            ctx.Override(new Name("a"));
            ctx.Inject(out Service service);
            return service;
        })
        .Bind<IService>("b").To(ctx =>
        {
            ctx.Override(new Name("b"));
            ctx.Inject(out Service service);
            return service;
        })
        .Root<App>("Root");
}
```

## Expected

The code compiles and prints `a b`.

## Actual

```
Composition.g.cs(86,5): error CS0103: The name 'overriddenName' does not exist in the current context
Composition.g.cs(87,57): error CS0103: The name 'overriddenName' does not exist in the current context
```

```csharp
global::Service transientService;
lock (_lock307d)
{
    global::Name overriddenName = new Name("a");
    global::Service localService = new global::Service(overriddenName);
    transientService = localService;
}
global::Service transientService1;
lock (_lock307d)
{
    overriddenName = new Name("b");
    global::Service localService1 = new global::Service(overriddenName);
    transientService1 = localService1;
}
```

Each factory with an override is generated in its own `lock` block, but `overriddenName` is declared only in the first one. With `.Hint(Hint.ThreadSafe, "Off")` both factories share one scope, and the code compiles and prints `a b`.

## Cause

`FactoryCodeBuilder.BuildOverrides` writes the declaration only the first time it adds the variable name to `ctx.Overrides`. That set is shared by the whole root (it is only cleared for lazy accumulators in `RootCodeBuilder`). The `lock` block that `FactoryCodeBuilder` opens for a factory with overrides (`hasOverridesLock`) is a new C# scope, so the declaration made in the previous block is out of scope.

A fix that worked for us in a similar case: give each new C# scope (here, each `hasOverridesLock` block) a new `Overrides` set, so that the first override in that scope declares the variable again.
