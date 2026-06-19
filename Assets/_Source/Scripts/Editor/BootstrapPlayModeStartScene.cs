using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class BootstrapPlayModeStartScene
{
    private const string BOOTSTRAP_SCENE_PATH = "Assets/Scene/Bootstrap.unity";

    static BootstrapPlayModeStartScene()
    {
        EditorApplication.delayCall += ConfigurePlayModeStartScene;
    }

    private static void ConfigurePlayModeStartScene()
    {
        SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BOOTSTRAP_SCENE_PATH);

        if (bootstrapScene == null)
        {
            Debug.LogError($"Bootstrap scene was not found at {BOOTSTRAP_SCENE_PATH}.");
            return;
        }

        EditorSceneManager.playModeStartScene = bootstrapScene;
    }
}
