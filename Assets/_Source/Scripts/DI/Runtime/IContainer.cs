using System;
using UnityEngine;

public interface IContainer : IDisposable
{
    BindingBuilder Bind<TAbstraction, TImplementation>() where TImplementation : TAbstraction;
    BindingBuilder Bind<TAbstraction>(Func<IContainer, TAbstraction> factory);
    BindingBuilder BindInstance<T>(T instance);

    bool IsRegistered<T>();
    bool IsRegistered(Type type);
    T Resolve<T>();
    bool TryResolve<T>(out T value);
    object Resolve(Type type);
    void Inject(object target);
    void InjectGameObject(GameObject root, bool includeInactive = true);
    IContainer CreateChildScope();
}
