using System;
using UnityEngine.SceneManagement;

public class SceneLoader
{
    public void LoadScene(string sceneName, Action onLoaded = null)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        if (onLoaded != null)
            op.completed += _ => onLoaded();
    }
}
