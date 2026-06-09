using System;

public sealed class Binding
{
    public Type Abstraction;
    public Func<IContainer, object> Factory;
    public Lifetime Lifetime;
    public object SingletonInstance;
    public bool IsExternInstance;
}