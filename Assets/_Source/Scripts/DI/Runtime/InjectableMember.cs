using System;

internal readonly struct InjectableMember
{
    private readonly Action<object, object> _setter;

    public InjectableMember(Type dependencyType, string kind, string name, bool optional, Action<object, object> setter)
    {
        DependencyType = dependencyType;
        Kind = kind;
        Name = name;
        Optional = optional;
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
    }

    public Type DependencyType { get; }
    public string Kind { get; }
    public string Name { get; }
    public bool Optional { get; }

    public void SetValue(object target, object value)
    {
        _setter(target, value);
    }
}
