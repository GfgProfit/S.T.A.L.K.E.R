using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public sealed class DiContainer : IContainer
{
    private readonly BindingRegistry _bindings = new();
    private readonly DiInjector _injector = new();
    private readonly HashSet<IDisposable> _ownedDisposables = new();
    private readonly Stack<Type> _buildStack = new();
    private readonly DiContainer _parent;

    private bool _disposed;

    public DiContainer(DiContainer parent = null) => _parent = parent;

    public BindingBuilder Bind<TAbstraction, TImplementation>() where TImplementation : TAbstraction
    {
        return Register(typeof(TAbstraction), _ => Create(typeof(TImplementation)));
    }

    public BindingBuilder Bind<TAbstraction>(Func<IContainer, TAbstraction> factory)
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return Register(typeof(TAbstraction), container => factory(container));
    }

    public BindingBuilder BindInstance<T>(T instance)
    {
        Binding binding = new()
        {
            Abstraction = typeof(T),
            Factory = _ => instance,
            Lifetime = Lifetime.Singleton,
            SingletonInstance = instance,
            IsExternInstance = true
        };

        _bindings.Add(binding);
        Inject(instance);

        return new BindingBuilder(binding);
    }

    public bool IsRegistered<T>()
    {
        return IsRegistered(typeof(T));
    }

    public bool IsRegistered(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        EnsureNotDisposed();

        return _bindings.Contains(type) || (_parent != null && _parent.IsRegistered(type));
    }

    public T Resolve<T>() => (T)Resolve(typeof(T));

    public bool TryResolve<T>(out T value)
    {
        try
        {
            value = Resolve<T>();
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public object Resolve(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        EnsureNotDisposed();

        if (_bindings.TryGet(type, out Binding binding))
        {
            return ResolveBinding(binding);
        }

        if (!type.IsAbstract && !type.IsInterface)
        {
            return Create(type);
        }

        if (_parent != null)
        {
            return _parent.Resolve(type);
        }

        throw new InvalidOperationException($"No binding found for type {type}.");
    }

    public void Inject(object target)
    {
        _injector.Inject(target, Resolve);
    }

    public void InjectGameObject(GameObject root, bool includeInactive = true)
    {
        if (root == null)
        {
            return;
        }

        foreach (MonoBehaviour monoBehaviour in root.GetComponentsInChildren<MonoBehaviour>(includeInactive))
        {
            if (monoBehaviour == null)
            {
                continue;
            }

            Inject(monoBehaviour);
        }
    }

    public IContainer CreateChildScope() => new DiContainer(this);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (IDisposable disposable in _ownedDisposables.Reverse())
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        _ownedDisposables.Clear();
    }

    private BindingBuilder Register(Type abstraction, Func<IContainer, object> factory)
    {
        if (abstraction == null)
        {
            throw new ArgumentNullException(nameof(abstraction));
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        Binding binding = new()
        {
            Abstraction = abstraction,
            Factory = factory,
            Lifetime = Lifetime.Transient
        };

        _bindings.Add(binding);

        return new BindingBuilder(binding);
    }

    private object ResolveBinding(Binding binding)
    {
        if (binding.Lifetime == Lifetime.Singleton)
        {
            if (binding.SingletonInstance == null)
            {
                binding.SingletonInstance = binding.Factory(this);
                TrackIfDisposable(binding.SingletonInstance, binding.IsExternInstance);
                Inject(binding.SingletonInstance);
            }

            return binding.SingletonInstance;
        }

        object instance = binding.Factory(this);
        TrackIfDisposable(instance, externInstance: false);
        Inject(instance);

        return instance;
    }

    internal object Create(Type concreteType)
    {
        EnsureNotDisposed();

        if (_buildStack.Contains(concreteType))
        {
            string cycle = string.Join(" -> ", _buildStack.Reverse().Append(concreteType).Select(type => type.Name));
            throw new InvalidOperationException($"Cyclic dependency detected: {cycle}");
        }

        _buildStack.Push(concreteType);

        try
        {
            ConstructorInfo constructor = DiConstructorSelector.Select(concreteType);
            object[] arguments = constructor.GetParameters()
                .Select(ResolveParameter)
                .ToArray();

            object instance = Activator.CreateInstance(concreteType, arguments);
            Inject(instance);
            TrackIfDisposable(instance, externInstance: false);

            return instance;
        }
        finally
        {
            _buildStack.Pop();
        }
    }

    private object ResolveParameter(ParameterInfo parameterInfo)
    {
        try
        {
            return Resolve(parameterInfo.ParameterType);
        }
        catch (Exception)
        {
            if (parameterInfo.HasDefaultValue)
            {
                return parameterInfo.DefaultValue;
            }

            throw;
        }
    }

    private void TrackIfDisposable(object obj, bool externInstance)
    {
        if (externInstance)
        {
            return;
        }

        if (obj is IDisposable disposable)
        {
            _ownedDisposables.Add(disposable);
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DiContainer));
        }
    }
}
