# Pure.DI: two factories that override the same type under one root fail with CS0103

Minimal repro for Pure.DI master (da6574d) and 2.5.7: two factory bindings each call `ctx.Override(new Name(...))`, and both are reached from one root. With the default thread safety, each factory is generated in its own `lock` block, and the override variable `overriddenName` is declared only in the first block. With `.Hint(Hint.ThreadSafe, "Off")` it compiles and prints `a b`.

[`ISSUE.md`](ISSUE.md) is the text to paste into a Pure.DI GitHub issue.

```
dotnet build                                                                                   # 2.5.7 from nuget.org
dotnet build -p:PureDIVersion=<master build version> -p:RestoreAdditionalProjectSources=<folder with the package>
```

The generated composition is written under `obj/Debug/net10.0/generated/Pure.DI`.
