using UnityEngine;
using UnityEngine.SceneManagement;

public class TestNewGame : MonoBehaviour
{
    public void Load() => SceneManager.LoadSceneAsync("Test", LoadSceneMode.Single);
}