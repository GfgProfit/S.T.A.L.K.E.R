using UnityEngine;

public class Test : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Time.timeScale = Time.timeScale == 0.1f ? 1 : 0.1f;
        }
    }
}