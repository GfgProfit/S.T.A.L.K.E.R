using System;

public sealed class BindingBuilder
{
    private readonly Binding _binding;

    public BindingBuilder(Binding binding)
    {
        _binding = binding ?? throw new ArgumentNullException(nameof(binding));
    }

    public BindingBuilder AsSingle()
    {
        return SetLifetime(Lifetime.Singleton);
    }

    public BindingBuilder AsSingleton()
    {
        return AsSingle();
    }

    public BindingBuilder AsTransient()
    {
        return SetLifetime(Lifetime.Transient);
    }

    private BindingBuilder SetLifetime(Lifetime lifetime)
    {
        _binding.Lifetime = lifetime;
        return this;
    }
}
