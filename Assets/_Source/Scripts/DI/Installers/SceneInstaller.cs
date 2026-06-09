using System;
using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-1000)]
public abstract class SceneInstaller : MonoBehaviour
{
    [Header("Auto-inject scene on Awake")]
    [FormerlySerializedAs("InjectOnAwake")]
    [SerializeField] private bool _injectOnAwake = true;

    private IContainer _container;

    public static IContainer Container { get; private set; }

    protected abstract void Install(IContainer container);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Container?.Dispose();
        Container = null;
    }

    private void Awake()
    {
        _container = CreateContainer();

        if (_container == null)
        {
            throw new InvalidOperationException($"{GetType().Name} returned null container.");
        }

        Container = _container;

        try
        {
            Install(_container);

            if (_injectOnAwake)
            {
                InjectScene();
            }
        }
        catch
        {
            DisposeContainer();
            throw;
        }
    }

    private void OnDestroy()
    {
        DisposeContainer();
    }

    protected virtual IContainer CreateContainer()
    {
        return new DiContainer();
    }

    protected void InjectScene()
    {
        if (_container == null)
        {
            return;
        }

        GameObject[] roots = gameObject.scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            _container.InjectGameObject(roots[i], true);
        }
    }

    private void DisposeContainer()
    {
        IContainer container = _container;
        _container = null;

        if (ReferenceEquals(Container, container))
        {
            Container = null;
        }

        container?.Dispose();
    }
}
