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
        // Compiles with .Hint(Hint.ThreadSafe, "Off")
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
