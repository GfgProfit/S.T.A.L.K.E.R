using UnityEngine;

public static class GraphicsSettingsRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeGraphicsSettings()
    {
        GraphicsData data = GraphicsSettingsStorage.LoadGraphicsSettings();
        GraphicsSettingsApplier applier = new(false);
        applier.ApplyGraphicsSettings(data);
    }
}
