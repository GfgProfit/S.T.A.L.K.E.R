using System;
using System.Collections.Generic;

internal sealed class BindingRegistry
{
    private readonly Dictionary<Type, List<Binding>> _bindingsByType = new();

    public void Add(Binding binding)
    {
        if (binding == null)
        {
            throw new ArgumentNullException(nameof(binding));
        }

        if (binding.Abstraction == null)
        {
            throw new ArgumentException("Binding abstraction cannot be null.", nameof(binding));
        }

        if (binding.Factory == null)
        {
            throw new ArgumentException("Binding factory cannot be null.", nameof(binding));
        }

        if (!_bindingsByType.TryGetValue(binding.Abstraction, out List<Binding> bindings))
        {
            bindings = new();
            _bindingsByType[binding.Abstraction] = bindings;
        }

        bindings.Add(binding);
    }

    public bool Contains(Type abstraction)
    {
        return abstraction != null && _bindingsByType.ContainsKey(abstraction);
    }

    public bool TryGet(Type abstraction, out Binding binding)
    {
        if (abstraction != null && _bindingsByType.TryGetValue(abstraction, out List<Binding> bindings) && bindings.Count > 0)
        {
            binding = bindings[0];
            return true;
        }

        binding = null;
        return false;
    }
}
