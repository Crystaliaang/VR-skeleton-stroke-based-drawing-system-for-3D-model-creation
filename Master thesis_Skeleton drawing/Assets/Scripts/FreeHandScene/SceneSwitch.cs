using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{

    //public string sceneName = 3DDrawing;
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}